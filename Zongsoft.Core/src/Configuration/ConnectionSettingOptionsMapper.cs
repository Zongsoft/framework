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

namespace Zongsoft.Configuration
{
	public abstract class ConnectionSettingOptionsMapper : IConnectionSettingOptionsMapper
	{
		#region 构造函数
		protected ConnectionSettingOptionsMapper(string driver, IEnumerable<KeyValuePair<string, string>> mapping = null)
		{
			if(string.IsNullOrEmpty(driver))
				throw new ArgumentNullException(nameof(driver));

			this.Driver = driver.Trim();
			this.Mapping = mapping == null ?
				new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase):
				new Dictionary<string, string>(mapping, StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		public string Driver { get; }
		public IDictionary<string, string> Mapping { get; }
		#endregion

		#region 公共方法
		public virtual bool Validate(string name, string value) => name != null;
		public bool Map<T>(string name, IDictionary<string, string> options, out T value)
		{
			if(this.Mapping.ContainsKey(name) && options.TryGetValue(name, out var text))
				return this.OnMap(name, text, options, out value);

			value = default;
			return false;
		}
		#endregion

		#region 保护方法
		protected virtual bool OnMap<T>(string name, string text, IDictionary<string, string> values, out T value) => Zongsoft.Common.Convert.TryConvertValue(text, out value);
		#endregion
	}
}
