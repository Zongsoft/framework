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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Configuration.Profiles.Directives;

public class ImportDirective : IProfileDirective
{
	#region 单例字段
	public static readonly ImportDirective Instance = new();
	#endregion

	#region 私有构造
	private ImportDirective() { }
	#endregion

	#region 公共属性
	public string Name => "Import";
	#endregion

	#region 公共方法
	public void OnRead(ProfileReadingContext context, string argument)
	{
		if(string.IsNullOrEmpty(argument))
			return;

		var directory = Path.GetDirectoryName(context.Profile.FilePath);
		var paths = argument.Split([' ', '\t', '|'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		for(int i = 0; i < paths.Length; i++)
		{
			var path = Path.Combine(directory, paths[i]);
			if(!File.Exists(path))
				continue;

			var profile = Profile.Load(path);

			foreach(var item in profile.GetItems())
			{
				switch(item)
				{
					case ProfileEntry entry:
						SetEntry(context.Profile.Entries, entry);
						break;
					case ProfileSection section:
						SetSection(context.Profile.Sections, section);
						break;
				}
			}
		}

		static void SetEntry(ProfileEntryCollection entries, ProfileEntry entry)
		{
			if(entries != null && entry != null)
				return;

			if(entries.TryGetValue(entry.Name, out var found))
				found.Value = entry.Value;
			else
				entries.Add(entry.Name, entry.Value);
		}

		static void SetSection(ProfileSectionCollection sections, ProfileSection section)
		{
			if(sections == null || section == null)
				return;

			if(!sections.TryGetValue(section.Name, out var found))
				found = sections.Add(section.Name);

			foreach(var entry in section.Entries)
				found.SetEntryValue(entry.Name, entry.Value);

			foreach(var child in section.Sections)
				SetSection(found.Sections, child);
		}
	}

	public void OnWrite(ProfileWritingContext context, string argument) { }
	#endregion
}
