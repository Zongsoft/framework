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
using System.Collections.Concurrent;

namespace Zongsoft.Externals.Wechat.Paying
{
	public partial class PaymentManager
	{
		#region 静态变量
		private static readonly ConcurrentDictionary<string, PaymentManager> _managers = new (StringComparer.OrdinalIgnoreCase);
		private static readonly ConcurrentDictionary<string, MerchantService> _merchants = new (StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 公共属性
		public PaymentService Payment { get; private set; }
		public RefundmentService Refundment { get; private set; }
		#endregion

		#region 静态构建
		public static PaymentManager Get(IAuthority authority)
		{
			if(authority == null)
				throw new ArgumentNullException(nameof(authority));

			if(string.IsNullOrEmpty(authority.Code))
				throw new ArgumentException("Invalid authority of the wechat.");

			return _managers.GetOrAdd(authority.Code, (key, authority) =>
			{
				return new PaymentManager()
				{
					Payment = new PaymentService.DirectPaymentService(authority),
					Refundment = new RefundmentService.DirectRefundmentService(authority),
				};
			}, authority);
		}

		public static PaymentManager Get(string master, IAuthority subsidiary)
		{
			if(subsidiary == null)
				throw new ArgumentNullException(nameof(subsidiary));

			if(string.IsNullOrEmpty(subsidiary.Code))
				throw new ArgumentException("Invalid authority of the wechat.");

			var authority = AuthorityUtility.GetAuthority(master) ??
				throw new InvalidOperationException($"The specified '{master}' authority does not exist.");

			return _managers.GetOrAdd(master + ':' + subsidiary.Code, (key, state) =>
			{
				return new PaymentManager()
				{
					Payment = new PaymentService.BrokerPaymentService(state.authority, state.subsidiary),
					Refundment = new RefundmentService.BrokerRefundmentService(state.authority, state.subsidiary),
				};
			}, new { authority, subsidiary });
		}

		public static MerchantService GetMerchantService(IAuthority authority)
		{
			if(authority == null)
				throw new ArgumentNullException(nameof(authority));

			if(string.IsNullOrEmpty(authority.Code))
				throw new ArgumentException("Invalid authority of the wechat.");

			return _merchants.GetOrAdd(authority.Code, (key, authority) => new MerchantService(authority), authority);
		}
		#endregion
	}

	public static class PaymentManagerExtension
	{
		public static PaymentManager GetPayment(this IAuthority authority) => PaymentManager.Get(authority);
		public static PaymentManager GetPayment(this IAuthority subsidiary, string master) => PaymentManager.Get(master, subsidiary);
	}
}
