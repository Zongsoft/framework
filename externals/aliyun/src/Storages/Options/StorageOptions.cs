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
using System.Collections.Generic;

namespace Zongsoft.Externals.Aliyun.Storages.Options;

/// <summary>表示阿里云对象存储(OSS)相关的配置接口。</summary>
public class StorageOptions
{
	#region 构造函数
	public StorageOptions() => this.Buckets = new();
	#endregion

	#region 公共属性
	/// <summary>获取或设置对象存储所属的服务区域。</summary>
	public ServiceCenterName? Region { get; set; }

	/// <summary>获取或设置关联的凭证名。</summary>
	public string Certificate { get; set; }

	/// <summary>获取存储器配置项集合。</summary>
	public BucketOptionCollection Buckets { get; }
	#endregion
}
