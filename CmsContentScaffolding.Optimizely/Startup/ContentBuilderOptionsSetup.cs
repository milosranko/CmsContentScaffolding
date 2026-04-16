using CmsContentScaffolding.Optimizely.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CmsContentScaffolding.Optimizely.Startup;

public class ContentBuilderOptionsSetup : IConfigureOptions<ContentBuilderOptions>
{
    public const string SectionName = "CmsContentScaffolding";
    private readonly IConfiguration _configuration;

    public ContentBuilderOptionsSetup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(ContentBuilderOptions options)
    {
        _configuration
            .GetSection(SectionName)
            .Bind(options);
    }
}
