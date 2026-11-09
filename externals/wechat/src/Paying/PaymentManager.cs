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

public partial class PaymentManager
{
	#region 公共属性
	public PaymentService Payment { get; private set; }
	public RefundmentService Refundment { get; private set; }
	#endregion

	#region 静态构建
	public static PaymentManager Get(IAuthority authority)
	{
		ArgumentNullException.ThrowIfNull(authority);
		ArgumentException.ThrowIfNullOrWhiteSpace(authority.Code);
		return new()
		{
			Payment = new PaymentService.DirectPaymentService(authority),
			Refundment = new RefundmentService.DirectRefundmentService(authority),
		};
	}

	public static PaymentManager Get(IAuthority master, IAuthority subsidiary)
	{
		ArgumentNullException.ThrowIfNull(master);
		ArgumentNullException.ThrowIfNull(subsidiary);
		ArgumentException.ThrowIfNullOrWhiteSpace(master.Code);
		ArgumentException.ThrowIfNullOrWhiteSpace(subsidiary.Code);
		return new()
		{
			Payment = new PaymentService.BrokerPaymentService(master, subsidiary),
			Refundment = new RefundmentService.BrokerRefundmentService(master, subsidiary),
		};
	}

	public static PartnerService GetPartnerService(IAuthority authority)
	{
		ArgumentNullException.ThrowIfNull(authority);
		ArgumentException.ThrowIfNullOrWhiteSpace(authority.Code);
		return new PartnerService(authority);
	}

	public static MerchantService GetMerchantService(IAuthority authority)
	{
		ArgumentNullException.ThrowIfNull(authority);
		ArgumentException.ThrowIfNullOrWhiteSpace(authority.Code);
		return new MerchantService(authority);
	}
	#endregion
}

public static class PaymentManagerExtension
{
	public static PaymentManager GetPayment(this IAuthority authority) => PaymentManager.Get(authority);
	public static PaymentManager GetPayment(this IAuthority subsidiary, IAuthority master) => PaymentManager.Get(master, subsidiary);
}
