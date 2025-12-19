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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.MinIO library.
 *
 * The Zongsoft.Externals.MinIO is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.MinIO is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.MinIO library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.MinIO.Configuration;

public class MinIOConnectionSettings : ConnectionSettingsBase<MinIOConnectionSettingsDriver>
{
	#region 构造函数
	public MinIOConnectionSettings(MinIOConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public MinIOConnectionSettings(MinIOConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[Alias("Url")]
	[Alias(nameof(Minio.MinioConfig.Endpoint))]
	[ConnectionSetting(true)]
	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Region
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Client
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Instance
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(Minio.MinioConfig.Secure))]
	public bool Secured
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[DefaultValue("30s")]
	[Alias(nameof(Minio.MinioConfig.RequestTimeout))]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[Alias("User")]
	[Alias(nameof(Minio.MinioConfig.AccessKey))]
	public string UserName
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(Minio.MinioConfig.SecretKey))]
	public string Password
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(Minio.MinioConfig.SessionToken))]
	public string Token
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}
    #endregion
}
