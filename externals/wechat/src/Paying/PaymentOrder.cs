/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat.Paying
{
	public abstract class PaymentOrder
	{
		#region 构造函数
		protected PaymentOrder() { }
		#endregion

		#region 公共属性
		[JsonPropertyName("transaction_id")]
		public string OrderId { get; set; }

		[JsonPropertyName("trade_type")]
		public PaymentKind Kind { get; set; }

		[JsonPropertyName("trade_state")]
		public PaymentStatus Status { get; set; }

		[JsonPropertyName("trade_state_desc")]
		public string StatusDescription { get; set; }

		[JsonPropertyName("bank_type")]
		public string BankType { get; set; }

		[JsonPropertyName("out_trade_no")]
		public string VoucherCode { get; set; }

		[JsonPropertyName("success_time")]
		public DateTime? PaidTime { get; set; }

		[JsonPropertyName("attach")]
		public string Extra { get; set; }

		[JsonPropertyName("amount")]
		public AmountInfo Amount { get; set; }

		[JsonPropertyName("scene_info")]
		public PlaceInfo Place { get; set; }

		[JsonPropertyName("promotion_detail")]
		public PromotionInfo[] Promotions { get; set; }
		#endregion

		#region 嵌套结构
		public struct AmountInfo
		{
			[JsonPropertyName("total")]
			public int Value { get; set; }

			[JsonPropertyName("currency")]
			public string Currency { get; set; }

			[JsonPropertyName("payer_total")]
			public int PaidValue { get; set; }
		}

		public struct PromotionInfo
		{
			[JsonPropertyName("coupon_id")]
			public string CouponId { get; set; }

			[JsonPropertyName("name")]
			public string CouponName { get; set; }

			[JsonPropertyName("type")]
			public string CouponType { get; set; }

			[JsonPropertyName("scope")]
			public string CouponScope { get; set; }

			[JsonPropertyName("amount")]
			public int Amount { get; set; }

			[JsonPropertyName("goods_detail")]
			public OrderDetailInfo[] Details { get; set; }
		}

		public struct OrderDetailInfo
		{
			[JsonPropertyName("goods_id")]
			public string ItemCode { get; set; }

			[JsonPropertyName("goods_remark")]
			public string ItemName { get; set; }

			[JsonPropertyName("quantity")]
			public int Quantity { get; set; }

			[JsonPropertyName("unit_price")]
			public int UnitPrice { get; set; }

			[JsonPropertyName("discount_amount")]
			public int Discount { get; set; }
		}

		public struct PlaceInfo
		{
			public PlaceInfo(string deviceId)
			{
				this.DeviceId = deviceId;
			}

			[JsonPropertyName("device_id")]
			public string DeviceId { get; set; }
		}
		#endregion

		#region 嵌套子类
		public sealed class Direct : PaymentOrder
		{
			#region 公共属性
			[JsonPropertyName("mchid")]
			[JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
			public uint MerchantId { get; set; }

			[JsonPropertyName("appid")]
			public string AppId { get; set; }

			[JsonPropertyName("payer")]
			public PayerInfo Payer { get; set; }
			#endregion

			#region 嵌套结构
			public struct PayerInfo
			{
				[JsonPropertyName("openid")]
				public string OpenId { get; set; }
			}
			#endregion
		}

		public sealed class Broker : PaymentOrder
		{
			#region 公共属性
			[JsonPropertyName("sp_mchid")]
			[JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
			public uint MerchantId { get; set; }

			[JsonPropertyName("sp_appid")]
			public string AppId { get; set; }

			[JsonPropertyName("sub_mchid")]
			[JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
			public uint SubMerchantId { get; set; }

			[JsonPropertyName("sub_appid")]
			public string SubAppId { get; set; }

			[JsonPropertyName("payer")]
			public PayerInfo Payer { get; set; }
			#endregion

			#region 嵌套结构
			public struct PayerInfo
			{
				[JsonPropertyName("sp_openid")]
				public string OpenId { get; set; }

				[JsonPropertyName("sub_openid")]
				public string SubOpenId { get; set; }
			}
			#endregion
		}
		#endregion
	}
}
