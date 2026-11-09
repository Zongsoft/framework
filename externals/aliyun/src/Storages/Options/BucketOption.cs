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

/// <summary>表示存储器(Bucket)的配置项。</summary>
public class BucketOption
{
	#region 公共属性
	/// <summary>获取或设置存储器的名称。</summary>
	public string Name { get; set; }

	/// <summary>获取或设置存储器所属的运营商区域。</summary>
	public ServiceCenterName? Region { get; set; }

	/// <summary>获取或设置存储器关联的凭证名。</summary>
	public string Certificate { get; set; }
	#endregion
}
