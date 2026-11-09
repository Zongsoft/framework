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

namespace Zongsoft.Externals.Wechat.Paying;

/// <summary>表示退款状态的枚举。</summary>
public enum RefundmentStatus
{
	/// <summary>退款成功</summary>
	[Components.Alias("Success")]
	Succeed,

	/// <summary>退款失败</summary>
	Failed,

	/// <summary>已关闭</summary>
	[Components.Alias("Closed")]
	Cancelled,

	/// <summary>处理中</summary>
	Processing,

	/// <summary>退款异常，需核对或处理收款账户。</summary>
	[Components.Alias("Abnormal")]
	Abnormal,

	/// <summary>未提供或无法识别的状态，必须查询确认。</summary>
	Unknown,
}
