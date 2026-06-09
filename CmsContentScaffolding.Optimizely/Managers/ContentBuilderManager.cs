using CmsContentScaffolding.Optimizely.Helpers;
using CmsContentScaffolding.Optimizely.Interfaces;
using CmsContentScaffolding.Optimizely.Models;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Security;
using EPiServer.Web;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace CmsContentScaffolding.Optimizely.Managers;

internal class ContentBuilderManager : IContentBuilderManager
{
    #region Private properties

    private readonly ISiteDefinitionRepository _siteDefinitionRepository;
    private readonly IContentRepository _contentRepository;
    private readonly IContentSecurityRepository _contentSecurityRepository;
    private readonly IContentLoader _contentLoader;
    private readonly ILanguageBranchRepository _languageBranchRepository;
    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IOptionsMonitor<ContentBuilderOptions> _options;

    #endregion

    #region Public properties

    public static ContentReference CurrentAssetsReference { get; set; } = ContentReference.GlobalBlockFolder;

    private static readonly object _siteCreationLock = new();

    /// <summary>
    /// True when the start page was created during this build run (i.e. the site
    /// did not previously exist). Used to decide whether the start page should
    /// receive its initial content, so that an already-existing start page is not
    /// re-written (and its MainContentArea wiped) on every application restart.
    /// </summary>
    public bool StartPageCreated { get; private set; }

    public bool SiteExists =>
        _siteDefinitionRepository
        .List()
        .Where(x =>
            x.Name.Equals(_options.CurrentValue.SiteName) &&
            x.Hosts.Any(y => y.Language.Equals(_options.CurrentValue.Language)))
        .Any();

    #endregion

    #region Constructors

    public ContentBuilderManager(
        ISiteDefinitionRepository siteDefinitionRepository,
        IContentRepository contentRepository,
        IOptionsMonitor<ContentBuilderOptions> options,
        IContentLoader contentLoader,
        ILanguageBranchRepository languageBranchRepository,
        IContentSecurityRepository contentSecurityRepository,
        IContentTypeRepository contentTypeRepository)
    {
        _siteDefinitionRepository = siteDefinitionRepository;
        _contentRepository = contentRepository;
        _options = options;
        _contentLoader = contentLoader;
        _languageBranchRepository = languageBranchRepository;
        _contentSecurityRepository = contentSecurityRepository;
        _contentTypeRepository = contentTypeRepository;
    }

    #endregion

    #region Public methods

    public SiteDefinition GetOrCreateSite()
    {
        var siteUri = new Uri(_options.CurrentValue.SiteHost);

        // Fast path: if a matching site already exists and we're not asked to
        // replace the primary, skip the lock entirely. Repository reads are
        // thread-safe; only the create/replace branches need serialization.
        if (!_options.CurrentValue.ReplaceExistingPrimarySite)
        {
            var existing = _siteDefinitionRepository
                .List()
                .SingleOrDefault(x => x.Hosts.Any(h => h.Name.Equals(siteUri.Authority)));

            if (existing is not null)
                return existing;
        }

        lock (_siteCreationLock)
        {
            // Re-check inside the lock — another thread may have just created it.
            var existingSite = _siteDefinitionRepository
                .List()
                .SingleOrDefault(x => x.Hosts.Any(h => h.Name.Equals(siteUri.Authority)));

            if (_options.CurrentValue.ReplaceExistingPrimarySite)
            {
                var primarySite = _siteDefinitionRepository
                    .List()
                    .SingleOrDefault(x => x.Hosts.Any(y => y.Type == HostDefinitionType.Primary));

                if (primarySite != null)
                {
                    var primaryHost = primarySite.Hosts.Single(x => x.Type == HostDefinitionType.Primary);
                    primaryHost.Type = HostDefinitionType.Undefined;
                    primarySite.Hosts.Remove(primaryHost);
                    primarySite.Hosts.Add(new()
                    {
                        Name = siteUri.Authority,
                        Language = _options.CurrentValue.Language,
                        Type = HostDefinitionType.Primary,
                        UseSecureConnection = siteUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
                    });

                    _siteDefinitionRepository.Save(primarySite);
                    existingSite = primarySite;
                }
            }

            if (existingSite is not null)
                return existingSite;

            var startPage = TryCreateStartPage();
            var siteDefinition = new SiteDefinition
            {
                Name = _options.CurrentValue.SiteName,
                StartPage = startPage,
                SiteAssetsRoot = GetOrCreateSiteAssetsRoot(startPage),
                SiteUrl = siteUri,
                Hosts = new List<HostDefinition>
                 {
                     new()
                     {
                         Name = siteUri.Authority,
                         Language = _options.CurrentValue.Language,
                         Type = HostDefinitionType.Primary,
                         UseSecureConnection = siteUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
                     }
                 }
            };

            _siteDefinitionRepository.Save(siteDefinition);
            return siteDefinition;
        }
    }

