using EPiServer.Security;
using System.Globalization;

namespace CmsContentScaffolding.Optimizely.Models;

/// <summary>
/// Default options for the builder
/// </summary>
public class ContentBuilderOptions
{
    /// <summary>
    /// Set language for the builder, default is English
    /// </summary>
    public CultureInfo Language { get; set; } = CultureInfo.GetCultureInfo("en");
    /// <summary>
    /// Set default host for the builder, default is http://localhost
    /// </summary>
    public string SiteHost { get; set; } = "http://localhost";
    /// <summary>
    /// Set site name, default is Demo
    /// </summary>
    public string SiteName { get; set; } = "Demo";
    /// <summary>
    /// Set type of StartPage, if not set assets won't be able to create under SiteRoot
    /// </summary>
    public Type? StartPageType { get; set; }
    /// <summary>
    /// Set build mode
    /// </summary>
    public BuildMode BuildMode { get; set; } = BuildMode.Append;
    /// <summary>
    /// Set if content should be published when created, default is False
    /// </summary>
    public bool PublishContent { get; set; } = false;
    /// <summary>
    /// Default roles that are set on the root
    /// </summary>
    public IDictionary<string, AccessLevel>? RootRolesAccessLevel { get; set; }
    /// <summary>
    /// Define new roles
    /// </summary>
    public IDictionary<string, AccessLevel>? SiteRolesAccessLevel { get; set; }
    /// <summary>
    /// Define new users thah will have an access to site instance
    /// </summary>
    public IList<UserModel>? Users { get; set; }
    /// <summary>
    /// Replaces existing primary site if it already exists. Use with caution, as it will override the existing site and all its content. Usefull when transfering DB from one environment to another.
    /// </summary>
    public bool ReplaceExistingPrimarySite { get; set; } = false;

    public void ApplyFrom(ContentBuilderOptions source)
    {
        Language = source.Language;
        SiteHost = source.SiteHost;
        SiteName = source.SiteName;
        StartPageType = source.StartPageType;
        BuildMode = source.BuildMode;
        PublishContent = source.PublishContent;
        RootRolesAccessLevel = source.RootRolesAccessLevel;
        SiteRolesAccessLevel = source.SiteRolesAccessLevel;
        Users = source.Users;
    }
}
