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

namespace Zongsoft.Components;

/// <summary>表示尝试器的配置选项。</summary>
public interface IAttempterOptions
{
	/// <summary>获取或设置尝试失败的阈值，零表示不限制。</summary>
	int Limit { get; set; }

	/// <summary>获取或设置尝试失败的窗口期。</summary>
	TimeSpan Window { get; set; }

	/// <summary>获取或设置尝试失败超过阈值后的锁定时长。</summary>
	TimeSpan Period { get; set; }
}
