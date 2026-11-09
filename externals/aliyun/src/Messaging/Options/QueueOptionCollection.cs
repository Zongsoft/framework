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
using System.Collections.ObjectModel;

namespace Zongsoft.Externals.Aliyun.Messaging.Options;

/// <summary>表示阿里云消息队列提供程序的配置项集合。</summary>
public class QueueOptionCollection() : KeyedCollection<string, QueueOption>(StringComparer.OrdinalIgnoreCase)
{
	#region 公共属性
	/// <summary>获取或设置提供程序所在的服务区域名。</summary>
	public ServiceCenterName? Region { get; set; }

	/// <summary>获取或设置提供程序默认的凭证名。</summary>
	public string Certificate { get; set; }
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(QueueOption item) => item.Name;
	#endregion
}
