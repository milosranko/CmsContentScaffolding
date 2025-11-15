using CmsContentScaffolding.Optimizely.Extensions;
using CmsContentScaffolding.Optimizely.Helpers;
using CmsContentScaffolding.Optimizely.Models;
using CmsContentScaffolding.Optimizely.Startup;
using CmsContentScaffolding.Optimizely.Tests.Models.Blocks;
using CmsContentScaffolding.Optimizely.Tests.Models.Media;
using CmsContentScaffolding.Optimizely.Tests.Models.Pages;
using CmsContentScaffolding.Shared.Resources;
using EPiServer;
using EPiServer.Core;
using EPiServer.Security;
using Microsoft.AspNetCore.Builder;
using System.Globalization;
using static CmsContentScaffolding.Optimizely.Tests.Constants.StringConstants;

namespace CmsContentScaffolding.Optimizely.Tests.Extensions;

internal static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder AddCmsContent(this IApplicationBuilder app)
    {
        app.UseCmsContentScaffolding(
            builderOptions: o =>
            {
                o.SiteName = "Site 1";
                o.SiteHost = Site1HostUrl;
                o.Language = CultureInfo.GetCultureInfo("sr");
                o.StartPageType = typeof(StartPage);
                o.BuildMode = BuildMode.Append;
                o.PublishContent = true;
                o.Roles = new Dictionary<string, AccessLevel>
                {
                    { Site1EditorsRole, AccessLevel.Read | AccessLevel.Create | AccessLevel.Edit }
                };
                o.Users =
                [
                    new("Site1User", "Site1User@test.com", TestUserPassword, [Site1EditorsRole])
                ];
            },
            builder: b =>
            {
                var teaser2Ref = ContentReference.EmptyReference;
                var teaser3Ref = ContentReference.EmptyReference;
                var articlePageRef = ContentReference.EmptyReference;

                b.UseAssets(ContentReference.SiteBlockFolder)
                .WithFolder("Folder 1", l1 =>
                {
                    l1
                    .WithFolder("Folder 1_1", l2 =>
                    {
                        l2.WithBlock<TeaserBlock>("Teaser 2", out teaser2Ref, x => x.Heading = "Test");
                    })
                    .WithMedia<VideoFile>(x => x.Name = "Test video", ResourceHelpers.GetVideoStream(), ".mp4")
                    .WithBlock<TeaserBlock>(
                        "Teaser 3",
                        out _,
                        x => x.Heading = "Test",
                        CultureInfo.GetCultureInfo("fr"),
                        "Teaser 3 [FR]",
                        t => { t.Heading = "Test [FR]"; });
                })
                .WithContent<ContentFolder>(x => x.Name = "Folder1")
                .WithContent<ImageFile>(x => x.Name = "Image 1")
                .WithBlock<TeaserBlock>("Teaser 1", x => x.Heading = "Test");

                b.UsePages(ContentReference.RootPage)
                .WithStartPage<StartPage>(p =>
                {
                    p.Name = "Home Page";
                    p.Heading = "Test";
                    p.OpenGraphImage = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
                    p.MainContentArea
                    .AddExistingItems(teaser2Ref, teaser3Ref)
                    .AddItem<TeaserBlock>("Start Page Teaser", b =>
                    {
                        b.Heading = ResourceHelpers.Faker.Lorem.Slug();
                        b.LeadText = ResourceHelpers.Faker.Lorem.Paragraph();
                        b.Image = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 2", ".png", ResourceHelpers.GetImageStream());
                    });
                }, CultureInfo.GetCultureInfo("sv"), t =>
                {
                    t.Name = "Start Page [SV]";
                    t.Heading = "Test Heading [SV]";
                }, l1 =>
                {
                    l1
                    .WithPage<ArticlePage>(out articlePageRef, p =>
                    {
                        p.Name = "article1";
                        p.Heading = ResourceHelpers.Faker.Lorem.Slug();
                        p.LeadText = ResourceHelpers.Faker.Lorem.Paragraph();
                        p.MainContent
                        .AddStringFragment(ResourceHelpers.Faker.Lorem.Paragraphs())
                        .AddContentFragment(PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream()))
                        .AddStringFragment(ResourceHelpers.Faker.Lorem.Paragraphs());
                        p.TopImage = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
                        p.MainContentArea
                        .AddItem<AccordionContainerBlock>("Accordion Container", b =>
                        {
                            b.Heading = ResourceHelpers.Faker.Lorem.Slug();
                            b.Items.AddItems<AccordionItemBlock>("Accordion Item", b1 =>
                            {
                                b1.Heading = ResourceHelpers.Faker.Lorem.Slug();
                                b1.Image = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
                                b1.LeadText = ResourceHelpers.Faker.Lorem.Paragraph();
                            }, 5);
                        })
                        .AddItem<ImageFile>(options: i =>
                        {
                            i.Name = "Test Image";
                            i.ContentLink = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
                        })
                        .AddExistingItem(teaser3Ref);
                    }, l2 =>
                    {
                        l2
                        .WithPage<ArticlePage>(p =>
                        {
                            p.Name = "Article2_1";
                            p.Heading = ResourceHelpers.Faker.Lorem.Slug();
                            p.LeadText = ResourceHelpers.Faker.Lorem.Paragraph();
                            p.MainContent.AddStringFragment(ResourceHelpers.Faker.Lorem.Paragraphs());
                        })
                        .WithPage<ArticlePage>(p =>
                        {
                            p.Name = "Article 22";
                            p.Heading = ResourceHelpers.Faker.Lorem.Slug();
                            p.LeadText = ResourceHelpers.Faker.Lorem.Paragraph();
                        }, CultureInfo.GetCultureInfo("sv"), t =>
                        {
                            t.Name = "Article 22 [SV]";
                            t.Heading = ResourceHelpers.Faker.Lorem.Slug();
                            t.LeadText = ResourceHelpers.Faker.Lorem.Paragraph();
                        }, l3 =>
                        {
                            l3.WithPages<ArticlePage>(p =>
                            {
                                p.Heading = ResourceHelpers.Faker.Lorem.Slug();
                                p.LeadText = ResourceHelpers.Faker.Lorem.Paragraph();
                                p.MainContent.AddStringFragment(ResourceHelpers.Faker.Lorem.Paragraphs());
                            }, 20);
                        });
                    })
                    .WithPages<ArticlePage>(p =>
                    {
                        p.Heading = ResourceHelpers.Faker.Lorem.Slug();
                        p.LeadText = ResourceHelpers.Faker.Lorem.Paragraph();
                        p.MainContent.AddStringFragment(ResourceHelpers.Faker.Lorem.Paragraphs(10));
                        p.MainContentArea.AddItem<TeaserBlock>(p.Name);
                    }, 100);
                })
                .WithPage<NotFoundPage>(out var notFoundPageRef, p =>
                {
                    p.Name = "Not Found Page";
                    p.Teaser.Heading = ResourceHelpers.Faker.Lorem.Slug(3);
                    p.Teaser.Image = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
                    p.Teaser.LeadText = ResourceHelpers.Faker.Lorem.Paragraph();
                    p.Teaser.LinkButton.LinkText = ResourceHelpers.Faker.Internet.DomainName();
                    p.Teaser.LinkButton.LinkUrl = new Url(ResourceHelpers.Faker.Internet.Url());
                })
                .WithPages<ArticlePage>(p =>
                {
                    p.Name = ResourceHelpers.Faker.Lorem.Slug(2);
                    p.MainContentArea
                    .AddItems<TeaserBlock>(block =>
                    {
                        block.Heading = ResourceHelpers.Faker.Lorem.Slug();
                        block.LeadText = ResourceHelpers.Faker.Lorem.Paragraph();
                        block.Image = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
                    }, 10);
                }, 10)
                .WithPages<ArticlePage>(p =>
                {
                    p.Name = ResourceHelpers.Faker.Lorem.Slug(3);
                    p.MainContentArea
                    .AddItems<TeaserBlock>(block =>
                    {
                        block.Heading = ResourceHelpers.Faker.Lorem.Slug();
                        block.LeadText = ResourceHelpers.Faker.Lorem.Paragraph();
                        block.Image = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
                    }, 2);
                }, 2)
                .WithPage<StartPage>(p =>
                {
                    p.Name = "Home Page 1";
                    p.Heading = "Test";
                    p.MainArticlePageReference = articlePageRef;
                    p.NotFoundPageReference = notFoundPageRef;
                });
            });

        app.UseCmsContentScaffolding(
            builderOptions: o =>
            {
                o.SiteName = "Site 2";
                o.SiteHost = "https://localhost:5001";
                o.StartPageType = typeof(StartPage);
                o.Language = CultureInfo.GetCultureInfo("en");
                o.BuildMode = BuildMode.Overwrite;
                o.PublishContent = true;
                o.Roles = new Dictionary<string, AccessLevel>
                {
                    { Site2EditorsRole, AccessLevel.Read | AccessLevel.Create | AccessLevel.Edit }
                };
                o.Users =
                [
                    new("Site2User", "Site2User@test.com", TestUserPassword, [Site2EditorsRole])
                ];
            },
            builder: b =>
            {
                b.UsePages()
                .WithStartPage<StartPage>(p =>
                {
                    p.Name = "Home Page";
                    p.Heading = "Test";
                    p.OpenGraphImage = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
                    p.MainContentArea
                    .AddItems<TeaserBlock>("Teaser Test", b =>
                    {
                        b.Heading = ResourceHelpers.Faker.Lorem.Slug();
                        b.LeadText = ResourceHelpers.Faker.Lorem.Paragraph();
                        b.Image = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
                    }, 3);
                }, l1 =>
                {
                    l1.WithPage<ArticlePage>();
                })
                .WithPage<NotFoundPage>(p =>
                {
                    p.Name = "NotFoundPage";
                    p.Teaser.Heading = ResourceHelpers.Faker.Lorem.Slug();
                    p.Teaser.Image = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
                    p.Teaser.LeadText = ResourceHelpers.Faker.Lorem.Paragraph();
                    p.Teaser.LinkButton.LinkText = ResourceHelpers.Faker.Internet.DomainName();
                    p.Teaser.LinkButton.LinkUrl = new Url(ResourceHelpers.Faker.Internet.Url());
                })
                .WithPages<ArticlePage>(p =>
                {
                    p.Name = "Article2";
                    p.MainContentArea
                    .AddItems<TeaserBlock>(block =>
                    {
                        block.Heading = ResourceHelpers.Faker.Lorem.Slug();
                        block.LeadText = ResourceHelpers.Faker.Lorem.Paragraph();
                        block.Image = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
                    }, 10);
                }, 10)
                .WithPages<ArticlePage>(p =>
                {
                    p.Name = "Articles4";
                    p.MainContentArea
                    .AddItems<TeaserBlock>(block =>
                    {
                        block.Heading = ResourceHelpers.Faker.Lorem.Slug();
                        block.LeadText = ResourceHelpers.Faker.Lorem.Paragraph();
                        block.Image = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
                    }, 2);
                }, 2);
            });

        return app;
    }
}
