﻿using CmsContentScaffolding.Optimizely.Models;
using EPiServer.Core;
using EPiServer.Security;

namespace CmsContentScaffolding.Optimizely.Interfaces;

internal interface IContentBuilderManager
{
	bool SiteExists { get; }
	ContentReference CurrentReference { get; set; }
	void SetOrCreateSiteContext();
	void SetAsStartPage(ContentReference pageRef);
	void ApplyDefaultLanguage();
	void CreateDefaultRoles(IDictionary<string, AccessLevel> roles);
	void CreateRoles(IDictionary<string, AccessLevel>? roles);
	void CreateUsers(IEnumerable<UserModel>? users);
	void GetOrSetContentName<T>(IContent content, string? name = default, string? nameSuffix = default) where T : IContentData;
}
