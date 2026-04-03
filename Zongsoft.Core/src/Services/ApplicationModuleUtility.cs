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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Services;

internal static class ApplicationModuleUtility
{
	public static Version GetVersion(this IApplicationModule module)
	{
		if(module == null || module.Assembly == null)
			return null;

		var version = GetVersionFromFile(module.Name, Path.GetDirectoryName(module.Assembly.Location));
		if(version != null)
			return version;

		return module.Assembly.GetName().Version;

		static Version GetVersionFromFile(string name, string path)
		{
			if(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(path))
				return null;

			//定义版本文件信息
			var info = new FileInfo(Path.Combine(path, ".version"));

			//如果文件不存在或者文件大小超过指定大小，则认为该文件无效
			if(!info.Exists || info.Length > 1024 * 10)
				return null;

			string text;
			using var reader = info.OpenText();

			while((text = reader.ReadLine()) != null)
			{
				if(string.IsNullOrEmpty(text))
					continue;

				var index = text.IndexOf('@');

				if(index < 0)
					return Version.TryParse(text, out var version) ? version : null;
				else
					return Version.TryParse(text.AsSpan()[(index + 1)..], out var version) &&
						   text.AsSpan()[..index].Equals(name.AsSpan(), StringComparison.OrdinalIgnoreCase) ? version : null;
			}

			return null;
		}
	}
}
