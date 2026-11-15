/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Zongsoft.Configuration;

public static class XmlConfigurationExtension
{
	/// <summary>按 XML 配置的声明顺序获取指定路径下的直接子键。同名键以首次出现的位置为准。</summary>
	/// <param name="configuration">包含 XML 配置提供程序的配置根。</param>
	/// <param name="path">父级配置路径，可使用冒号或斜杠分隔。</param>
	/// <returns>按声明顺序排列的 XML 配置子键。</returns>
	public static IEnumerable<string> GetDeclaredOptionKeys(this IConfigurationRoot configuration, string path)
	{
		ArgumentNullException.ThrowIfNull(configuration);
		return GetKeys(configuration.Providers, Xml.XmlConfigurationUtility.NormalizeOptionPath(path)).Distinct(StringComparer.OrdinalIgnoreCase);

		static IEnumerable<string> GetKeys(IEnumerable<IConfigurationProvider> providers, string path)
		{
			foreach(var provider in providers)
			{
				var keys = provider switch
				{
					Xml.XmlConfigurationProvider file => file.GetDeclaredChildKeys(path),
					Xml.XmlStreamConfigurationProvider stream => stream.GetDeclaredChildKeys(path),
					ICompositeConfigurationProvider composite => GetKeys(composite.Providers, path),
					ChainedConfigurationProvider chained when chained.Configuration is IConfigurationRoot root => GetKeys(root.Providers, path),
					_ => [],
				};

				foreach(var key in keys)
					yield return key;
			}
		}
	}

	public static IConfigurationBuilder AddOptionFile(this IConfigurationBuilder builder, string path)
	{
		return AddOptionFile(builder, provider: null, path: path, optional: false, reloadOnChange: false);
	}

	public static IConfigurationBuilder AddOptionFile(this IConfigurationBuilder builder, string path, bool optional)
	{
		return AddOptionFile(builder, provider: null, path: path, optional: optional, reloadOnChange: false);
	}

	public static IConfigurationBuilder AddOptionFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange)
	{
		return AddOptionFile(builder, provider: null, path: path, optional: optional, reloadOnChange: reloadOnChange);
	}

	public static IConfigurationBuilder AddOptionFile(this IConfigurationBuilder builder, IFileProvider provider, string path, bool optional, bool reloadOnChange)
	{
		if(builder == null)
			throw new ArgumentNullException(nameof(builder));

		if(string.IsNullOrEmpty(path))
			throw new ArgumentNullException(nameof(path));

		return builder.AddOptionFile(source =>
		{
			source.FileProvider = provider;
			source.Path = path;
			source.Optional = optional;
			source.ReloadOnChange = reloadOnChange;
			source.ResolveFileProvider();
		});
	}

	public static IConfigurationBuilder AddOptionFile(this IConfigurationBuilder builder, Action<Xml.XmlConfigurationSource> configureSource)
	{
		return builder.Add(configureSource);
	}

	/// <summary>添加 XML 配置流，并在加载时收集和执行处理指令。</summary>
	/// <param name="builder">配置构建器。</param>
	/// <param name="stream">由调用方负责关闭的配置输入流。</param>
	/// <returns>指定的配置构建器。</returns>
	public static IConfigurationBuilder AddOptionStream(this IConfigurationBuilder builder, Stream stream) => AddOptionStream(builder, stream, null);

	/// <summary>添加 XML 配置流，并指定处理指令记录的来源路径。</summary>
	/// <param name="builder">配置构建器。</param>
	/// <param name="stream">由调用方负责关闭的配置输入流。</param>
	/// <param name="filePath">仅用于记录来源；为 <see langword="null"/> 时从文件流获取路径，其它流采用空字符串。</param>
	/// <returns>指定的配置构建器。</returns>
	public static IConfigurationBuilder AddOptionStream(this IConfigurationBuilder builder, Stream stream, string filePath)
	{
		if(builder == null)
			throw new ArgumentNullException(nameof(builder));

		return builder.Add<Xml.XmlStreamConfigurationSource>(source =>
		{
			source.Stream = stream;
			source.FilePath = filePath;
		});
	}

	/// <summary>通过配置委托添加 XML 配置流及其指令选项。</summary>
	/// <param name="builder">配置构建器。</param>
	/// <param name="configureSource">配置输入流及其加载选项的委托。</param>
	/// <returns>指定的 <paramref name="builder"/>。</returns>
	public static IConfigurationBuilder AddOptionStream(this IConfigurationBuilder builder, Action<Xml.XmlStreamConfigurationSource> configureSource)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(configureSource);
		return builder.Add(configureSource);
	}
}
