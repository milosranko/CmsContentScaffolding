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
    private readonly ContentBuilderOptions _options;

    #endregion

    #region Public properties

    public ContentReference CurrentAssetsReference { get; set; } = ContentReference.EmptyReference;

    public bool SiteExists =>
        _siteDefinitionRepository
        .List()
        .Where(x =>
            x.Name.Equals(_options.SiteName) &&
            x.Hosts.Any(y => y.Language.Equals(_options.Language)))
        .Any();

    #endregion

    #region Constructors

    public ContentBuilderManager(
        ISiteDefinitionRepository siteDefinitionRepository,
        IContentRepository contentRepository,
        ContentBuilderOptions options,
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

    public void SetOrCreateSiteContext()
    {
        var existingSite = _siteDefinitionRepository
            .List()
            .SingleOrDefault(x => x.Name.Equals(_options.SiteName) && x.Hosts.Any(x => x.Language.Equals(_options.Language)));

        if (existingSite is not null)
        {
            SiteDefinition.Current = existingSite;
            return;
        }

        var startPage = TryCreateStartPage();
        var siteUri = new Uri(_options.SiteHost);
        var siteDefinition = new SiteDefinition
        {
            Name = _options.SiteName,
            StartPage = startPage,
            SiteAssetsRoot = GetOrCreateSiteAssetsRoot(startPage),
            SiteUrl = siteUri,
            Hosts = new List<HostDefinition>
            {
                new()
                {
                    Name = siteUri.Authority,
                    Language = _options.Language,
                    Type = HostDefinitionType.Primary,
                    UseSecureConnection = siteUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
                }
            }
        };

        _siteDefinitionRepository.Save(siteDefinition);
        SiteDefinition.Current = siteDefinition;
    }

    public void SetStartPageSecurity(ContentReference pageRef)
    {
        if (_options.Roles is null || !_options.Roles.Any())
            return;

        if (_contentSecurityRepository.Get(SiteDefinition.Current.StartPage).CreateWritableClone() is IContentSecurityDescriptor startPageSecurity)
        {
            foreach (var role in _options.Roles)
                if (startPageSecurity.Entries.Any(x => x.Name.Equals(role)))
                    return;

            if (startPageSecurity.IsInherited)
                startPageSecurity.ToLocal();

            foreach (var role in _options.Roles)
                startPageSecurity.AddEntry(new AccessControlEntry(role.Key, role.Value, SecurityEntityType.Role));

            _contentSecurityRepository.Save(startPageSecurity.ContentLink, startPageSecurity, SecuritySaveType.Replace);
        }
    }

    public void ApplyDefaultLanguage()
    {
        DisableLanguage("sv");
        CreateAndEnableLanguage(_options.Language);
        AppendLanguageToExistingLanguages(ContentReference.RootPage, _options.Language);
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
            _contentRepository.Save(pageClone, SaveAction.Default, AccessLevel.NoAccess);
        }
    }

    public void CreateDefaultRoles(IDictionary<string, AccessLevel> roles)
    {
        if (!ServiceLocator.Current.TryGetExistingInstance<UIRoleProvider>(out var uiRoleProvider))
            return;

        if (!roles.Any())
            return;

        var rootPageSecurity = _contentSecurityRepository.Get(ContentReference.RootPage).CreateWritableClone() as IContentSecurityDescriptor;

        foreach (var role in roles)
        {
            if (uiRoleProvider.RoleExistsAsync(role.Key).GetAwaiter().GetResult())
                continue;

            uiRoleProvider.CreateRoleAsync(role.Key).GetAwaiter().GetResult();

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
        var content = _contentRepository.GetDefault<T>(CurrentAssetsReference, _options.Language);

        PropertyHelpers.InitProperties(content);
        options?.Invoke(content);

        var iContent = (IContent)content;
        SetContentName<T>(iContent, name, suffix);

        if (!ContentReference.IsNullOrEmpty(iContent.ContentLink))
            return iContent.ContentLink;

        return _contentRepository.Save(iContent, _options.PublishContent ? SaveAction.Publish : SaveAction.Default, AccessLevel.NoAccess);
    }

    #endregion

    #region Private methods

    private ContentReference TryCreateStartPage()
    {
        if (_options.StartPageType == null)
            return ContentReference.RootPage;

        var startPageType = _contentTypeRepository.Load(_options.StartPageType);
        var startPage = _contentRepository.GetDefault<PageData>(ContentReference.RootPage, startPageType.ID, _options.Language);
        startPage.Name = _options.StartPageType.Name;

        return _contentRepository.Save(startPage, _options.PublishContent ? SaveAction.SkipValidation | SaveAction.Publish : SaveAction.SkipValidation | SaveAction.Default, AccessLevel.NoAccess);
    }

    private ContentReference GetOrCreateSiteAssetsRoot(ContentReference pageRef)
    {
        if (ContentReference.IsNullOrEmpty(pageRef) || pageRef.CompareToIgnoreWorkID(ContentReference.RootPage))
            return ContentReference.GlobalBlockFolder;

        var siteRoot = _contentRepository.GetDefault<ContentFolder>(pageRef);
        siteRoot.Name = _options.SiteName;

        return _contentRepository.Save(siteRoot, AccessLevel.NoAccess);
    }

    private void DisableLanguage(string languageId)
    {
        var availableLanguages = _languageBranchRepository.ListAll();
        var lang = availableLanguages.SingleOrDefault(x => x.LanguageID.Equals(languageId, StringComparison.OrdinalIgnoreCase));

        if (lang != null && !_options.Language.TwoLetterISOLanguageName.Equals(languageId, StringComparison.OrdinalIgnoreCase))
            _languageBranchRepository.Disable(lang.Culture);
    }

    #endregion
}
