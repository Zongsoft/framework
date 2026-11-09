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

namespace Zongsoft.Externals.Aliyun.Messaging;

/// <summary>表示主题信息的实体类。</summary>
public class MessageTopicInfo
{
	#region 公共属性
	/// <summary>获取或设置主题的名称。</summary>
	public string Name { get; set; }

	/// <summary>获取或设置主题的创建时间。</summary>
	public DateTime CreatedTime { get; set; }

	/// <summary>获取或设置主题的最后修改时间。</summary>
	public DateTime? ModifiedTime { get; set; }

	/// <summary>获取或设置主题中消息的最大长度，单位：byte。</summary>
	public int MaximumMessageSize { get; set; }

	/// <summary>获取或设置主题中消息的最大保持时长。</summary>
	public TimeSpan MessageRetentionPeriod { get; set; }

	/// <summary>获取或设置当前主题中的消息数量。</summary>
	public int MessageCount { get; set; }

	/// <summary>获取或设置一个值，指示主题队列是否启用了日志记录。</summary>
	public bool LoggingEnabled { get; set; }
	#endregion
}
