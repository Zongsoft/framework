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
 * This file is part of Zongsoft.Data.PostgreSql library.
 *
 * The Zongsoft.Data.PostgreSql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.PostgreSql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.PostgreSql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

using Npgsql;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Data.PostgreSql.Configuration;

public sealed class PostgreSqlConnectionSettings : ConnectionSettingsBase<PostgreSqlConnectionSettingsDriver, NpgsqlConnectionStringBuilder>
{
	#region 构造函数
	public PostgreSqlConnectionSettings(PostgreSqlConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public PostgreSqlConnectionSettings(PostgreSqlConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[Category("Connection")]
	[ConnectionSetting(true)]
	[Alias(nameof(NpgsqlConnectionStringBuilder.Host))]
	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[DefaultValue(5432)]
	[Category("Connection")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.Port))]
	public ushort Port
	{
		get => this.GetValue<ushort>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.ApplicationName))]
	public string Client
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[ConnectionSetting(true)]
	[Alias(nameof(NpgsqlConnectionStringBuilder.Database))]
	public string Database
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.Username))]
	public string UserName
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.Password))]
	public string Password
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.Passfile))]
	public string Passfile
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue("UTF8")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.Encoding))]
	[Alias(nameof(NpgsqlConnectionStringBuilder.ClientEncoding))]
	public string Encoding
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Advanced")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.LoadBalanceHosts))]
	public bool LoadBalanceEnabled
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[DefaultValue("10s")]
	[Category("Advanced")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.HostRecheckSeconds))]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Seconds))]
	public TimeSpan LoadBalanceLifetime
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[Category("Advanced")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.MaxAutoPrepare))]
	public int MaxAutoPrepare
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[DefaultValue(5)]
	[Category("Advanced")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.AutoPrepareMinUsages))]
	public int AutoPrepareMinUsages
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[DefaultValue("30s")]
	[Category("Connection")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.CommandTimeout))]
	[Alias(nameof(NpgsqlConnectionStringBuilder.CancellationTimeout))]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Seconds))]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[DefaultValue("300s")]
	[Category("Pooling")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.ConnectionIdleLifetime))]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Seconds))]
	public TimeSpan ConnectionIdleLifetime
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[DefaultValue("10s")]
	[Category("Pooling")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.ConnectionPruningInterval))]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Seconds))]
	public TimeSpan ConnectionPruningInterval
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[DefaultValue("3600s")]
	[Category("Pooling")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.ConnectionLifetime))]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Seconds))]
	public TimeSpan ConnectionLifetime
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.Timezone))]
	public string Timezone
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.KeepAlive))]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Seconds), Visible = false)]
	public TimeSpan KeepAlive
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[Category("Pooling")]
	[DefaultValue(true)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(NpgsqlConnectionStringBuilder.Pooling))]
	public bool Pooling
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Pooling")]
	[DefaultValue(500)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(NpgsqlConnectionStringBuilder.MaxPoolSize))]
	public uint MaximumPoolSize
	{
		get => this.GetValue<uint>();
		set => this.SetValue(value);
	}

	[Category("Pooling")]
	[DefaultValue(5)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(NpgsqlConnectionStringBuilder.MinPoolSize))]
	public uint MinimumPoolSize
	{
		get => this.GetValue<uint>();
		set => this.SetValue(value);
	}

	[Category("Security")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.SslKey))]
	public string SslKey
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Security")]
	[DefaultValue(SslMode.Prefer)]
	[Alias(nameof(NpgsqlConnectionStringBuilder.SslMode))]
	public SslMode SslMode
	{
		get => this.GetValue<SslMode>();
		set => this.SetValue(value);
	}

	[Category("Security")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.SslCertificate))]
	public string SslCertificate
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Security")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.RootCertificate))]
	public string RootCertificate
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Security")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.SslPassword))]
	public string CertificateSecret
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Security")]
	[Alias(nameof(NpgsqlConnectionStringBuilder.CheckCertificateRevocation))]
	public bool CheckCertificateRevocation
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(false)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(NpgsqlConnectionStringBuilder.IncludeErrorDetail))]
	public bool ErrorDetailed
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(false)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(NpgsqlConnectionStringBuilder.Multiplexing))]
	public bool Multiplexing
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(8192)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(NpgsqlConnectionStringBuilder.ReadBufferSize))]
	public int ReadBufferSize
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(8192)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(NpgsqlConnectionStringBuilder.WriteBufferSize))]
	public int WriteBufferSize
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}
	#endregion
}
