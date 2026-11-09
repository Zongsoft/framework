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

namespace Zongsoft.Externals.Aliyun.Pushing.Options;

/// <summary>表示移动推送相关的配置选项。</summary>
public class PushingOptions
{
	public PushingOptions() => this.Apps = new();

	/// <summary>获取或设置移动推送运营商区域。</summary>
	public ServiceCenterName? Region { get; set; }

	/// <summary>获取或设置关联的凭证名。</summary>
	public string Certificate { get; set; }

	/// <summary>获取移动推送的应用配置项集合。</summary>
	public PushingAppOptionCollection Apps { get; }
}
