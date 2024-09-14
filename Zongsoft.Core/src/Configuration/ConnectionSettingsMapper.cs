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
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Configuration
{
	public abstract class ConnectionSettingsMapper : IConnectionSettingsMapper
	{
		#region 私有变量
		private bool _initialized = false;
		private readonly Dictionary<string, string> _mapping = new(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 构造函数
		protected ConnectionSettingsMapper(IConnectionSettingsDriver driver)
		{
			this.Driver = driver ?? throw new ArgumentNullException(nameof(driver));
		}
		#endregion

		#region 公共属性
		public IConnectionSettingsDriver Driver { get; }
		#endregion

		#region 初始化器
		private void Initialize()
		{
			if(_initialized)
				return;

			lock(_mapping)
			{
				if(_initialized)
					return;

				var descriptors = (ConnectionSettingDescriptorCollection)this.Driver.GetType()
					.GetProperty(nameof(IConnectionSettingsDriver.Descriptors), BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
					.GetValue(null);

				if(descriptors != null)
				{
					foreach(var descriptor in descriptors)
					{
						if(string.IsNullOrEmpty(descriptor.Alias))
							continue;

						_mapping.Add(descriptor.Name, descriptor.Alias);
					}
				}

				_initialized = true;
			}
		}
		#endregion

		#region 公共方法
		public bool Map(string name, IDictionary<string, string> values, out object result)
		{
			this.Initialize();

			if(_mapping.ContainsKey(name) && values.ContainsKey(name))
				return this.OnMap(name, values, out result);

			result = default;
			return false;
		}

		public string Map(string name, object value, IDictionary<string, string> values)
		{
			this.Initialize();
			return _mapping.ContainsKey(name) ? this.OnMap(name, value, values) : null;
		}
		#endregion

		#region 保护方法
		protected virtual string OnMap(string name, object value, IDictionary<string, string> values)
		{
			return Common.Convert.TryConvertValue<string>(value, out var result) ? result : null;
		}

		protected virtual bool OnMap(string name, IDictionary<string, string> values, out object value)
		{
			if(values.TryGetValue(name, out var text))
			{
				value = text;
				return true;
			}

			value = null;
			return false;
		}
		#endregion
	}
}
