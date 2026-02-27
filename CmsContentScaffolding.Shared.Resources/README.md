# Optimizely CMS Content Scaffolding Shared Resources

This Nuget package containes shared resources for the Optimizely CMS Content Scaffolding library, such as helper classes and example content builders. The resources in this package are used by the Optimizely CMS Content Scaffolding library and can also be used by developers when building their own content scaffolding builders.

## Example

app.UseCmsContentScaffolding(
    builderOptions: o =>
    {
        o.Language = CultureInfo.GetCultureInfo("sr");
        o.StartPageType = typeof(StartPage);
        o.BuildMode = BuildMode.Append;
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
                l2.WithBlock<TeaserBlock>("Teaser 1", out teaser2Ref, x =>
                {
                    x.Heading = "Teaser 1 Heading";
                    x.Text = ResourceHelpers.Faker.Lorem.Paragraph();
                    x.Image = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 2", ".png", ResourceHelpers.GetImageStream());
                });
            })
            .WithMedia<VideoFile>(x => x.Name = "Test video", ResourceHelpers.GetVideoStream(), ".mp4")
            .WithBlock<TeaserBlock>("Teaser 3", out teaser3Ref, x =>
            {
                x.Heading = "Test";
                x.Text = ResourceHelpers.Faker.Lorem.Paragraph();
                x.Image = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 2", ".png", ResourceHelpers.GetImageStream());
            });
        })
        .WithContent<ContentFolder>(x => x.Name = "Folder1")
        .WithContent<ImageFile>(x => x.Name = "Image 1")
        .WithBlock<TeaserBlock>("Teaser 2", x =>
        {
            x.Heading = "Test";
            x.Text = ResourceHelpers.Faker.Lorem.Paragraph();
            x.Image = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 2", ".png", ResourceHelpers.GetImageStream());
        });


        b.UsePages(ContentReference.RootPage)
        .WithStartPage<StartPage>(p =>
        {
            p.Name = "Home Page";
            p.MainContentArea
            .AddExistingItems(teaser2Ref, teaser3Ref)
            .AddItem<TeaserBlock>("Start Page Teaser", b =>
            {
                b.Heading = ResourceHelpers.Faker.Lorem.Slug();
                b.Text = ResourceHelpers.Faker.Lorem.Paragraph();
                b.Image = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 2", ".png", ResourceHelpers.GetImageStream());
            });
        }, CultureInfo.GetCultureInfo("sv"), t =>
        {
            t.Name = "Start Page [SV]";
        }, l1 =>
        {
            l1
            .WithPage<ArticlePage>(out articlePageRef, p =>
            {
                p.Name = "article1";
                p.MetaTitle = ResourceHelpers.Faker.Lorem.Slug();
                p.TeaserText = ResourceHelpers.Faker.Lorem.Paragraph();
                p.MainBody
                .AddStringFragment(ResourceHelpers.Faker.Lorem.Paragraphs())
                .AddContentFragment(PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream()))
                .AddStringFragment(ResourceHelpers.Faker.Lorem.Paragraphs());
                p.PageImage = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
                p.MainContentArea
                .AddItem<JumbotronBlock>("Jumbotron Block", b =>
                {
                    b.ButtonText = ResourceHelpers.Faker.Lorem.Slug();
                    b.ButtonLink = new Url(ResourceHelpers.Faker.Internet.Url());
                    b.Heading = ResourceHelpers.Faker.Lorem.Slug();
                })
                .AddItem<ImageFile>(options: i =>
                {
                    i.Name = "Test Image";
                    i.ContentLink = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
                })
                .AddExistingItem(teaser3Ref);
            })
            .WithPages<ArticlePage>(p =>
            {
                p.Name = "Article 33";
                p.MetaTitle = ResourceHelpers.Faker.Lorem.Slug();
                p.TeaserText = ResourceHelpers.Faker.Lorem.Paragraph();
                p.MainBody.AddStringFragment(ResourceHelpers.Faker.Lorem.Paragraphs(10));
                p.MainContentArea.AddExistingItem(teaser2Ref);
            }, 100);
        })
        .WithPage<StandardPage>(out var notFoundPageRef, p =>
        {
            p.Name = "Not Found Page";
        })
        .WithPages<ArticlePage>(p =>
        {
            p.Name = ResourceHelpers.Faker.Lorem.Slug(2);
            p.MainContentArea
            .AddItems<TeaserBlock>(b =>
            {
                b.Heading = ResourceHelpers.Faker.Lorem.Slug();
                b.Text = ResourceHelpers.Faker.Lorem.Paragraph();
                b.Image = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
            }, 10);
        }, 10)
        .WithPages<ArticlePage>(p =>
        {
            p.Name = ResourceHelpers.Faker.Lorem.Slug(3);
            p.MainContentArea
            .AddItems<TeaserBlock>(b =>
            {
                b.Heading = ResourceHelpers.Faker.Lorem.Slug();
                b.Text = ResourceHelpers.Faker.Lorem.Paragraph();
                b.Image = PropertyHelpers.GetOrAddMedia<ImageFile>("Image 1", ".png", ResourceHelpers.GetImageStream());
            }, 2);
        }, 2);
    });
