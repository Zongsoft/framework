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
 * Copyright (C) 2015-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.MySql library.
 *
 * The Zongsoft.Data.MySql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.MySql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.MySql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

using MySqlConnector;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Data.MySql.Configuration;

public sealed class MySqlConnectionSettings : ConnectionSettingsBase<MySqlConnectionSettingsDriver, MySqlConnectionStringBuilder>
{
	#region 构造函数
	public MySqlConnectionSettings(MySqlConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public MySqlConnectionSettings(MySqlConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[Category("Connection")]
	[ConnectionSetting(true)]
	[Alias(nameof(MySqlConnectionStringBuilder.Server))]
	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[DefaultValue(3306)]
	[Category("Connection")]
	[Alias(nameof(MySqlConnectionStringBuilder.Port))]
	public ushort Port
	{
		get => this.GetValue<ushort>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[Alias(nameof(MySqlConnectionStringBuilder.ApplicationName))]
	public string Client
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[ConnectionSetting(true)]
	[Alias(nameof(MySqlConnectionStringBuilder.Database))]
	public string Database
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(MySqlConnectionStringBuilder.UserID))]
	public string UserName
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(MySqlConnectionStringBuilder.Password))]
	public string Password
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue("utf8mb4")]
	[Alias(nameof(MySqlConnectionStringBuilder.CharacterSet))]
	public string Charset
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(MySqlConnectionStringBuilder.PipeName))]
	public string PipeName
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(true)]
	[Alias(nameof(MySqlConnectionStringBuilder.Pipelining))]
	public bool Pipelining
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[DefaultValue(MySqlLoadBalance.RoundRobin)]
	[Alias(nameof(MySqlConnectionStringBuilder.LoadBalance))]
	public MySqlLoadBalance LoadBalance
	{
		get => this.GetValue<MySqlLoadBalance>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[Alias(nameof(MySqlConnectionStringBuilder.UseCompression))]
	public bool Compressible
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[DefaultValue("30s")]
	[Category("Connection")]
	[Alias(nameof(MySqlConnectionStringBuilder.ConnectionTimeout))]
	[Alias(nameof(MySqlConnectionStringBuilder.DefaultCommandTimeout))]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Seconds))]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(MySqlConnectionStringBuilder.Keepalive))]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Seconds), Visible = false)]
	public TimeSpan KeepAlive
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[DefaultValue(MySqlConnectionProtocol.Tcp)]
	[Alias(nameof(MySqlConnectionStringBuilder.ConnectionProtocol))]
	public MySqlConnectionProtocol Protocol
	{
		get => this.GetValue<MySqlConnectionProtocol>();
		set => this.SetValue(value);
	}

	[Category("Pooling")]
	[DefaultValue(true)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(MySqlConnectionStringBuilder.Pooling))]
	public bool Pooling
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Pooling")]
	[DefaultValue(500)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(MySqlConnectionStringBuilder.MaximumPoolSize))]
	public uint MaximumPoolSize
	{
		get => this.GetValue<uint>();
		set => this.SetValue(value);
	}

	[Category("Pooling")]
	[DefaultValue(5)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(MySqlConnectionStringBuilder.MinimumPoolSize))]
	public uint MinimumPoolSize
	{
		get => this.GetValue<uint>();
		set => this.SetValue(value);
	}

	[Category("Pooling")]
	[DefaultValue(true)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(MySqlConnectionStringBuilder.ConnectionReset))]
	public bool ConnectionReset
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("TLS")]
	[Alias(nameof(MySqlConnectionStringBuilder.SslKey))]
	public string SslKey
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("TLS")]
	[DefaultValue(MySqlSslMode.None)]
	[Alias(nameof(MySqlConnectionStringBuilder.SslMode))]
	public MySqlSslMode SslMode
	{
		get => this.GetValue<MySqlSslMode>();
		set => this.SetValue(value);
	}

	[Category("TLS")]
	[Alias(nameof(MySqlConnectionStringBuilder.SslCert))]
	public string SslCertification
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("TLS")]
	[Alias(nameof(MySqlConnectionStringBuilder.CertificateFile))]
	public string CertificateFile
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("TLS")]
	[Alias(nameof(MySqlConnectionStringBuilder.CertificatePassword))]
	public string CertificateSecret
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("TLS")]
	[Alias(nameof(MySqlConnectionStringBuilder.CertificateThumbprint))]
	public string CertificateDigest
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("TLS")]
	[DefaultValue(MySqlCertificateStoreLocation.None)]
	[Alias(nameof(MySqlConnectionStringBuilder.CertificateStoreLocation))]
	public MySqlCertificateStoreLocation CertificateStoreLocation
	{
		get => this.GetValue<MySqlCertificateStoreLocation>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(false)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(MySqlConnectionStringBuilder.AllowUserVariables))]
	public bool AllowUserVariables
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(true)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(MySqlConnectionStringBuilder.AllowPublicKeyRetrieval))]
	public bool AllowPublicKeyRetrieval
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(true)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(MySqlConnectionStringBuilder.AllowLoadLocalInfile))]
	public bool AllowLoadLocalInfile
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[ConnectionSetting(Visible = false)]
	public string AllowLoadLocalInfileInPath
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(true)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(MySqlConnectionStringBuilder.AllowZeroDateTime))]
	public bool AllowZeroDateTime
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(true)]
	[ConnectionSetting(Visible = false)]
	[Alias(nameof(MySqlConnectionStringBuilder.ConvertZeroDateTime))]
	public bool ConvertZeroDateTime
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(MySqlDateTimeKind.Unspecified)]
	[Alias(nameof(MySqlConnectionStringBuilder.DateTimeKind))]
	public MySqlDateTimeKind DateTimeKind
	{
		get => this.GetValue<MySqlDateTimeKind>();
		set => this.SetValue(value);
	}
	#endregion
}
