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
	public class ProfileSectionCollection : ProfileItemCollection<ProfileSection>
	{
		#region 成员字段
		private readonly Dictionary<string, ProfileSection> _dictionary;
		#endregion

		#region 构造函数
		public ProfileSectionCollection(Profile profile) : base(profile) => _dictionary = new(StringComparer.OrdinalIgnoreCase);
		public ProfileSectionCollection(ProfileSection section) : base(section) => _dictionary = new(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 公共属性
		public ProfileSection this[string name] => name != null && _dictionary.TryGetValue(name, out var section) ? section : null;
		#endregion

		#region 公共方法
		public bool Contains(string name) => name != null && _dictionary.ContainsKey(name);
		public bool TryGetValue(string name, out ProfileSection section) => _dictionary.TryGetValue(name, out section);

		public ProfileSection Find(string path)
		{
			if(string.IsNullOrEmpty(path))
				return null;

			var dictionary = _dictionary;
			var parts = path.Split(['/', ' ', '\t'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			for(int i = 0; i < parts.Length; i++)
			{
				if(!dictionary.TryGetValue(parts[i], out var section))
					return null;

				if(i == parts.Length - 1)
					return section;

				dictionary = section.Sections._dictionary;
			}

			return null;
		}

		public bool Remove(string name, out ProfileSection section)
		{
			if(_dictionary.Remove(name, out section))
			{
				base.Items.Remove(section);
				return true;
			}

			return false;
		}

		public ProfileSection Add(string name, int lineNumber = -1)
		{
			ProfileSection section = this.Section == null ? new(this.Profile, name, lineNumber) : new(this.Section, name, lineNumber);
			this.Add(section);
			return section;
		}
		#endregion

		#region 重写方法
		protected override void InsertItem(int index, ProfileSection entry)
		{
			if(entry == null)
				throw new ArgumentNullException(nameof(entry));

			_dictionary.Add(entry.Name, entry);
			base.InsertItem(index, entry);
		}

		protected override void SetItem(int index, ProfileSection entry)
		{
			if(entry == null)
				throw new ArgumentNullException(nameof(entry));

			_dictionary[entry.Name] = entry;
			base.SetItem(index, entry);
		}

		protected override void RemoveItem(int index)
		{
			var item = base.Items[index];
			base.RemoveItem(index);
			_dictionary.Remove(item.Name);
		}

		protected override void ClearItems()
		{
			_dictionary.Clear();
			base.ClearItems();
		}
		#endregion
	}
}
