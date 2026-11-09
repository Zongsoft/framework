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

public abstract class RefundmentHandlerBase : FallbackHandlerBase<PaymentManager.RefundmentService.RefundmentOrder>
{
	protected RefundmentHandlerBase(IServiceProvider serviceProvider) : base(serviceProvider) { }

	public override FallbackKind Kind => FallbackKind.Refundment;

	internal protected override Type GetRequestType(string format) =>
		string.Equals(format, PaymentManager.RefundmentService.DirectRefundmentService.FORMAT, StringComparison.OrdinalIgnoreCase) ?
		typeof(PaymentManager.RefundmentService.DirectRefundmentService.DirectOrder) :
		typeof(PaymentManager.RefundmentService.BrokerRefundmentService.BrokerOrder);
}

public abstract class RefundmentHandlerBase<TResult> : FallbackHandlerBase<PaymentManager.RefundmentService.RefundmentOrder, TResult>
{
	protected RefundmentHandlerBase(IServiceProvider serviceProvider) : base(serviceProvider) { }

	public override FallbackKind Kind => FallbackKind.Refundment;

	internal protected override Type GetRequestType(string format) =>
		string.Equals(format, PaymentManager.RefundmentService.DirectRefundmentService.FORMAT, StringComparison.OrdinalIgnoreCase) ?
		typeof(PaymentManager.RefundmentService.DirectRefundmentService.DirectOrder) :
		typeof(PaymentManager.RefundmentService.BrokerRefundmentService.BrokerOrder);
}
