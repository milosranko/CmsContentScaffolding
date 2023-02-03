﻿using EPiServer.Core;
using EPiServer.DataAnnotations;
using EPiServer.Framework.DataAnnotations;
using EPiServer.Web;
using Optimizely.Demo.PublicWeb.Models.Blocks.Base;
using Optimizely.Demo.PublicWeb.Models.Blocks.Local;
using System.ComponentModel.DataAnnotations;

namespace Optimizely.Demo.PublicWeb.Models.Blocks;

[ContentType(GUID = "{C98C99EA-A630-49CD-8A45-5AEF47EE265D}")]
public class TeaserBlock : BlockBase
{
    #region Content tab

    [CultureSpecific]
    [Display(
        GroupName = "Content",
        Order = 100)]
    public virtual string Heading { get; set; }

    [CultureSpecific]
    [Display(
        GroupName = "Content",
        Order = 110)]
    [UIHint(UIHint.Textarea, PresentationLayer.Edit)]
    public virtual string LeadText { get; set; }

    [Display(
        GroupName = "Content",
        Order = 120)]
    public virtual LinkBlock LinkButton { get; set; }

    [CultureSpecific]
    [Display(
        GroupName = "Content",
        Order = 130)]
    [UIHint(UIHint.Image)]
    public virtual ContentReference Image { get; set; }

    #endregion
}
