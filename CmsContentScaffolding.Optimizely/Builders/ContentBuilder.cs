using CmsContentScaffolding.Optimizely.Helpers;
using CmsContentScaffolding.Optimizely.Interfaces;
using CmsContentScaffolding.Optimizely.Models;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Blobs;
using EPiServer.Web;
using Microsoft.Extensions.Options;

namespace CmsContentScaffolding.Optimizely.Builders;

internal class ContentBuilder : IContentBuilder
{
    #region Private properties

    private readonly IContentRepository _contentRepository;
    private readonly IContentBuilderManager _contentBuilderManager;
    private readonly IBlobFactory _blobFactory;
    private readonly ContentAssetHelper _contentAssetHelper;
    private readonly IOptionsMonitor<ContentBuilderOptions> _contentBuilderOptions;
    private readonly IUrlSegmentGenerator _urlSegmentGenerator;
    private bool _buildContent = false;
    private bool disposedValue;

    #endregion

    #region Constructor

    public ContentBuilder(
        IContentRepository contentRepository,
        IContentBuilderManager contentBuilderManager,
        IOptionsMonitor<ContentBuilderOptions> contentBuilderOptions,
        IBlobFactory blobFactory,
        ContentAssetHelper contentAssetHelper,
        IUrlSegmentGenerator urlSegmentGenerator)
    {
        _contentRepository = contentRepository;
        _contentBuilderManager = contentBuilderManager;
        _contentBuilderOptions = contentBuilderOptions;
        _blobFactory = blobFactory;
        _contentAssetHelper = contentAssetHelper;
        _urlSegmentGenerator = urlSegmentGenerator;
    }

    #endregion

    #region Public methods

    public IAssetsBuilder UseAssets(ContentReference? root = null)
    {
        if (_buildContent)
            return new AssetsBuilder(root ?? SiteDefinition.Current.GlobalAssetsRoot, _contentRepository, _contentBuilderManager, _contentBuilderOptions, _blobFactory);

        return AssetsBuilder.Empty;
    }

    public IPagesBuilder UsePages(ContentReference? root = null)
    {
        if (_buildContent)
            return new PagesBuilder(root ?? SiteDefinition.Current.RootPage, _contentRepository, _contentBuilderManager, _contentBuilderOptions, _contentAssetHelper, _urlSegmentGenerator);

        return PagesBuilder.Empty;
    }

    public void InitSite(ContentBuilderOptions? options = null)
    {
        if (options is not null)
            _contentBuilderOptions.CurrentValue.ApplyFrom(options);

        ApplyOptions();
        SiteDefinition.Current = _contentBuilderManager.GetOrCreateSite();
    }

    #endregion

    #region Private methods

    private void ApplyOptions()
    {
        switch (_contentBuilderOptions.CurrentValue.BuildMode)
        {
            case BuildMode.Append:
                _buildContent = true;
                break;
            case BuildMode.Overwrite:
                _buildContent = true;
                break;
            case BuildMode.OnlyIfEmpty:
                _buildContent = !_contentBuilderManager.SiteExists;
                break;
            default:
                break;
        }

        if (!_buildContent)
            return;

        _contentBuilderManager.ApplyDefaultLanguage();

        if (_contentBuilderOptions.CurrentValue.RootRolesAccessLevel is not null && _contentBuilderOptions.CurrentValue.RootRolesAccessLevel.Any())
            _contentBuilderManager.CreateDefaultRoles(_contentBuilderOptions.CurrentValue.RootRolesAccessLevel);

        _contentBuilderManager.CreateRoles(_contentBuilderOptions.CurrentValue.SiteRolesAccessLevel);
        _contentBuilderManager.CreateUsers(_contentBuilderOptions.CurrentValue.Users);
    }

    #endregion

    #region IDisposable implementation

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                PropertyHelpers.TypeProperties.Clear();
                //SiteDefinition.Current = SiteDefinition.Empty;
            }
            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~ContentBuilder()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
