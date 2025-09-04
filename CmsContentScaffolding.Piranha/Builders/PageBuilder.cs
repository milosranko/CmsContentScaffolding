using CmsContentScaffolding.Piranha.Interfaces;
using CmsContentScaffolding.Piranha.Models;
using Piranha;
using Piranha.Models;
using System.ComponentModel.DataAnnotations;

namespace CmsContentScaffolding.Piranha.Builders;

public class PageBuilder : IPageBuilder
{
    private readonly IApi _api;
    private readonly CmsContentApplicationBuilderOptions _options;
    private Guid? _parentId;
    private Guid? _siteId;
    private bool _stop = false;

    #region Public properties

    public static IPageBuilder Empty => new PageBuilder();

    #endregion

    public PageBuilder() => _stop = true;

    public PageBuilder(IApi api, Guid? parentId, Guid? siteId, CmsContentApplicationBuilderOptions options)
    {
        _api = api;
        _parentId = parentId;
        _siteId = siteId;
        _options = options;
    }

    public IPageBuilder WithSite<T>(Action<T>? value = null)
        where T : SiteContent<T>
    {
        if (_stop || _parentId != null)
            return this;

        var availableLanguages = _api.Languages.GetAllAsync().GetAwaiter().GetResult();
        var languageId = _api.Languages.GetDefaultAsync().GetAwaiter().GetResult().Id;

        if (!availableLanguages.Any(x => x.Culture.Equals(_options.DefaultLanguage, StringComparison.OrdinalIgnoreCase)))
        {
            var newLanguage = new Language
            {
                Culture = _options.DefaultLanguage,
                Id = Guid.NewGuid(),
                IsDefault = true,
                Title = _options.DefaultLanguage
            };

            _api.Languages.SaveAsync(newLanguage).GetAwaiter().GetResult();
            languageId = newLanguage.Id;
        }

        var defaultSite = _api.Sites.GetDefaultAsync().GetAwaiter().GetResult();
        defaultSite.SiteTypeId = typeof(T).Name;
        defaultSite.LanguageId = languageId;
        _api.Sites.SaveAsync(defaultSite).GetAwaiter().GetResult();

        var site = SiteContent<T>.CreateAsync(_api).GetAwaiter().GetResult();

        value?.Invoke(site);

        _api.Sites.SaveContentAsync(defaultSite.Id, site).GetAwaiter().GetResult();
        _siteId = defaultSite.Id;

        return this;
    }

    public IPageBuilder WithPage<T>(Action<T>? value = null, Action<IPageBuilder>? options = null)
        where T : Page<T>
    {
        if (_stop || _siteId == null)
            return this;

        var page = Page<T>.CreateAsync(_api).GetAwaiter().GetResult();
        value?.Invoke(page);

        page.ParentId = _parentId;
        page.SiteId = _siteId.Value;

        if (string.IsNullOrEmpty(page.Title))
        {
            page.Title = $"{typeof(T).Name}_{Guid.NewGuid()}";
        }

        if (_options.PublishContent)
            _api.Pages.SaveAsync(page).GetAwaiter().GetResult();
        else
            _api.Pages.SaveDraftAsync(page).GetAwaiter().GetResult();

        var pageContentBuilder = new PageBuilder(_api, page.Id, _siteId, _options);
        options?.Invoke(pageContentBuilder);

        return pageContentBuilder;
    }

    public IPageBuilder WithPages<T>(Action<T>? value = null, [Range(1, 10000)] int totalPages = 1)
        where T : Page<T>
    {
        if (_stop || _siteId == null)
            return this;

        if (totalPages < 1 || totalPages > 10000)
            throw new ArgumentOutOfRangeException(nameof(totalPages));

        T page;

        for (int i = 0; i < totalPages; i++)
        {
            page = Page<T>.CreateAsync(_api).GetAwaiter().GetResult();
            value?.Invoke(page);

            if (string.IsNullOrEmpty(page.Title))
            {
                page.Title = $"{typeof(T).Name}_{i}";
            }
            else
            {
                page.Title = $"{page.Title}_{i}";
            }

            page.ParentId = _parentId;
            page.SiteId = _siteId.Value;

            if (_options.PublishContent)
                _api.Pages.SaveAsync(page).GetAwaiter().GetResult();
            else
                _api.Pages.SaveDraftAsync(page).GetAwaiter().GetResult();
        }

        return this;
    }
}
