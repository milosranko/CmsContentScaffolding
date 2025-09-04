using CmsContentScaffolding.Optimizely.Models;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Framework.Blobs;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace CmsContentScaffolding.Optimizely.Helpers;

public static class PropertyHelpers
{
    #region Private fields

    private static readonly Injected<ContentBuilderOptions> _options = default;
    private static readonly Injected<IContentRepository> _contentRepository = default;
    private static readonly Injected<IBlobFactory> _blobFactory = default;

    #endregion

    #region Public fields

    public static IDictionary<Type, PropertyInfo[]> TypeProperties = new Dictionary<Type, PropertyInfo[]>();

    #endregion

    #region Public methods

    public static ContentReference GetOrAddMedia<TMedia>(string name, string extension, Stream stream) where TMedia : MediaData
    {
        var mediaFolder = ContentReference.IsNullOrEmpty(SiteDefinition.Current.SiteAssetsRoot)
            ? SiteDefinition.Current.GlobalAssetsRoot
            : SiteDefinition.Current.SiteAssetsRoot;
        var existingItems = _contentRepository.Service
            .GetChildren<TMedia>(mediaFolder)
            .Where(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existingItems != null && existingItems.Any())
            return existingItems.ElementAt(0).ContentLink;

        var image = _contentRepository.Service.GetDefault<TMedia>(mediaFolder);
        var blob = _blobFactory.Service.CreateBlob(image.BinaryDataContainer, extension);

        blob.Write(stream);
        image.BinaryData = blob;
        image.Name = name;

        return _contentRepository.Service.Save(image, _options.Service.PublishContent ? SaveAction.Publish : SaveAction.Default, AccessLevel.NoAccess);
    }

    public static void InitProperties<T>(T content) where T : IContentData
    {
        var type = typeof(T);

        if (!TypeProperties.ContainsKey(type))
            TypeProperties.Add(type, type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.PropertyType.Name.Equals(nameof(ContentArea)) || x.PropertyType.Name.Equals(nameof(XhtmlString)))
                .ToArray());

        for (int i = 0; i < TypeProperties[type].Length; i++)
            TypeProperties[type][i].SetValue(content, TypeProperties[type][i].PropertyType.Name.Equals(nameof(ContentArea))
                ? new ContentArea()
                : new XhtmlString());
    }

    #endregion
}