    public void SetStartPageSecurity(ContentReference pageRef)
    {
        if (_options.CurrentValue.SiteRolesAccessLevel is null || !_options.CurrentValue.SiteRolesAccessLevel.Any())
            return;

        if (_contentSecurityRepository.Get(SiteDefinition.Current.StartPage).CreateWritableClone() is IContentSecurityDescriptor startPageSecurity)
        {
            foreach (var role in _options.CurrentValue.SiteRolesAccessLevel)
                if (startPageSecurity.Entries.Any(x => x.Name.Equals(role)))
                    return;

            if (startPageSecurity.IsInherited)
                startPageSecurity.ToLocal();

            foreach (var role in _options.CurrentValue.SiteRolesAccessLevel)
                startPageSecurity.AddEntry(new AccessControlEntry(role.Key, role.Value, SecurityEntityType.Role));

            _contentSecurityRepository.Save(startPageSecurity.ContentLink, startPageSecurity, SecuritySaveType.Replace);
        }
    }

    public void ApplyDefaultLanguage()
    {
        DisableLanguage("sv");
        CreateAndEnableLanguage(_options.CurrentValue.Language);
        AppendLanguageToExistingLanguages(ContentReference.RootPage, _options.CurrentValue.Language);
    }

    public void CreateAndEnableLanguage(CultureInfo culture)
    {
        var availableLanguages = _languageBranchRepository.ListAll();

        if (availableLanguages.Any(x => x.Culture.Equals(culture)))
        {
            var existingLanguage = availableLanguages.Single(x => x.Culture.Equals(culture));

            if (!existingLanguage.Enabled)
                _languageBranchRepository.Enable(existingLanguage.Culture);
        }
        else
        {
            var newLanguageBranch = new LanguageBranch(culture);
            _languageBranchRepository.Save(newLanguageBranch);
            _languageBranchRepository.Enable(newLanguageBranch.Culture);
        }
    }

    public void AppendLanguageToExistingLanguages(ContentReference contentReference, CultureInfo language)
    {
        var page = _contentLoader.Get<PageData>(contentReference);

        if (!page.ExistingLanguages.Any(x => x.Equals(language)))
        {
            var pageClone = page.CreateWritableClone();
            _ = pageClone.ExistingLanguages.Append(language);
            _contentRepository.Save(pageClone, SaveAction.Default | (_options.CurrentValue.SkipValidation ? SaveAction.SkipValidation : SaveAction.Default), AccessLevel.NoAccess);
        }
    }

    public void CreateDefaultRoles(IDictionary<string, AccessLevel> roles)
    {
        _ = ServiceLocator.Current.TryGetExistingInstance<UIRoleProvider>(out var uiRoleProvider);

        if (!roles.Any())
            return;

        var rootPageSecurity = _contentSecurityRepository.Get(ContentReference.RootPage).CreateWritableClone() as IContentSecurityDescriptor;

        foreach (var role in roles)
        {
            if (uiRoleProvider != null)
            {
                if (uiRoleProvider.RoleExistsAsync(role.Key).GetAwaiter().GetResult())
                    continue;

                uiRoleProvider.CreateRoleAsync(role.Key).GetAwaiter().GetResult();
            }

            if (rootPageSecurity == null || rootPageSecurity.Entries.Any(x => x.Name.Equals(role.Key)))
                continue;

            rootPageSecurity.AddEntry(new AccessControlEntry(role.Key, role.Value, SecurityEntityType.Role));
            _contentSecurityRepository.Save(rootPageSecurity.ContentLink, rootPageSecurity, SecuritySaveType.Replace);
        }
    }

    public void CreateRoles(IDictionary<string, AccessLevel>? roles)
    {
        if (!ServiceLocator.Current.TryGetExistingInstance<UIRoleProvider>(out var uiRoleProvider))
            return;

        if (roles is null || !roles.Any())
            return;

        foreach (var role in roles)
        {
            if (uiRoleProvider.RoleExistsAsync(role.Key).GetAwaiter().GetResult())
                continue;

            uiRoleProvider.CreateRoleAsync(role.Key).GetAwaiter().GetResult();
        }
    }

