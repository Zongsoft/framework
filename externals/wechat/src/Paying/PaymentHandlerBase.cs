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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Externals.Wechat.Paying
{
	public abstract class PaymentHandlerBase : FallbackHandlerBase<PaymentManager.PaymentService.PaymentOrder>
	{
		protected PaymentHandlerBase(IServiceProvider serviceProvider) : base(serviceProvider) { }

		internal protected override Type GetRequestType(string format) =>
			string.Equals(format, PaymentManager.PaymentService.DirectPaymentService.FORMAT, StringComparison.OrdinalIgnoreCase) ?
			typeof(PaymentManager.PaymentService.DirectPaymentService.DirectOrder) :
			typeof(PaymentManager.PaymentService.BrokerPaymentService.BrokerOrder);
	}

	public abstract class PaymentHandlerBase<TResult> : FallbackHandlerBase<PaymentManager.PaymentService.PaymentOrder, TResult>
	{
		protected PaymentHandlerBase(IServiceProvider serviceProvider) : base(serviceProvider) { }

		internal protected override Type GetRequestType(string format) =>
			string.Equals(format, PaymentManager.PaymentService.DirectPaymentService.FORMAT, StringComparison.OrdinalIgnoreCase) ?
			typeof(PaymentManager.PaymentService.DirectPaymentService.DirectOrder) :
			typeof(PaymentManager.PaymentService.BrokerPaymentService.BrokerOrder);
	}
}
