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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
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
