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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;

namespace Zongsoft.Configuration;

[Zongsoft.Services.Service(Members = nameof(Drivers))]
public class ConnectionSettings : ConnectionSettingsBase<ConnectionSettingsDriver>
{
	#region 静态构造
	static ConnectionSettings() => Drivers = new();
	#endregion

	#region 静态属性
	public static ConnectionSettingsDriverCollection Drivers { get; }
	#endregion

	#region 构造函数
	public ConnectionSettings(string value) : base(ConnectionSettingsDriver.Default, value) { }
	public ConnectionSettings(string name, string value) : base(ConnectionSettingsDriver.Default, name, value) { }
	#endregion

	#region 特定属性
	public string Group
	{
		get => this.GetValue<string>(nameof(Group));
		set => this.SetValue(nameof(Group), value);
	}

	public string Client
	{
		get => this.GetValue<string>(nameof(Client));
		set => this.SetValue(nameof(Client), value);
	}

	public string Server
	{
		get => this.GetValue<string>(nameof(Server));
		set => this.SetValue(nameof(Server), value);
	}

	public ushort Port
	{
		get => this.GetValue(nameof(Port), (ushort)0);
		set => this.SetValue(nameof(Port), value);
	}

	public TimeSpan Timeout
	{
		get => this.GetValue(nameof(Timeout), TimeSpan.Zero);
		set => this.SetValue(nameof(Timeout), value);
	}

	public string Charset
	{
		get => this.GetValue<string>(nameof(Charset));
		set => this.SetValue(nameof(Charset), value);
	}

	public string Encoding
	{
		get => this.GetValue<string>(nameof(Encoding));
		set => this.SetValue(nameof(Encoding), value);
	}

	public string Provider
	{
		get => this.GetValue<string>(nameof(Provider));
		set => this.SetValue(nameof(Provider), value);
	}

	public string Database
	{
		get => this.GetValue<string>(nameof(Database));
		set => this.SetValue(nameof(Database), value);
	}

	public string UserName
	{
		get => this.GetValue<string>(nameof(UserName));
		set => this.SetValue(nameof(UserName), value);
	}

	public string Password
	{
		get => this.GetValue<string>(nameof(Password));
		set => this.SetValue(nameof(Password), value);
	}

	public string Instance
	{
		get => this.GetValue<string>(nameof(Instance));
		set => this.SetValue(nameof(Instance), value);
	}

	public string Application
	{
		get => this.GetValue<string>(nameof(Application)) ?? Services.ApplicationContext.Current?.Name;
		set => this.SetValue(nameof(Application), value);
	}
	#endregion

	#region 公共属性
	public new IDictionary<object, string> Entries => base.Entries;
	#endregion

	#region 公共方法
	/// <summary>获取指定设置项的值。</summary>
	/// <param name="name">指定要获取的设置项名称。</param>
	/// <param name="value">输出参数，表示获取成功的设置项值。</param>
	/// <returns>如果获取成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	public new bool TryGetValue(string name, out object value) => base.TryGetValue(name, out value);

	/// <summary>获取指定设置项的值。</summary>
	/// <typeparam name="T">泛型参数，表示设置项的类型。</typeparam>
	/// <param name="name">指定要获取的设置项名称。</param>
	/// <param name="defaultValue">指定的获取失败的返回值。</param>
	/// <returns>返回的设置项值，如果获取失败则返回值为<paramref name="defaultValue"/>参数指定的值。</returns>
	public new T GetValue<T>(string name, T defaultValue = default) => base.GetValue(name, defaultValue);

	/// <summary>设置指定设置项的值。</summary>
	/// <typeparam name="T">泛型参数，表示设置项的类型。</typeparam>
	/// <param name="name">指定要设置的设置项名称。</param>
	/// <param name="value">指定要设置的设置项的值。</param>
	/// <returns>如果设置成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	public new bool SetValue<T>(string name, T value) => base.SetValue(name, value);
	#endregion

	#region 嵌套子类
	private sealed class DriverConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => new DriverProxy(value as string);

		private sealed class DriverProxy(string name) : IConnectionSettingsDriver
		{
			private readonly string _name = name;

			public string Name => _name ?? string.Empty;
			public string Description => this.Driver.Description;
			public IConnectionSettingsDriver Driver => Drivers.TryGetValue(_name, out var driver) ? driver : ConnectionSettingsDriver.Default;

			public bool TryGetValue(string name, IDictionary<object, string> values, out object value) => this.Driver.TryGetValue(name, values, out value);
			public T GetValue<T>(string name, IDictionary<object, string> values, T defaultValue) => this.Driver.GetValue(name, values, defaultValue);
			public bool SetValue<T>(string name, T value, IDictionary<object, string> values) => this.Driver.SetValue(name, value, values);

			public static ConnectionSettingDescriptorCollection Descriptors => null;
			public bool Equals(IConnectionSettingsDriver other) => this.Driver.Equals(other);
		}
	}
	#endregion
}
