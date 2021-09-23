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
using System.Collections.Generic;

namespace Zongsoft.Configuration
{
	public abstract class ConnectionSettingValuesMapper : IConnectionSettingValuesMapper
	{
		#region 私有字段
		private readonly IDictionary<string, string> _keys;
		#endregion

		#region 构造函数
		public ConnectionSettingValuesMapper(string driver, IEnumerable<KeyValuePair<string, string>> mapping = null)
		{
			if(string.IsNullOrEmpty(driver))
				throw new ArgumentNullException(nameof(driver));

			this.Driver = driver.Trim();

			_keys = mapping == null ?
				new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase):
				new Dictionary<string, string>(mapping, StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		public string Driver { get; }
		#endregion

		#region 公共方法
		public virtual string Map(string key) => key != null && _keys.TryGetValue(key, out var name) ? name : key;

		public virtual string GetValue(string key, IDictionary<string, string> values)
		{
			if(key == null)
				return null;

			if(_keys.TryGetValue(key, out var name))
				key = name;

			return values.TryGetValue(key, out var value) ? value : null;
		}

		public virtual bool Validate(string key, string value) => key != null;
		#endregion
	}
}
