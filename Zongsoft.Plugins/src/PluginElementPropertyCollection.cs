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
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.ObjectModel;

namespace Zongsoft.Plugins
{
	public class PluginElementPropertyCollection : KeyedCollection<string, PluginElementProperty>
	{
		#region 成员字段
		private readonly PluginElement _owner;
		#endregion

		#region 构造函数
		public PluginElementPropertyCollection(PluginElement owner) : base(StringComparer.OrdinalIgnoreCase)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		}
		#endregion

		#region 公共方法
		public void Set(string name, string rawValue)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			if(this.TryGetValue(name, out var property))
				property.RawValue = rawValue;
			else
				this.Add(new PluginElementProperty(_owner, name, rawValue));
		}
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(PluginElementProperty item) => item.Name;
		protected override void InsertItem(int index, PluginElementProperty item)
		{
			if(item == null)
				throw new ArgumentNullException(nameof(item));

			item.Owner = _owner;
			base.InsertItem(index, item);
		}
		#endregion
	}
}
