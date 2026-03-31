using CmsContentScaffolding.Optimizely.Models;
using EPiServer.Core;
using EPiServer.Security;
using EPiServer.Web;
using System.Globalization;

namespace CmsContentScaffolding.Optimizely.Interfaces;

/// <summary>
/// ContentBuilderManager used internaly to support builders
/// </summary>
internal interface IContentBuilderManager
{
    bool SiteExists { get; }
    SiteDefinition GetOrCreateSite();
    void SetStartPageSecurity(ContentReference pageRef);
    void ApplyDefaultLanguage();
    void CreateAndEnableLanguage(CultureInfo culture);
    void AppendLanguageToExistingLanguages(ContentReference contentReference, CultureInfo language);
    void CreateDefaultRoles(IDictionary<string, AccessLevel> roles);
    void CreateRoles(IDictionary<string, AccessLevel>? roles);
    void CreateUsers(IEnumerable<UserModel>? users);
    void SetContentName<T>(IContent content, string? name = default, string? nameSuffix = default) where T : IContentData;
    ContentReference CreateItem<T>(string? name = default, string? suffix = default, Action<T>? options = default) where T : IContentData;
}
