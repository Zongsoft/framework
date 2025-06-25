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

		return builder.AddOptionFile(src =>
		{
			src.FileProvider = provider;
			src.Path = path;
			src.Optional = optional;
			src.ReloadOnChange = reloadOnChange;
			src.ResolveFileProvider();
		});
	}

	public static IConfigurationBuilder AddOptionFile(this IConfigurationBuilder builder, Action<Xml.XmlConfigurationSource> configureSource)
	{
		return builder.Add(configureSource);
	}

	public static IConfigurationBuilder AddOptionStream(this IConfigurationBuilder builder, Stream stream)
	{
		if(builder == null)
			throw new ArgumentNullException(nameof(builder));

		return builder.Add<Xml.XmlStreamConfigurationSource>(s => s.Stream = stream);
	}
}
