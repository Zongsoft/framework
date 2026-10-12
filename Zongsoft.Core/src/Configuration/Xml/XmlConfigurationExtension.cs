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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Zongsoft.Configuration;

public static class XmlConfigurationExtension
{
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
