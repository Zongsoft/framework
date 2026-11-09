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

namespace Zongsoft.Externals.Aliyun.Messaging.Options;

/// <summary>表示阿里云消息服务的配置选项。</summary>
public class MessagingOptions
{
	#region 构造函数
	public MessagingOptions()
	{
		this.Queues = new QueueOptionCollection();
		this.Topics = new TopicOptionCollection();
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置消息服务的访问标识。</summary>
	public string Name { get; set; }

	/// <summary>获取消息队列提供程序的配置项。</summary>
	public QueueOptionCollection Queues { get; }

	/// <summary>获取消息主题提供程序的配置项。</summary>
	public TopicOptionCollection Topics { get; }
	#endregion
}
