using CmsContentScaffolding.Optimizely.Helpers;
using CmsContentScaffolding.Optimizely.Interfaces;
using CmsContentScaffolding.Optimizely.Models;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Framework.Blobs;
using EPiServer.Security;
using EPiServer.Web;
using System.Globalization;

namespace CmsContentScaffolding.Optimizely.Builders;

internal class AssetsBuilder : IAssetsBuilder
{
    #region Private properties

    private readonly ContentReference _parent;
    private readonly IContentRepository _contentRepository;
    private readonly IContentBuilderManager _contentBuilderManager;
    private readonly IBlobFactory _blobFactory;
    private readonly ContentBuilderOptions _options;
    private readonly bool _stop = false;

    #endregion

    #region Public properties

    public static AssetsBuilder Empty => new();

    #endregion

    #region Constructors

    public AssetsBuilder() => _stop = true;

    public AssetsBuilder(
        ContentReference parent,
        IContentRepository contentRepository,
        IContentBuilderManager contentBuilderManager,
        ContentBuilderOptions options,
        IBlobFactory blobFactory)
    {
        _parent = parent;
        _contentRepository = contentRepository;
        _contentBuilderManager = contentBuilderManager;
        _options = options;
        _blobFactory = blobFactory;
    }

    #endregion

    #region Public methods

    public IAssetsBuilder WithBlock<T>(string name, Action<T>? value = null) where T : IContentData
    {
        return WithBlock(true, name, out var contentReference, value);
    }

    public IAssetsBuilder WithBlock<T>(string name, Action<T> value, CultureInfo translationLanguage, string translatedName, Action<T> translation) where T : IContentData
    {
        return WithBlock(true, name, out _, value, translationLanguage, translatedName, translation);
    }

    public IAssetsBuilder WithBlock<T>(string name, out ContentReference contentReference, Action<T>? value = null) where T : IContentData
    {
        return WithBlock(true, name, out contentReference, value, null, null, null);
    }

    public IAssetsBuilder WithBlock<T>(string name, out ContentReference contentReference, Action<T>? value = null, CultureInfo? translationLanguage = null, string? translatedName = null, Action<T>? translation = null) where T : IContentData
    {
        return WithBlock(true, name, out contentReference, value, translationLanguage, translatedName, translation);
    }

    public IAssetsBuilder WithContent<T>(Action<T>? value = null, Action<IAssetsBuilder>? options = null) where T : IContent
    {
        return WithContent(out var contentReference, value, options);
    }

    public IAssetsBuilder WithContent<T>(out ContentReference contentReference, Action<T>? value = null, Action<IAssetsBuilder>? options = null) where T : IContent
    {
        contentReference = ContentReference.EmptyReference;
        if (_stop) return Empty;

        contentReference = _parent != null && !ContentReference.IsNullOrEmpty(_parent)
            ? _parent
            : SiteDefinition.Current.SiteAssetsRoot;
        var content = _contentRepository.GetDefault<T>(contentReference, _options.Language);

        PropertyHelpers.InitProperties(content);
        value?.Invoke(content);

        var existingContent = _contentRepository
            .GetChildren<T>(contentReference, _options.Language)
            .SingleOrDefault(x => ((IContent)x).Name.Equals(content.Name, StringComparison.OrdinalIgnoreCase));

        if (existingContent is null)
        {
            _contentBuilderManager.SetContentName<T>(content);
            contentReference = _contentRepository.Save(content, _options.PublishContent ? SaveAction.Publish : SaveAction.Default, AccessLevel.NoAccess);
        }
        else
        {
            contentReference = existingContent.ContentLink;
        }

        if (options == null)
            return this;

        var builder = new AssetsBuilder(contentReference, _contentRepository, _contentBuilderManager, _options, _blobFactory);
        options?.Invoke(builder);

        return this;
    }

    public IAssetsBuilder WithFolder(string name, Action<IAssetsBuilder>? options = null)
    {
        return WithFolder(name, out var contentReference, options);
    }

