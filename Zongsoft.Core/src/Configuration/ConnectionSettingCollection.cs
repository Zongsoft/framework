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

namespace Zongsoft.Configuration
{
	public class ConnectionSettingCollection : Collections.NamedCollectionBase<ConnectionSetting>
	{
		#region 构造函数
		public ConnectionSettingCollection() { }
		#endregion

		#region 公共属性
		public string Default { get; set; }
		#endregion

		#region 公共方法
		public ConnectionSetting GetDefault()
		{
			return this.Default != null && this.TryGet(this.Default, out var setting) ? setting : null;
		}

		public bool Contains(string name, string driver)
		{
			return string.IsNullOrEmpty(driver) ?
				this.ContainsName(name) :
				this.TryGet(name, driver, out _);
		}

		public bool TryGet(string name, string driver, out ConnectionSetting result)
		{
			if(this.TryGetItem(name, out result) && !string.IsNullOrEmpty(driver))
				result = string.Equals(result.Driver, driver, StringComparison.OrdinalIgnoreCase) ||
				         result.Driver.EndsWith($".{driver}", StringComparison.OrdinalIgnoreCase)? result : null;

			return result != null;
		}
		#endregion

		#region 重写方法
		protected override ConnectionSetting GetItem(string name)
		{
			if(string.IsNullOrEmpty(name))
				name = this.Default ?? string.Empty;

			return base.GetItem(name);
		}

		protected override bool TryGetItem(string name, out ConnectionSetting value)
		{
			if(string.IsNullOrEmpty(name))
				name = this.Default ?? string.Empty;

			return base.TryGetItem(name, out value);
		}

		protected override string GetKeyForItem(ConnectionSetting item)
		{
			return item.Name;
		}
		#endregion
	}
}
