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
	public class ProfileEntryCollection : ProfileItemCollection<ProfileEntry>
	{
		#region 成员字段
		private readonly Dictionary<string, ProfileEntry> _dictionary;
		#endregion

		#region 构造函数
		public ProfileEntryCollection(Profile profile) : base(profile) => _dictionary = new(StringComparer.OrdinalIgnoreCase);
		public ProfileEntryCollection(ProfileSection section) : base(section) => _dictionary = new(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 公共属性
		public ProfileEntry this[string name] => name != null && _dictionary.TryGetValue(name, out var entry) ? entry : null;
		#endregion

		#region 公共方法
		public bool Contains(string name) => name != null && _dictionary.ContainsKey(name);
		public bool TryGetValue(string name, out ProfileEntry entry) => _dictionary.TryGetValue(name, out entry);

		public bool Remove(string name, out ProfileEntry entry)
		{
			if(_dictionary.Remove(name, out entry))
			{
				base.Items.Remove(entry);
				return true;
			}

			return false;
		}

		public ProfileEntry Add(string name, string value = null)
		{
			ProfileEntry entry = this.Section == null ? new(this.Profile, name, value) : new(this.Section, name, value);
			this.Add(entry);
			return entry;
		}

		public ProfileEntry Add(int lineNumber, string name, string value = null)
		{
			ProfileEntry entry = this.Section == null ? new(this.Profile, lineNumber, name, value) : new(this.Section, lineNumber, name, value);
			this.Add(entry);
			return entry;
		}

		#endregion

		#region 重写方法
		protected override void InsertItem(int index, ProfileEntry entry)
		{
			if(entry == null)
				throw new ArgumentNullException(nameof(entry));

			_dictionary.Add(entry.Name, entry);
			base.InsertItem(index, entry);
		}

		protected override void SetItem(int index, ProfileEntry entry)
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
