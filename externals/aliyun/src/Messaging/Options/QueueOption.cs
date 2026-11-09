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

namespace Zongsoft.Externals.Aliyun.Messaging.Options;

/// <summary>表示消息队列的配置选项。</summary>
public class QueueOption
{
	/// <summary>获取或设置消息队列名。</summary>
	public string Name
	{
		get; set;
	}

	/// <summary>获取或设置消息队列所在的服务区域名，如果为空(<c>null</c>)或空字符串则由提供程序配置项指定。</summary>
	public ServiceCenterName? Region
	{
		get; set;
	}

	/// <summary>获取或设置消息队列关联的凭证名，如果为空(<c>null</c>)或空字符串则由提供程序配置项指定。</summary>
	public string Certificate
	{
		get; set;
	}
}