    public IAssetsBuilder WithFolder(string name, out ContentReference contentReference, Action<IAssetsBuilder>? options = null)
    {
        contentReference = ContentReference.EmptyReference;
        if (_stop) return Empty;

        contentReference = _parent != null && !ContentReference.IsNullOrEmpty(_parent)
            ? _parent
            : SiteDefinition.Current.SiteAssetsRoot;

        var existingContent = _contentRepository
            .GetChildren<ContentFolder>(contentReference, _options.Language)
            .SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existingContent is null)
        {
            var content = _contentRepository.GetDefault<ContentFolder>(contentReference, _options.Language);
            content.Name = name;
            contentReference = _contentRepository.Save(content, _options.PublishContent ? SaveAction.Publish : SaveAction.Default, AccessLevel.NoAccess);
        }
        else
        {
            contentReference = existingContent.ContentLink;
        }

        if (options == null)
            return this;

        var builder = new AssetsBuilder(contentReference, _contentRepository, _contentBuilderManager, _options, _blobFactory);
        options?.Invoke(builder);

        return this;
    }

    public IAssetsBuilder WithMedia<T>(Action<T>? value = null, Stream? stream = null, string? extension = null) where T : MediaData
    {
        return WithMedia(out var contentReference, value, stream, extension);
    }

    public IAssetsBuilder WithMedia<T>(out ContentReference contentReference, Action<T>? value = null, Stream? stream = null, string? extension = null) where T : MediaData
    {
        contentReference = ContentReference.EmptyReference;
        if (_stop) return Empty;

        contentReference = _parent is not null && !ContentReference.IsNullOrEmpty(_parent)
            ? _parent
            : SiteDefinition.Current.SiteAssetsRoot;

        var media = _contentRepository.GetDefault<T>(contentReference, _options.Language);
        value?.Invoke(media);

        var existingItem = _contentRepository
            .GetChildren<T>(contentReference, _options.Language)
            .FirstOrDefault(x => x.Name.Equals(media.Name, StringComparison.OrdinalIgnoreCase));

        if (existingItem != null)
        {
            contentReference = existingItem.ContentLink;
            return this;
        }

        if (stream is not null && !string.IsNullOrEmpty(extension))
        {
            var blob = _blobFactory.CreateBlob(media.BinaryDataContainer, extension);

            blob.Write(stream);
            media.BinaryData = blob;
        }

        contentReference = _contentRepository.Save(media, _options.PublishContent ? SaveAction.Publish : SaveAction.Default, AccessLevel.NoAccess);

        return this;
    }

    #endregion

    #region Private methods

    private IAssetsBuilder WithBlock<T>(bool privateMarket, string name, out ContentReference contentReference, Action<T>? value = null, CultureInfo? translationLanguage = null, string? translatedName = null, Action<T>? translation = null) where T : IContentData
    {
        contentReference = ContentReference.EmptyReference;
        if (_stop) return Empty;

        contentReference = _parent != null && !ContentReference.IsNullOrEmpty(_parent)
            ? _parent
            : SiteDefinition.Current.SiteAssetsRoot;
        var existingBlock = _contentRepository
            .GetChildren<T>(contentReference, _options.Language)
            .SingleOrDefault(x => ((IContent)x).Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existingBlock is not null)
        {
            contentReference = ((IContent)existingBlock).ContentLink;
            return this;
        }

        var block = _contentRepository.GetDefault<T>(contentReference, _options.Language);

        PropertyHelpers.InitProperties(block);
        value?.Invoke(block);

        var iContent = (IContent)block;

        _contentBuilderManager.SetContentName<T>(iContent, name);
        contentReference = _contentRepository.Save(iContent, _options.PublishContent ? SaveAction.Publish : SaveAction.Default, AccessLevel.NoAccess);

        CreateTranslation(contentReference, translationLanguage, translatedName, translation);

        return this;
    }

    private void CreateTranslation<T>(ContentReference contentReference, CultureInfo? translationLanguage, string? translatedName = null, Action<T>? translation = null) where T : IContentData
    {
        if (ContentReference.IsNullOrEmpty(contentReference) || translationLanguage == null || string.IsNullOrEmpty(translatedName) || translation == null)
            return;

        _contentBuilderManager.CreateAndEnableLanguage(translationLanguage);

        var translatedBlock = _contentRepository.CreateLanguageBranch<T>(contentReference, translationLanguage);
        translation?.Invoke(translatedBlock);

        var iContent = (IContent)translatedBlock;

        _contentBuilderManager.SetContentName<T>(iContent, translatedName);
        _ = _contentRepository.Save(iContent, _options.PublishContent ? SaveAction.Publish : SaveAction.Default, AccessLevel.NoAccess);
    }

    #endregion
}
