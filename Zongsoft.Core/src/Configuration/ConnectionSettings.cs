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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Configuration;

public partial class ConnectionSettings : ConnectionSettingsBase<ConnectionSettingsDriver>
{
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
	/// <param name="name">指定设置项的名称。</param>
	/// <param name="value">指定设置项的值。</param>
	/// <returns>返回是否成功转换并设置。</returns>
	public new bool SetValue<T>(string name, T value) => base.SetValue(name, value);
	#endregion
}

partial class ConnectionSettings
{
	#region 静态字段
	private static bool _loaded;
	private static readonly ConnectionSettingsDriverCollection _drivers = new();
	#endregion

	#region 静态属性
	public static ConnectionSettingsDriverCollection Drivers
	{
		get
		{
			if(!_loaded)
				Load();

			return _drivers;
		}
	}
	#endregion

	#region 私有方法
	private static void Load()
	{
		lock(_drivers)
		{
			if(_loaded)
				return;

			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			for(int i = 0; i < assemblies.Length; i++)
			{
				if(assemblies[i].IsDynamic)
					continue;

				foreach(var type in GetDefinedTypes(assemblies[i]))
				{
					if(type.IsNotPublic || type.IsAbstract || type.IsValueType || type.IsGenericTypeDefinition)
						continue;

					if(typeof(IConnectionSettingsDriver).IsAssignableFrom(type))
					{
						var field = type.GetField(nameof(IConnectionSettingsDriver.Instance), BindingFlags.Public | BindingFlags.Static);

						if(field != null && typeof(IConnectionSettingsDriver).IsAssignableFrom(field.FieldType))
							_drivers.Add((IConnectionSettingsDriver)field.GetValue(null));
						else
						{
							var property = type.GetProperty(nameof(IConnectionSettingsDriver.Instance), BindingFlags.Public | BindingFlags.Static);

							if(property != null && property.CanRead && typeof(IConnectionSettingsDriver).IsAssignableFrom(field.FieldType))
								_drivers.Add((IConnectionSettingsDriver)property.GetValue(null));
						}
					}
				}
			}

			_loaded = true;
		}

		static IEnumerable<Type> GetDefinedTypes(Assembly assembly)
		{
			try
			{
				return assembly.DefinedTypes;
			}
			catch
			{
				return [];
			}
		}
	}
	#endregion
}
