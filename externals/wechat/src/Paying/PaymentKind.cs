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

/// <summary>表示支付方式的枚举。</summary>
public enum PaymentKind
{
	/// <summary>公众号支付</summary>
	[Components.Alias("JSAPI")]
	Web,

	/// <summary>扫码支付</summary>
	Native,

	/// <summary>APP支付</summary>
	App,

	/// <summary>付款码支付</summary>
	[Components.Alias("MICROPAY")]
	Paycode,

	/// <summary>H5支付</summary>
	[Components.Alias("MWEB")]
	Mobile,

	/// <summary>刷脸支付</summary>
	[Components.Alias("FACEPAY")]
	Facepay,
}
