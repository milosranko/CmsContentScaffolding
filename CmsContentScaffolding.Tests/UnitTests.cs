using CmsContentScaffolding.Optimizely.Interfaces;
using CmsContentScaffolding.Optimizely.Models;
using CmsContentScaffolding.Optimizely.Startup;
using CmsContentScaffolding.Optimizely.Tests.Extensions;
using CmsContentScaffolding.Optimizely.Tests.Models.Blocks;
using CmsContentScaffolding.Optimizely.Tests.Models.Pages;
using EPiServer;
using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Filters;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Globalization;
using static CmsContentScaffolding.Optimizely.Tests.Constants.StringConstants;

namespace CmsContentScaffolding.Optimizely.Tests;

[TestClass]
public class UnitTests
{
    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        var builder = Host
            .CreateDefaultBuilder()
            .ConfigureCmsDefaults()
            .ConfigureAppConfiguration((context, config) =>
            {
                config
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddConfiguration(context.Configuration)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.unittest.json", false, true)
                .Build();
            })
            .ConfigureServices((context, services) =>
            {
                services
                .Configure<SchedulerOptions>(o => o.Enabled = false)
                .AddSingleton<IHttpContextFactory, DefaultHttpContextFactory>()
                .AddCmsAspNetIdentity<ApplicationUser>()
                .AddCms()
                .AddCmsContentScaffolding<StartPage>(context.Configuration);

                Globals.Services = services.BuildServiceProvider();

                var dbContext = Globals.Services.GetRequiredService<ApplicationDbContext<ApplicationUser>>();
                dbContext.Database.EnsureCreated();
            })
            .ConfigureWebHostDefaults(config =>
            {
                config.UseUrls(Site1HostUrl, Site2HostUrl);
                config.Configure(app =>
                {
                    app.AddCmsContent();
                });
            });

