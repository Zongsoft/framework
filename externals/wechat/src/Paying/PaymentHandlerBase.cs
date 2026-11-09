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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Externals.Wechat.Paying;

public abstract class PaymentHandlerBase : FallbackHandlerBase<PaymentManager.PaymentService.PaymentOrder>
{
	protected PaymentHandlerBase(IServiceProvider serviceProvider) : base(serviceProvider) { }

	public override FallbackKind Kind => FallbackKind.Payment;

	internal protected override Type GetRequestType(string format) =>
		string.Equals(format, PaymentManager.PaymentService.DirectPaymentService.FORMAT, StringComparison.OrdinalIgnoreCase) ?
		typeof(PaymentManager.PaymentService.DirectPaymentService.DirectOrder) :
		typeof(PaymentManager.PaymentService.BrokerPaymentService.BrokerOrder);
}

public abstract class PaymentHandlerBase<TResult> : FallbackHandlerBase<PaymentManager.PaymentService.PaymentOrder, TResult>
{
	protected PaymentHandlerBase(IServiceProvider serviceProvider) : base(serviceProvider) { }

	public override FallbackKind Kind => FallbackKind.Payment;

	internal protected override Type GetRequestType(string format) =>
		string.Equals(format, PaymentManager.PaymentService.DirectPaymentService.FORMAT, StringComparison.OrdinalIgnoreCase) ?
		typeof(PaymentManager.PaymentService.DirectPaymentService.DirectOrder) :
		typeof(PaymentManager.PaymentService.BrokerPaymentService.BrokerOrder);
}
