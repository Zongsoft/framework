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
 * This file is part of Zongsoft.Externals.Amazon library.
 *
 * The Zongsoft.Externals.Amazon is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Amazon is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Amazon library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

using Amazon;
using Amazon.S3;
using Amazon.Runtime;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Amazon.IO.Configuration;

public class S3ConnectionSettings : ConnectionSettingsBase<S3ConnectionSettingsDriver, AmazonS3Config>
{
	#region 构造函数
	public S3ConnectionSettings(S3ConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public S3ConnectionSettings(S3ConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[Alias("Url")]
	[Alias(nameof(AmazonS3Config.ServiceURL))]
	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(AmazonS3Config.RegionEndpoint))]
	[TypeConverter(typeof(RegionEndpointConverter))]
	public RegionEndpoint Region
	{
		get => this.GetValue<RegionEndpoint>();
		set => this.SetValue(value);
	}

	[Alias(nameof(AmazonS3Config.ClientAppId))]
	public string Client
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[DefaultValue("30s")]
	[Alias(nameof(AmazonS3Config.ConnectTimeout))]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[Alias("AppId")]
	[Alias("Access")]
	public string AccessKey
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias("Secret")]
	public string SecretKey
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias("Account")]
	public string AccountId
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}
	#endregion

	#region 重写方法
	protected override void Populate(AmazonS3Config options)
	{
		base.Populate(options);

		if(!string.IsNullOrEmpty(this.AccessKey))
			options.DefaultAWSCredentials = new BasicAWSCredentials(this.AccessKey, this.SecretKey, this.AccountId);

		options.ForcePathStyle = !string.IsNullOrEmpty(options.ServiceURL);
	}
	#endregion
}
