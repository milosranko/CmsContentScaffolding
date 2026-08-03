using CmsContentScaffolding.Optimizely.Builders;
using CmsContentScaffolding.Optimizely.Interfaces;
using CmsContentScaffolding.Optimizely.Managers;
using CmsContentScaffolding.Optimizely.Models;
using EPiServer.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CmsContentScaffolding.Optimizely.Startup;

public static class StartupExtensions
{
    public const string SectionName = "CmsContentScaffolding";

    public static IServiceCollection AddCmsContentScaffolding<TStartPage>(
        this IServiceCollection services,
        IConfiguration configuration) where TStartPage : PageData
    {
        return services
            .ConfigureOptions<ContentBuilderOptionsSetup<TStartPage>>()
            .AddTransient<IContentBuilderManager, ContentBuilderManager>()
            .AddTransient<IContentBuilder, ContentBuilder>();
    }

    /// <summary>
    /// Repoints the existing primary site definition at the configured SiteHost, SiteName and Language.
    /// Useful when a database is transferred from one environment to another. Does nothing when no primary
    /// site exists or when its primary host already matches the configured SiteHost.
    /// Call before <see cref="UseCmsContentScaffolding(IApplicationBuilder, Action{IContentBuilder}, Action{ContentBuilderOptions}?)"/>,
    /// which resolves the site by its host name. Note that options set through <paramref name="builderOptions"/>
    /// are applied to the shared options instance and therefore also apply to any subsequent call.
    /// </summary>
    public static IApplicationBuilder ReplacePrimarySiteHost(
        this IApplicationBuilder app,
        Action<ContentBuilderOptions>? builderOptions = null)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptionsMonitor<ContentBuilderOptions>>();

        builderOptions?.Invoke(options.CurrentValue);

        var contentBuilderManager = app.ApplicationServices.GetRequiredService<IContentBuilderManager>();

        contentBuilderManager.ReplacePrimarySite();

        return app;
    }

    public static IApplicationBuilder UseCmsContentScaffolding(
        this IApplicationBuilder app,
        Action<IContentBuilder> builder,
        Action<ContentBuilderOptions>? builderOptions = null)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptionsMonitor<ContentBuilderOptions>>();

        builderOptions?.Invoke(options.CurrentValue);

        using var contentBuilder = app.ApplicationServices.GetRequiredService<IContentBuilder>();
        contentBuilder.Init();
        builder.Invoke(contentBuilder);

        return app;
    }

    public static IApplicationBuilder UseCmsContentScaffolding<TStartPage>(
        this IApplicationBuilder app,
        Action<IContentBuilder>? builder = null,
        Action<ContentBuilderOptions>? builderOptions = null) where TStartPage : PageData
    {
        var options = app.ApplicationServices.GetRequiredService<IOptionsMonitor<ContentBuilderOptions>>();

        options.CurrentValue.StartPageType = typeof(TStartPage);
        builderOptions?.Invoke(options.CurrentValue);

        if (builder is null)
            return app;

        using var contentBuilder = app.ApplicationServices.GetRequiredService<IContentBuilder>();
        contentBuilder.Init();
        builder.Invoke(contentBuilder);

        return app;
    }
}
