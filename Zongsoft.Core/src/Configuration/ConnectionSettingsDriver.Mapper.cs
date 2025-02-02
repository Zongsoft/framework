﻿/*
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
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Configuration;

partial class ConnectionSettingsDriver<TDescriptors>
{
	protected abstract class MapperBase
	{
		#region 构造函数
		protected MapperBase(ConnectionSettingsDriver<TDescriptors> driver)
		{
			this.Driver = driver ?? throw new ArgumentNullException(nameof(driver));
			this.Descriptors = (TDescriptors)driver.GetType()
				.GetProperty(nameof(IConnectionSettingsDriver.Descriptors), BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
				.GetValue(null);
		}
		#endregion

		#region 保护字段
		protected readonly ConnectionSettingsDriver<TDescriptors> Driver;
		protected readonly TDescriptors Descriptors;
		#endregion

		#region 公共方法
		/// <summary>尝试映射转换指定键名对应的值。</summary>
		/// <param name="name">指定的待映射的键名。</param>
		/// <param name="values">指定的待映射的原始值集合。</param>
		/// <param name="result">输出参数，返回映射转换成功后的值。</param>
		/// <returns>如果映射成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
		public bool Map(string name, IDictionary<object, string> values, out object result)
		{
			if(this.Descriptors.TryGetValue(name, out var descriptor))
				return this.OnMap(descriptor, values, out result);

			result = default;
			return false;
		}

		/// <summary>尝试映射转换指定键名对应的值。</summary>
		/// <param name="name">指定的待映射的键名。</param>
		/// <param name="value">指定的待映射的值。</param>
		/// <param name="values">指定的待映射的原始值集合。</param>
		/// <returns>如果映射成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
		public string Map<T>(string name, T value, IDictionary<object, string> values)
		{
			return this.Descriptors.TryGetValue(name, out var descriptor) ? this.OnMap(descriptor, value, values) : null;
		}
		#endregion

		#region 虚拟方法
		protected virtual string OnMap<T>(ConnectionSettingDescriptor descriptor, T value, IDictionary<object, string> values)
		{
			return Common.Convert.TryConvertValue<string>(value, out var result) ? result : null;
		}

		protected virtual bool OnMap(ConnectionSettingDescriptor descriptor, IDictionary<object, string> values, out object value)
		{
			if(values.TryGetValue(descriptor, out var text))
				return Common.Convert.TryConvertValue(text, descriptor.Type, out value);

			value = descriptor.DefaultValue;
			return !descriptor.Required || descriptor.DefaultValue != null;
		}
		#endregion
	}
}