    public void CreateUsers(IEnumerable<UserModel>? users)
    {
        if (!ServiceLocator.Current.TryGetExistingInstance<UIRoleProvider>(out var uiRoleProvider))
            return;

        if (!ServiceLocator.Current.TryGetExistingInstance<UIUserProvider>(out var uiUserProvider))
            return;

        if (users is null || !users.Any())
            return;

        IUIUser? uiUser;

        foreach (var user in users)
        {
            uiUser = uiUserProvider.GetUserAsync(user.UserName).GetAwaiter().GetResult();

            if (uiUser != null)
                continue;

            uiUserProvider.CreateUserAsync(user.UserName, user.Password, user.Email, null, null, true).GetAwaiter().GetResult();

            if (user.Roles.Any())
                uiRoleProvider.AddUserToRolesAsync(user.UserName, user.Roles).GetAwaiter().GetResult();
        }
    }

    public void SetContentName<T>(IContent content, string? name = default, string? nameSuffix = default) where T : IContentData
    {
        if (!string.IsNullOrEmpty(content.Name) &&
            !content.Name.Equals(Constants.TempPageName, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrEmpty(nameSuffix))
            return;

        if (!string.IsNullOrEmpty(name))
        {
            if (!string.IsNullOrEmpty(nameSuffix))
            {
                content.Name = $"{name} {nameSuffix}";
                return;
            }

            content.Name = name;
            return;
        }

        if (!string.IsNullOrEmpty(content.Name) && !content.Name.Equals(Constants.TempPageName))
            content.Name = $"{content.Name} {nameSuffix ?? Guid.NewGuid().ToString()}";
        else
            content.Name = $"{_contentTypeRepository.Load<T>().Name} {nameSuffix ?? Guid.NewGuid().ToString()}";
    }

    public ContentReference CreateItem<T>(string? name = default, string? suffix = default, Action<T>? options = default) where T : IContentData
    {
        var content = _contentRepository.GetDefault<T>(CurrentAssetsReference, _options.CurrentValue.Language);

        PropertyHelpers.InitProperties(content);
        options?.Invoke(content);

        var iContent = (IContent)content;
        SetContentName<T>(iContent, name, suffix);

        if (!ContentReference.IsNullOrEmpty(iContent.ContentLink))
            return iContent.ContentLink;

        return _contentRepository.Save(iContent, (_options.CurrentValue.PublishContent ? SaveAction.Publish : SaveAction.Default) | (_options.CurrentValue.SkipValidation ? SaveAction.SkipValidation : SaveAction.Default), AccessLevel.NoAccess);
    }

    #endregion

    #region Private methods

    private ContentReference TryCreateStartPage()
    {
        if (_options.CurrentValue.StartPageType == null)
            return ContentReference.RootPage;

        var startPageType = _contentTypeRepository.Load(_options.CurrentValue.StartPageType);
        var startPage = _contentRepository.GetDefault<PageData>(ContentReference.RootPage, startPageType.ID, _options.CurrentValue.Language);
        startPage.Name = _options.CurrentValue.StartPageType.Name;

        // The start page is the site's entry point and must always have a published
        // version. If it is only saved as a draft (SaveAction.Default), the site has
        // no servable start page once the application restarts and the content cache
        // is gone, so its content appears to be lost. Publish it regardless of
        // PublishContent; validation is skipped because this initial page is empty.
        var startPageReference = _contentRepository.Save(startPage, SaveAction.SkipValidation | SaveAction.Publish, AccessLevel.NoAccess);

        // Flag that this run created the start page, so its initial content is applied
        // exactly once. On later runs the existing start page must be left untouched.
        StartPageCreated = true;

        return startPageReference;
    }

    private ContentReference GetOrCreateSiteAssetsRoot(ContentReference pageRef)
    {
        if (ContentReference.IsNullOrEmpty(pageRef) || pageRef.CompareToIgnoreWorkID(ContentReference.RootPage))
            return ContentReference.GlobalBlockFolder;

        var siteRoot = _contentRepository.GetDefault<ContentFolder>(pageRef);
        siteRoot.Name = _options.CurrentValue.SiteName;

        return _contentRepository.Save(siteRoot, AccessLevel.NoAccess);
    }

    private void DisableLanguage(string languageId)
    {
        var availableLanguages = _languageBranchRepository.ListAll();
        var lang = availableLanguages.SingleOrDefault(x => x.LanguageID.Equals(languageId, StringComparison.OrdinalIgnoreCase));

        if (lang != null && !_options.CurrentValue.Language.TwoLetterISOLanguageName.Equals(languageId, StringComparison.OrdinalIgnoreCase))
            _languageBranchRepository.Disable(lang.Culture);
    }

    #endregion
}
