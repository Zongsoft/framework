/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata.Profiles
{
	public class MetadataCommandScriptor : IDataCommandScriptor
	{
		#region 常量定义
		private const string DEFAULT_KEY = "*";
		#endregion

		#region 成员字段
		private readonly MetadataCommand _command;
		private readonly IDictionary<string, string> _entries;
		#endregion

		#region 构造函数
		public MetadataCommandScriptor(MetadataCommand command)
		{
			_command = command ?? throw new ArgumentNullException(nameof(command));
			_entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共方法
		public void Load(string path)
		{
			if(string.IsNullOrWhiteSpace(path))
				return;

			var files = Directory.GetFiles(path, $"{_command.Name}-*.sql", SearchOption.AllDirectories);

			if(files != null && files.Length > 0)
			{
				for(int i = 0; i < files.Length; i++)
				{
					var name = Path.GetFileNameWithoutExtension(files[i]);
					var index = name.LastIndexOf('-');

					if(index > 0 && index < name.Length - 1)
					{
						var driver = name.Substring(index + 1);

						if(!_entries.ContainsKey(driver))
							this.TrySetScript(driver, File.ReadAllText(files[i]));
					}
				}
			}
		}

		public string GetScript(string driver)
		{
			var key = string.IsNullOrWhiteSpace(driver) ? DEFAULT_KEY : driver;
			return _entries.TryGetValue(key, out var text) ? text : null;
		}

		public void SetScript(string driver, string text)
		{
			if(string.IsNullOrWhiteSpace(text))
				return;

			var key = string.IsNullOrWhiteSpace(driver) ? DEFAULT_KEY : driver;
			_entries[key] = text.Trim();
		}

		public bool TrySetScript(string driver, string text)
		{
			if(string.IsNullOrWhiteSpace(text))
				return false;

			var key = string.IsNullOrWhiteSpace(driver) ? DEFAULT_KEY : driver;
			return _entries.TryAdd(key, text.Trim());
		}
		#endregion
	}
}
