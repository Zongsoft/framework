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
using System.Linq;

namespace Zongsoft.Configuration;

public class ConnectionSettings : ConnectionSettingsBase<ConnectionSettingsDriver>
{
	#region 静态构造
	static ConnectionSettings() => Drivers = new();
	#endregion

	#region 静态属性
	public static ConnectionSettingsDriverCollection Drivers { get; }
	#endregion

	#region 构造函数
	public ConnectionSettings(string value = null) : base(ConnectionSettingsDriver.Default, value) { }
	public ConnectionSettings(string name, string value) : base(ConnectionSettingsDriver.Default, name, value) { }
	#endregion

	#region 特定属性
	public string Group
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Client
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public ushort Port
	{
		get => this.GetValue<ushort>();
		set => this.SetValue(value);
	}

	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	public string Charset
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Encoding
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Provider
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Database
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string UserName
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Password
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Instance
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Application
	{
		get => this.GetValue<string>() ?? Services.ApplicationContext.Current?.Name;
		set => this.SetValue(value);
	}
	#endregion

	#region 公共方法
	/// <summary>设置指定设置项的值。</summary>
	/// <typeparam name="T">泛型参数，表示设置项的类型。</typeparam>
	/// <param name="name">指定要设置的设置项名称。</param>
	/// <param name="value">指定要设置的设置项的值。</param>
	/// <returns>如果设置成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	public new bool SetValue<T>(string name, T value)
	{
		if(base.SetValue(name, value))
			return true;

		if(Common.Convert.TryConvertValue<string>(value, out var result))
		{
			if(string.IsNullOrEmpty(result))
				base.Entries.Remove(name);
			else
				base.Entries[name] = result;

			//更新 Value 属性值
			this.Value = string.Join(';', base.Entries.Where(entry => !string.IsNullOrEmpty(entry.Value)).Select(entry => $"{entry.Key}={entry.Value}"));

			return true;
		}

		return false;
	}

	/// <summary>获取指定设置项的值。</summary>
	/// <typeparam name="T">泛型参数，表示设置项的类型。</typeparam>
	/// <param name="name">指定要获取的设置项名称。</param>
	/// <param name="defaultValue">指定的获取失败的返回值。</param>
	/// <returns>返回的设置项值，如果获取失败则返回值为<paramref name="defaultValue"/>参数指定的值。</returns>
	public T GetValue<T>(string name, T defaultValue = default)
	{
		if(this.TryGetValue<T>(name, out var value))
			return value;

		return base.Entries.TryGetValue(name, out var text) && Common.Convert.TryConvertValue<T>(text, out value) ? value : defaultValue;
	}
	#endregion
}
