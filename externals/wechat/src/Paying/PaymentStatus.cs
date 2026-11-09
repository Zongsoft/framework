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

/// <summary>表示支付状态的枚举。</summary>
public enum PaymentStatus
{
	/// <summary>未支付</summary>
	[Components.Alias("NotPay")]
	None,

	/// <summary>支付成功</summary>
	[Components.Alias("Success")]
	Succeed,

	/// <summary>转入退款</summary>
	[Components.Alias("Refund")]
	Refund,

	/// <summary>已关闭</summary>
	[Components.Alias("Closed")]
	Cancelled,

	/// <summary>已撤销（仅付款码支付会返回）</summary>
	[Components.Alias("Revoked")]
	Revoked,

	/// <summary>用户支付中（仅付款码支付会返回）</summary>
	[Components.Alias("UserPaying")]
	Paying,

	/// <summary>支付失败（仅付款码支付会返回）</summary>
	[Components.Alias("PayError")]
	Failed,

	/// <summary>未提供或无法识别的状态，必须查询确认。</summary>
	Unknown,
}
