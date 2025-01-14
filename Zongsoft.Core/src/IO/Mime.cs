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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;
using Zongsoft.Configuration.Profiles;

namespace Zongsoft.IO
{
	public static class Mime
	{
		#region 私有变量
		private static readonly object _locker = new();
		private static Dictionary<string, string> _mapping;
		#endregion

		#region 公共属性
		public static IDictionary<string, string> Mapping
		{
			get
			{
				Initialize();
				return _mapping;
			}
		}
		#endregion

		#region 公共方法
		public static string GetMimeType(string path) => TryGetMimeType(path, out var type) ? type : null;
		public static bool TryGetMimeType(string path, out string type)
		{
			if(string.IsNullOrEmpty(path))
			{
				type = null;
				return false;
			}

			Initialize();
			return _mapping.TryGetValue(System.IO.Path.GetExtension(path), out type);
		}
		#endregion

		#region 私有方法
		private static void Initialize()
		{
			if(_mapping != null)
				return;

			lock(_locker)
			{
				_mapping ??= new Dictionary<string, string>(Load(), StringComparer.OrdinalIgnoreCase);
			}
		}

		private static IEnumerable<KeyValuePair<string, string>> Load()
		{
			var root = ApplicationContext.Current?.ApplicationPath ?? AppDomain.CurrentDomain.BaseDirectory;
			var path = Path.Combine(root, "mime");
			if(!File.Exists(path))
				yield break;

			//加载应用程序根目录中名为“mime”的文件
			var profile = Profile.Load(System.IO.Path.Combine(root, "mime"));

			foreach(var item in profile.Items)
			{
				if(item is ProfileEntry entry && !string.IsNullOrEmpty(entry.Name) && !string.IsNullOrEmpty(entry.Value))
				{
					yield return new(entry.Name[0] == '.' ? entry.Name : $".{entry.Name}", entry.Value);
				}
			}
		}
		#endregion
	}
}