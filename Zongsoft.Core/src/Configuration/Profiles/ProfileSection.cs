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
using System.Collections.Generic;

namespace Zongsoft.Configuration.Profiles
{
	public class ProfileSection : ProfileItem
	{
		#region 静态常量
		private static readonly char[] IllegalCharacters = ['.', '/', '\\', '*', '?', '!', '@', '#', '%', '^', '&'];
		#endregion

		#region 构造函数
		public ProfileSection(Profile profile, string name, int lineNumber = -1) : base(profile, lineNumber)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			if(name.IndexOfAny(IllegalCharacters) >= 0)
				throw new ArgumentException($"The specified '{name}' section name contains illegal characters.");

			this.Name = name.Trim();
			this.FullName = this.Name;
			this.Entries = new(this);
			this.Comments = new(this);
			this.Sections = new(this);
		}

		public ProfileSection(ProfileSection section, string name, int lineNumber = -1) : base(section, lineNumber)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			if(name.IndexOfAny(IllegalCharacters) >= 0)
				throw new ArgumentException($"The specified '{name}' section name contains illegal characters.");

			this.Name = name.Trim();
			this.FullName = section.FullName + ' ' + this.Name;
			this.Entries = new(this);
			this.Comments = new(this);
			this.Sections = new(this);
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public string FullName { get; }
		public ProfileEntryCollection Entries { get; }
		public ProfileCommentCollection Comments { get; }
		public ProfileSectionCollection Sections { get; }
		public override ProfileItemType ItemType => ProfileItemType.Section;
		#endregion

		#region 重写方法
		public override string ToString() => $"[{this.FullName}]";
		#endregion

		#region 公共方法
		public string GetEntryValue(string name)
		{
			if(this.Entries.TryGetValue(name, out var entry))
				return entry.Value;

			return null;
		}

		public void SetEntryValue(string name, string value)
		{
			if(this.Entries.TryGetValue(name, out var entry))
				entry.Value = value;
			else
				this.Entries.Add(name, value);
		}
		#endregion
	}
}