        builder.Build().Start();
    }

    [ClassCleanup]
    public static void Uninitialize()
    {
        var dbContext = Globals.Services.GetRequiredService<ApplicationDbContext<ApplicationUser>>();
        dbContext.Database.EnsureDeleted();
    }

    [TestMethod]
    public void InitializationTest_ShouldGetStartPage()
    {
        //Arrange
        var contentLoader = ServiceLocator.Current.GetRequiredService<IContentLoader>();
        var siteDefinitionRepository = ServiceLocator.Current.GetRequiredService<ISiteDefinitionRepository>();

        //Act
        var pages = contentLoader.GetDescendents(ContentReference.RootPage);
        var siteDefinition = siteDefinitionRepository
            .List()
            .Where(x => x.GetHosts(Language, false).Any())
            .Single();
        var startPage = contentLoader.Get<StartPage>(siteDefinition.StartPage, Language);

        //Assert
        Assert.IsNotNull(pages);
        Assert.IsTrue(pages.Any());
        Assert.IsNotNull(startPage);
        Assert.IsNotNull(startPage.MainContentArea);
        Assert.IsFalse(startPage.MainContentArea.IsEmpty);
    }

    [TestMethod]
    public void SiteDefinitions_ShouldHaveDefaultSiteDefinition()
    {
        //Arrange
        var siteDefinitionRepository = ServiceLocator.Current.GetRequiredService<ISiteDefinitionRepository>();

        //Act
        var siteDefinitions = siteDefinitionRepository.List();

        //Assert
        Assert.IsTrue(siteDefinitions.Any());
    }

    [TestMethod]
    public void PerformanceTest_ShouldGetAllArticlePagesUsingPageCriteriaQueryService()
    {
        //Arrange
        var contentTypeRepository = ServiceLocator.Current.GetInstance<IContentTypeRepository>();
        var pageCriteriaQueryService = ServiceLocator.Current.GetInstance<IPageCriteriaQueryService>();
        var criterias = new PropertyCriteriaCollection
        {
            new PropertyCriteria
            {
                Name = "PageTypeID",
                Type = PropertyDataType.PageType,
                Condition = CompareCondition.Equal,
                Value = contentTypeRepository.Load<ArticlePage>().ID.ToString(),
                Required = true
            }
        };

        //Act
        var res = pageCriteriaQueryService.FindAllPagesWithCriteria(
            ContentReference.RootPage,
            criterias,
            Language.TwoLetterISOLanguageName,
            LanguageSelector.MasterLanguage());

        //Assert
        Assert.IsNotNull(res);
        Assert.IsTrue(res.Count > 100);
    }

    [TestMethod]
    public void PerformanceTest_ShouldGetAllArticlePagesUsingContentLoader()
    {
        //Arrange
        var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

        //Act
        var res = contentLoader
            .GetDescendents(ContentReference.RootPage)
            .Where(x =>
            {
                if (contentLoader.TryGet<PageData>(x, out var page))
                {
                    return page is ArticlePage;
                }

                return false;
            })
            .ToArray();

        //Assert
        Assert.IsNotNull(res);
        Assert.IsTrue(res.Length > 100);
    }

    [TestMethod]
    public void ArticlePageBlocksTest_ShouldGetAllBlocksFromMainContentArea()
    {
        //Arrange
        var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

        //Act
        var res = contentLoader
            .GetChildren<ArticlePage>(ContentReference.RootPage, Language)
            .Where(x => x.MainContentArea != null && x.MainContentArea.Count > 0)
            .ToArray();

        //Assert
        Assert.IsNotNull(res);
        Assert.IsTrue(res.Length > 0);
    }

    [TestMethod]
    public void LocalBlockTest_ShouldHaveValues()
    {
        //Arrange
        var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

        //Act
        var res = contentLoader
            .GetChildren<NotFoundPage>(ContentReference.RootPage, Language)
            .Single();

        //Assert
        Assert.IsNotNull(res);
        Assert.IsNotNull(res.Teaser);
        Assert.IsNotNull(res.Teaser.Heading);
        Assert.IsFalse(ContentReference.IsNullOrEmpty(res.Teaser.Image));
        Assert.IsNotNull(res.Teaser.LinkButton);
        Assert.IsFalse(string.IsNullOrEmpty(res.Teaser.LinkButton.LinkText));
        Assert.IsNotNull(res.Teaser.LinkButton.LinkUrl);
    }

    [TestMethod]
    public void StartPage_ShouldHavePublishedVersion_SoItSurvivesRestart()
    {
        //Arrange
        var contentLoader = ServiceLocator.Current.GetRequiredService<IContentLoader>();
        var siteDefinitionRepository = ServiceLocator.Current.GetRequiredService<ISiteDefinitionRepository>();
        var site = siteDefinitionRepository.Get("Site 1");

        //Act
        var startPage = contentLoader.Get<StartPage>(site.StartPage, Language);
        var status = ((IVersionable)startPage).Status;

        //Assert: the site's start page must resolve to a Published version. A live
        // request after an application restart serves the published version only;
        // if the start page exists solely as a draft its content appears lost.
        Assert.AreEqual(VersionStatus.Published, status, "Start page is not Published - it only exists as a draft, so its content is lost after restart.");
        Assert.IsNotNull(startPage.MainContentArea);
        Assert.IsFalse(startPage.MainContentArea.IsEmpty, "Published start page MainContentArea is empty.");
    }

    [TestMethod]
    public void StartPage_ShouldNotBeWipedOrRepublished_OnAppendRestart()
    {
        //Arrange
        var contentBuilder = ServiceLocator.Current.GetRequiredService<IContentBuilder>();
        var contentLoader = ServiceLocator.Current.GetRequiredService<IContentLoader>();
        var siteDefinitionRepository = ServiceLocator.Current.GetRequiredService<ISiteDefinitionRepository>();
        var options = ServiceLocator.Current.GetRequiredService<IOptionsMonitor<ContentBuilderOptions>>();

        var site = siteDefinitionRepository.Get("Site 1");
        var before = contentLoader.Get<StartPage>(site.StartPage, Language);
        var beforeContentCount = before.MainContentArea.Count;
        Assert.IsTrue(beforeContentCount > 0, "Precondition: start page should already have MainContentArea items.");

        var snapshot = new ContentBuilderOptions();
        snapshot.ApplyFrom(options.CurrentValue);
        var originalCurrentSite = SiteDefinition.Current;

        //Act - simulate an application restart that re-runs scaffolding in Append mode,
        // where the start-page lambda does not repopulate the MainContentArea.
        try
        {
            options.CurrentValue.SiteName = "Site 1";
            options.CurrentValue.SiteHost = Site1HostUrl;
            options.CurrentValue.Language = Language;
            options.CurrentValue.StartPageType = typeof(StartPage);
            options.CurrentValue.BuildMode = BuildMode.Append;
            options.CurrentValue.PublishContent = false;
            options.CurrentValue.SkipValidation = true;

            contentBuilder.Init();
            contentBuilder
                .UsePages(ContentReference.RootPage)
                .WithStartPage<StartPage>(p =>
                {
                    // Same name as the existing start page so it is matched and updated
                    // (as happens on a real restart), but the content area is not repopulated.
                    p.Name = "Home Page";
                    p.Heading = "Restart Heading";
                });
        }
        finally
        {
            options.CurrentValue.ApplyFrom(snapshot);
            SiteDefinition.Current = originalCurrentSite;
        }

        //Assert - the existing start page must be left untouched: same content and no
        // new published version with an empty MainContentArea.
        var after = contentLoader.Get<StartPage>(siteDefinitionRepository.Get("Site 1").StartPage, Language);
        Assert.AreEqual(VersionStatus.Published, ((IVersionable)after).Status);
        Assert.IsTrue(after.MainContentArea != null && !after.MainContentArea.IsEmpty, "MainContentArea was wiped on restart.");
        Assert.AreEqual(beforeContentCount, after.MainContentArea!.Count, "MainContentArea item count changed on restart.");
    }

    [TestMethod]
    public async Task GetSiteStartPage_ShouldReturnHtml()
    {
        //Arrange
        var client = new HttpClient
        {
            BaseAddress = new Uri(Site1HostUrl)
        };

        //Act
        var res = await client.GetAsync("/");

        //Assert
        Assert.IsNotNull(res);
        client.Dispose();
    }

    [TestMethod]
    public async Task GetSiteArticlePage_ShouldReturnHtml()
    {
        //Arrange
        var client = new HttpClient
        {
            BaseAddress = new Uri(Site1HostUrl)
        };

        //Act
        var res = await client.GetAsync("/article1");

        //Assert
        Assert.IsNotNull(res);
        client.Dispose();
    }

    [TestMethod]
    public void GetBlocksFromFolder_ShouldReturnBlocks()
    {
        //Arrange
        var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
        var siteDefinitionRepository = ServiceLocator.Current.GetRequiredService<ISiteDefinitionRepository>();
        var res = contentLoader.GetChildren<ContentFolder>(siteDefinitionRepository.Get("Site 1").SiteAssetsRoot, Language);

        //Act
        var blocks = contentLoader.GetChildren<BlockData>(res.First().ContentLink, Language);

        //Assert
        Assert.IsNotNull(res);
        Assert.IsNotNull(blocks);
        Assert.IsTrue(blocks.Any());
    }

    [TestMethod]
    public void GetTranslatedBlockFromFolder_ShouldReturnTranslatedBlock()
    {
        //Arrange
        var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
        var siteDefinitionRepository = ServiceLocator.Current.GetRequiredService<ISiteDefinitionRepository>();
        var res = contentLoader.GetChildren<ContentFolder>(siteDefinitionRepository.Get("Site 1").SiteAssetsRoot, Language);

        //Act
        var block = contentLoader
            .GetChildren<TeaserBlock>(res.First().ContentLink, CultureInfo.GetCultureInfo("fr"))
            .FirstOrDefault();

        //Assert
        Assert.IsNotNull(res);
        Assert.IsNotNull(block);
        Assert.IsNotNull(block.Heading);
    }

    [TestMethod]
    public void GetTranslatedStartPage_ShouldReturnTranslatedPage()
    {
        //Arrange
        var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
        var siteDefinitionRepository = ServiceLocator.Current.GetRequiredService<ISiteDefinitionRepository>();

        //Act
        var page = contentLoader.Get<StartPage>(siteDefinitionRepository.Get("Site 1").StartPage, CultureInfo.GetCultureInfo("sv"));

        //Assert
        Assert.IsNotNull(page);
        Assert.IsTrue(page.Name.Equals("Start Page [SV]", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void SkipValidation_ShouldAllowSavingPageWithMissingRequiredProperties()
    {
        //Arrange
        var contentBuilder = ServiceLocator.Current.GetRequiredService<IContentBuilder>();
        var contentLoader = ServiceLocator.Current.GetRequiredService<IContentLoader>();
        var options = ServiceLocator.Current.GetRequiredService<IOptionsMonitor<ContentBuilderOptions>>();
        var originalSkipValidation = options.CurrentValue.SkipValidation;
        var originalBuildMode = options.CurrentValue.BuildMode;
        var pageRef = ContentReference.EmptyReference;

        //Act
        try
        {
            options.CurrentValue.SkipValidation = true;
            options.CurrentValue.BuildMode = BuildMode.Append;
            contentBuilder.Init();

            contentBuilder
                .UsePages(ContentReference.RootPage)
                .WithPage<StartPage>(out pageRef, p =>
                {
                    p.Name = "SkipValidationStartPage";
                    // Heading is [Required] but intentionally not set
                });
        }
        finally
        {
            options.CurrentValue.SkipValidation = originalSkipValidation;
            options.CurrentValue.BuildMode = originalBuildMode;
        }

        //Assert
        Assert.IsFalse(ContentReference.IsNullOrEmpty(pageRef));
        var savedPage = contentLoader.Get<StartPage>(pageRef, Language);
        Assert.IsNotNull(savedPage);
        Assert.AreEqual("SkipValidationStartPage", savedPage.Name);
        Assert.IsNull(savedPage.Heading);
    }

    [TestMethod]
    public void GetAssetsFromSite1StartPage_ShouldReturnAssets()
    {
        //Arrange
        var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
        var siteDefinitionRepository = ServiceLocator.Current.GetRequiredService<ISiteDefinitionRepository>();
        var assetsHelper = ServiceLocator.Current.GetRequiredService<ContentAssetHelper>();
        var site = siteDefinitionRepository.Get("Site 1");

        //Act
        var res = contentLoader.GetChildren<IContentData>(assetsHelper.GetAssetFolder(site.StartPage).ContentLink, Language);

        //Assert
        Assert.IsNotNull(res);
        Assert.IsTrue(res.Any());
    }
}