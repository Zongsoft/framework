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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.Configuration
{
	public abstract class ConnectionSettingsMapper : IConnectionSettingsMapper
	{
		#region 成员字段
		private readonly IDictionary<string, string> _mapping;
		#endregion

		#region 构造函数
		protected ConnectionSettingsMapper(IConnectionSettingsDriver driver)
		{
			this.Driver = driver ?? throw new ArgumentNullException(nameof(driver));

			_mapping = new Dictionary<string, string>(
				driver.Descriptors
					.Where(descriptor => !string.IsNullOrEmpty(descriptor.Alias))
					.Select(descriptor => new KeyValuePair<string, string>(descriptor.Name, descriptor.Alias)),
				StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		public IConnectionSettingsDriver Driver { get; }
		#endregion

		#region 公共方法
		public bool Map(IDictionary<string, string> values, string name, out object value)
		{
			if(_mapping.ContainsKey(name) && values.TryGetValue(name, out var text))
				return this.OnMap(name, text, values, out value);

			value = default;
			return false;
		}

		public string Map(string name, object value, IDictionary<string, string> values)
		{
			return _mapping.ContainsKey(name) ? this.OnMap(name, value, values) : null;
		}
		#endregion

		#region 保护方法
		protected virtual string OnMap(string name, object value, IDictionary<string, string> values)
		{
			return Common.Convert.TryConvertValue<string>(value, out var result) ? result : null;
		}

		protected virtual bool OnMap(string name, string text, IDictionary<string, string> values, out object value)
		{
			value = text;
			return true;
		}
		#endregion
	}
}
