﻿using EPiServer.Core;

namespace CmsContentScaffolding.Optimizely.Interfaces;

public interface IAssetsBuilder
{
	IAssetsBuilder WithFolder(string name, Action<IAssetsBuilder>? options = null);
	IAssetsBuilder WithContent<T>(Action<T>? value = null, Action<IAssetsBuilder>? options = null) where T : IContent;
	IAssetsBuilder WithBlock<T>(string name, Action<T>? value = null) where T : IContentData;
	IAssetsBuilder WithMedia<T>(Action<T>? value = null, Stream? stream = null, string? extension = null) where T : MediaData;
}