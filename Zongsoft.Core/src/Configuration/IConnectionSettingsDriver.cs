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
using System.Collections.Generic;

namespace Zongsoft.Configuration;

/// <summary>
/// 表示连接设置驱动器的接口。
/// </summary>
public interface IConnectionSettingsDriver : IEquatable<IConnectionSettingsDriver>
{
	#region 静态属性
	/// <summary>获取连接设置项描述器集合。</summary>
	static ConnectionSettingDescriptorCollection Descriptors { get; }
	#endregion

	#region 实例属性
	/// <summary>获取驱动名称。</summary>
	string Name { get; }
	/// <summary>获取驱动的描述信息。</summary>
	string Description { get; }
	#endregion

	#region 方法定义
	/// <summary>获取指定的连接设置对应的选项配置。</summary>
	/// <param name="settings">指定的连接设置。</param>
	/// <returns>返回的对应选项配置。</returns>
	object GetOptions(IConnectionSettings settings);

	/// <summary>获取指定设置项的值。</summary>
	/// <param name="name">指定要获取的设置项名称。</param>
	/// <param name="values">当前连接设置的设置项值集合。</param>
	/// <param name="value">输出参数，表示获取成功的设置项值。</param>
	/// <returns>如果获取成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	bool TryGetValue(string name, IDictionary<string, string> values, out object value);

	/// <summary>获取指定设置项的值。</summary>
	/// <typeparam name="T">泛型参数，表示设置项的类型。</typeparam>
	/// <param name="name">指定要获取的设置项名称。</param>
	/// <param name="values">当前连接设置的设置项值集合。</param>
	/// <param name="defaultValue">指定的获取失败的返回值。</param>
	/// <returns>返回的设置项值，如果获取失败则返回值为<paramref name="defaultValue"/>参数指定的值。</returns>
	T GetValue<T>(string name, IDictionary<string, string> values, T defaultValue);

	/// <summary>设置指定设置项的值。</summary>
	/// <typeparam name="T">泛型参数，表示设置项的类型。</typeparam>
	/// <param name="name">指定要设置的设置项名称。</param>
	/// <param name="value">指定要设置的设置项的值。</param>
	/// <param name="values">当前连接设置的设置项值集合。</param>
	/// <returns>如果设置成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	bool SetValue<T>(string name, T value, IDictionary<string, string> values);
	#endregion

	#region 默认实现
	bool IsDriver(string name)
	{
		if(string.IsNullOrEmpty(name))
			return string.IsNullOrEmpty(this.Name);

		return string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase) || this.Name.StartsWith($".{name}", StringComparison.OrdinalIgnoreCase);
	}
	#endregion
}

/// <summary>
/// 表示连接设置驱动器的接口。
/// </summary>
public interface IConnectionSettingsDriver<out TDescriptors> : IConnectionSettingsDriver where TDescriptors : ConnectionSettingDescriptorCollection, new()
{
	#region 静态属性
	/// <summary>获取连接设置项描述器集合。</summary>
	static new TDescriptors Descriptors { get; }
	#endregion
}

/// <summary>
/// 表示连接设置驱动器的接口。
/// </summary>
public interface IConnectionSettingsDriver<out TOptions, out TDescriptors> : IConnectionSettingsDriver<TDescriptors> where TDescriptors : ConnectionSettingDescriptorCollection, new()
{
	#region 实例方法
	new TOptions GetOptions(IConnectionSettings settings);
	TOptions GetOptions(string connectionString);
	#endregion
}
