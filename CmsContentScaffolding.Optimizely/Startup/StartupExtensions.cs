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

    public static IServiceCollection AddCmsContentScaffolding(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .ConfigureOptions<ContentBuilderOptionsSetup>()
            .AddTransient<IContentBuilderManager, ContentBuilderManager>()
            .AddTransient<IContentBuilder, ContentBuilder>();
    }

    public static IApplicationBuilder UseCmsContentScaffolding(
        this IApplicationBuilder app,
        Action<IContentBuilder> builder,
        Action<ContentBuilderOptions>? builderOptions = null)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptionsMonitor<ContentBuilderOptions>>();

        builderOptions?.Invoke(options.CurrentValue);
        //else
        //{
        //    var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        //    var contentBuilderOptionsFromConfiguration = configuration.GetSection(SectionName).Get<ContentBuilderOptions>();

        //    if (contentBuilderOptionsFromConfiguration is not null)
        //        options.CurrentValue.ApplyFrom(contentBuilderOptionsFromConfiguration);
        //}

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
        //var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        //var contentBuilderOptionsFromConfiguration = configuration.GetSection(SectionName).Get<ContentBuilderOptions>();

        //if (contentBuilderOptionsFromConfiguration is not null)
        //    options.CurrentValue.ApplyFrom(contentBuilderOptionsFromConfiguration);

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
