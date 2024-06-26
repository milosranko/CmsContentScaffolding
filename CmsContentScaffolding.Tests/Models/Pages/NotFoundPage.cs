﻿using CmsContentScaffolding.Optimizely.Tests.Models.Blocks;
using CmsContentScaffolding.Optimizely.Tests.Models.Pages.Base;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace CmsContentScaffolding.Optimizely.Tests.Models.Pages;

[ContentType(
    GUID = "{005DBD8E-B8C6-4C0F-8066-44CFE01FD535}",
    GroupName = "MetaData")]
[AvailableContentTypes(
    Availability.None)]
public class NotFoundPage : PageBasePublic
{
    #region Content tab

    [Display(
        GroupName = SystemTabNames.Content,
        Order = 100)]
    public virtual TeaserBlock Teaser { get; set; }

    #endregion

    #region Public properties

    public override void SetDefaultValues(ContentType contentType)
    {
        base.SetDefaultValues(contentType);

        VisibleInMenu = false;
    }

    #endregion
}
