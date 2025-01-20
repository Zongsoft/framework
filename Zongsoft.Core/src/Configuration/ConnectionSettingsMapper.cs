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

namespace Zongsoft.Configuration;

public abstract class ConnectionSettingsMapper<TDriver> : IConnectionSettingsMapper where TDriver : IConnectionSettingsDriver
{
	#region 构造函数
	protected ConnectionSettingsMapper(TDriver driver)
	{
		this.Driver = driver ?? throw new ArgumentNullException(nameof(driver));
		this.Descriptors = (ConnectionSettingDescriptorCollection)driver.GetType()
				.GetProperty(nameof(IConnectionSettingsDriver.Descriptors), BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
				.GetValue(null);
	}
	#endregion

	#region 公共属性
	public TDriver Driver { get; }
	IConnectionSettingsDriver IConnectionSettingsMapper.Driver => this.Driver;
	protected ConnectionSettingDescriptorCollection Descriptors { get; }
	#endregion

	#region 公共方法
	public bool Map(string name, IDictionary<string, string> values, out object result)
	{
		if(this.Descriptors.TryGetValue(name, out var descriptor))
			return this.OnMap(descriptor, values, out result);

		result = default;
		return false;
	}

	public string Map<T>(string name, T value, IDictionary<string, string> values)
	{
		return this.Descriptors.TryGetValue(name, out var descriptor) ? this.OnMap(descriptor, value, values) : null;
	}
	#endregion

	#region 保护方法
	protected virtual string OnMap<T>(ConnectionSettingDescriptor descriptor, T value, IDictionary<string, string> values)
	{
		return Common.Convert.TryConvertValue<string>(value, out var result) ? result : null;
	}

	protected virtual bool OnMap(ConnectionSettingDescriptor descriptor, IDictionary<string, string> values, out object value)
	{
		if(values.TryGetValue(descriptor.Name, out var text))
			return Common.Convert.TryConvertValue(text, descriptor.Type, out value);

		if(!string.IsNullOrEmpty(descriptor.Alias) && values.TryGetValue(descriptor.Alias, out text))
			return Common.Convert.TryConvertValue(text, descriptor.Type, out value);

		value = descriptor.DefaultValue;
		return !descriptor.Required || descriptor.DefaultValue != null;
	}
	#endregion
}
