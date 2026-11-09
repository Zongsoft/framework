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
using System.Data;
using System.Data.Common;
using System.ComponentModel;

using InfluxDB3.Client;
using InfluxDB3.Client.Write;
using InfluxDB3.Client.Config;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Data.Influx.Common;

public class InfluxConnectionStringBuilder : DbConnectionStringBuilder
{
	#region 构造函数
	public InfluxConnectionStringBuilder(string connectionString = null) => this.ConnectionString = connectionString;
	#endregion

	#region 公共属性
	[ConnectionSetting(true)]
	[Alias(nameof(ClientConfig.Host))]
	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[ConnectionSetting(true)]
	[Alias(nameof(ClientConfig.Database))]
	public string Database
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(ClientConfig.Token))]
	public string Token
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[DefaultValue("15s")]
	[Alias(nameof(ClientConfig.Timeout))]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[DefaultValue("ms")]
	public string Precision
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(ClientConfig.Organization))]
	public string Organization
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}
	#endregion

	#region 保护属性
	private PropertyDescriptorCollection _properties;
	public PropertyDescriptorCollection Properties => _properties ??= ((ICustomTypeDescriptor)this).GetProperties();
	#endregion

	#region 私有方法
	private T GetValue<T>([System.Runtime.CompilerServices.CallerMemberName]string name = null)
	{
		var properties = ((ICustomTypeDescriptor)this).GetProperties();

		if(this.TryGetValue(name, out var value))
			return Zongsoft.Common.Convert.ConvertValue<T>(value, () => this.Properties.Find(name, true)?.Converter);

		var attribute = GetAttribute<DefaultValueAttribute>(name);
		return attribute == null ? default : Zongsoft.Common.Convert.ConvertValue<T>(attribute.Value);

		TAttribute GetAttribute<TAttribute>(string name) where TAttribute : Attribute
		{
			var property = this.Properties.Find(name, true);
			return property == null ? null : (TAttribute)property.Attributes[typeof(TAttribute)];
		}
	}

	private void SetValue<T>(T value, [System.Runtime.CompilerServices.CallerMemberName]string name = null)
	{
		if(value is null)
			this.Remove(name);
		else
			this[name] = value;
	}
	#endregion

	#region 公共方法
	public ClientConfig GetConfiguration() => new()
	{
		Host = this.Server,
		Token = this.Token,
		Database = this.Database,
		Organization = this.Organization,
		Timeout = this.Timeout,
		WriteOptions = new WriteOptions()
		{
			Precision = InfluxUtility.GetPrecision(this.Precision),
		}
	};
	#endregion
}
