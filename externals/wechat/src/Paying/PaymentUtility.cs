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
using System.ComponentModel;

namespace Zongsoft.Externals.Wechat.Paying;

public static class PaymentUtility
{
	public static PaymentKind GetKind(string text) => text?.ToUpperInvariant() switch
	{
		"JSAPI" => PaymentKind.Web,
		"APP" => PaymentKind.App,
		"NATIVE" => PaymentKind.Native,
		"MICROPAY" => PaymentKind.Paycode,
		"MWEB" => PaymentKind.Mobile,
		"FACEPAY" => PaymentKind.Facepay,
		_ => PaymentKind.Web,
	};

	public static PaymentStatus GetStatus(string text) => text?.ToUpperInvariant() switch
	{
		"FAIL" => PaymentStatus.Failed,
		"NOTPAY" => PaymentStatus.None,
		"SUCCESS" => PaymentStatus.Succeed,
		"REFUND" => PaymentStatus.Refund,
		"CLOSED" => PaymentStatus.Cancelled,
		"REVOKED" => PaymentStatus.Revoked,
		"USERPAYING" => PaymentStatus.Paying,
		"PAYERROR" => PaymentStatus.Failed,
		_ => PaymentStatus.Unknown,
	};
}
