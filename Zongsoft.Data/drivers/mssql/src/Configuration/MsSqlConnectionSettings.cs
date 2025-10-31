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
 * This file is part of Zongsoft.Data.MsSql library.
 *
 * The Zongsoft.Data.MsSql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.MsSql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.MsSql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

using Microsoft.Data.SqlClient;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Data.MsSql.Configuration;

public sealed class MsSqlConnectionSettings : ConnectionSettingsBase<MsSqlConnectionSettingsDriver, SqlConnectionStringBuilder>
{
	#region 构造函数
	public MsSqlConnectionSettings(MsSqlConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public MsSqlConnectionSettings(MsSqlConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[Category("Connection")]
	[ConnectionSetting(true)]
	[Alias(nameof(SqlConnectionStringBuilder.DataSource))]
	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[Alias(nameof(SqlConnectionStringBuilder.ApplicationName))]
	public string Client
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[ConnectionSetting(true)]
	[Alias(nameof(SqlConnectionStringBuilder.InitialCatalog))]
	public string Database
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(SqlConnectionStringBuilder.UserID))]
	public string UserName
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(SqlConnectionStringBuilder.Password))]
	public string Password
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[DefaultValue(false)]
	[Alias(nameof(SqlConnectionStringBuilder.IntegratedSecurity))]
	public bool IntegratedSecurity
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[Alias(nameof(SqlConnectionStringBuilder.CurrentLanguage))]
	public string Language
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(SqlConnectionStringBuilder.ServerCertificate))]
	public string ServerCertificate
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(SqlConnectionStringBuilder.HostNameInCertificate))]
	public string HostNameInCertificate
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(true)]
	[Alias(nameof(SqlConnectionStringBuilder.Enlist))]
	public bool Enlist
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[Alias(nameof(SqlConnectionStringBuilder.Replication))]
	public bool Replication
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[DefaultValue("30s")]
	[Category("Connection")]
	[Alias(nameof(SqlConnectionStringBuilder.ConnectTimeout))]
	[Alias(nameof(SqlConnectionStringBuilder.CommandTimeout))]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Seconds))]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[DefaultValue("30s")]
	[Category("Connection")]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Seconds))]
	public TimeSpan LoadBalanceTimeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	public int ConnectRetryCount
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[DefaultValue("10s")]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Seconds), Visible = false)]
	public TimeSpan ConnectRetryInterval
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[Category("Pooling")]
	[DefaultValue(true)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(SqlConnectionStringBuilder.Pooling))]
	public bool Pooling
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Pooling")]
	[DefaultValue(500)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(SqlConnectionStringBuilder.MaxPoolSize))]
	public uint MaximumPoolSize
	{
		get => this.GetValue<uint>();
		set => this.SetValue(value);
	}

	[Category("Pooling")]
	[DefaultValue(5)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(SqlConnectionStringBuilder.MinPoolSize))]
	public uint MinimumPoolSize
	{
		get => this.GetValue<uint>();
		set => this.SetValue(value);
	}

	[Category("Pooling")]
	[DefaultValue(true)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(SqlConnectionStringBuilder.MultipleActiveResultSets))]
	public bool MultipleActiveResultSets
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(SqlConnectionStringBuilder.WorkstationID))]
	public string WorkstationId
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}
	#endregion
}
