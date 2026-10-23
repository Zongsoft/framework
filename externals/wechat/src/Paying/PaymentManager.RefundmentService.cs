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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public abstract partial class RefundmentService
	{
		#region 私有变量
		private readonly IAuthority _authority;
		private readonly IAuthority _subsidiary;
		#endregion

		#region 构造函数
		protected RefundmentService(IAuthority authority, IAuthority subsidiary = null)
		{
			_authority = authority ?? throw new ArgumentNullException(nameof(authority));
			_subsidiary = subsidiary;
		}
		#endregion

		#region 公共属性
		public RequestBuilder Request { get; protected set; }
		#endregion

		#region 保护属性
		protected abstract HttpClient Client { get; }
		#endregion

		#region 公共方法
		public async ValueTask<RefundmentResult> RefundAsync(RefundmentRequest request, CancellationToken cancellation = default)
		{
			var data = this.GetRefundmentRequest(request);
			var order = await this.Client.PostAsync<object, Response>("refund/domestic/refunds", data, cancellation);
			this.ValidateResponse(order, request.VoucherCode);

			if(order.PaymentVoucherCode != request.PaymentKey || order.Amount.Value != request.Amount.Value || order.Amount.PaidAmount != request.Amount.PaidAmount)
				throw new FormatException();

			return new RefundmentResult(order);
		}

		public async ValueTask<RefundmentOrder> GetAsync(string voucher, CancellationToken cancellation = default)
		{
			ValidateIdentifier(voucher, 64);
			var url = $"refund/domestic/refunds/{Uri.EscapeDataString(voucher)}";
			var argument = this.GetArgument();

			if(!string.IsNullOrEmpty(argument))
				url += "?" + argument;

			var order = await this.Client.GetAsync<Response>(url, cancellation);
			this.ValidateResponse(order, voucher);
			return order;
		}
		#endregion

		#region 抽象方法
		protected abstract string GetArgument();
		#endregion

		#region 模型类型
		public abstract class RequestBuilder
		{
			public RefundmentRequest Create(string paymentKey, string voucher, decimal amount, decimal paidAmount, string description = null) => this.Create(paymentKey, voucher, amount, paidAmount, null, description);
			public abstract RefundmentRequest Create(string paymentKey, string voucher, decimal amount, decimal paidAmount, string currency, string description = null);

			internal static string GetFallback(string key)
			{
				var url = Utility.GetOptions<Options.FallbackOptions>($"/Externals/Wechat/Fallback")?.Url;

				if(string.IsNullOrWhiteSpace(url))
					return null;

				return string.Format(url, "refundment", key);
			}
		}

		public class RefundmentRequest
		{
			#region 构造函数
			public RefundmentRequest(string paymentKey, string voucher, decimal amount, decimal paidAmount, string currency = null, string description = null)
			{
				this.PaymentKey = paymentKey;
				this.VoucherCode = voucher;
				this.Amount = new AmountInfo(amount, paidAmount, currency);
				this.Description = description;
			}
			#endregion

			#region 公共属性
			[JsonPropertyName("out_trade_no")]
			public string PaymentKey { get; set; }

			[JsonPropertyName("out_refund_no")]
			public string VoucherCode { get; set; }

			[JsonPropertyName("reason")]
			public string Description { get; set; }

			[JsonPropertyName("notify_url")]
			public string FallbackUrl { get; set; }

			[JsonPropertyName("amount")]
			public AmountInfo Amount
			{
				get;
				set
				{
					Money.Validate(value.Value, nameof(value));
					Money.Validate(value.PaidAmount, nameof(value));
					Money.Currency(value.Currency);
					field = value;
				}
			}

			[JsonPropertyName("goods_detail")]
			public OrderDetailInfo[] Details { get; set; }
			#endregion

			#region 嵌套结构
			public readonly struct AmountInfo
			{
				#region 构造函数
				[JsonConstructor]
				public AmountInfo(int value, int paidAmount, string currency = null)
				{
					this.Value = Money.Validate(value, nameof(value));
					this.PaidAmount = Money.Validate(paidAmount, nameof(paidAmount));
					this.Currency = Money.Currency(currency);

					if(value > paidAmount)
						throw new ArgumentOutOfRangeException(nameof(value));
				}

				public AmountInfo(decimal value, decimal paidAmount, string currency = null) :
					this(Money.Convert(value, nameof(value)), Money.Convert(paidAmount, nameof(paidAmount)), currency) { }
				#endregion

				#region 公共属性
				[JsonPropertyName("refund")]
				public int Value { get; }

				[JsonPropertyName("total")]
				public int PaidAmount { get; }

				[JsonPropertyName("currency")]
				public string Currency { get; }
				#endregion

				#region 重写方法
				public override string ToString() => string.IsNullOrEmpty(this.Currency) ? $"{this.Value}/{this.PaidAmount}" : $"{this.Currency}:{this.Value}/{this.PaidAmount}";
				#endregion
			}

			public struct OrderDetailInfo
			{
				[JsonPropertyName("merchant_goods_id")]
				public string ItemCode { get; set; }

				[JsonPropertyName("goods_name")]
				public string ItemName { get; set; }

				[JsonPropertyName("refund_amount")]
				public int Amount { get; set; }

				[JsonPropertyName("refund_quantity")]
				public int Quantity { get; set; }

				[JsonPropertyName("unit_price")]
				public int UnitPrice { get; set; }
			}
			#endregion
		}

		public class RefundmentOrder
		{
			#region 构造函数
			public RefundmentOrder() { }
			#endregion

			#region 公共属性
			public virtual AuthorityToken Merchant { get; }
			public virtual AuthorityToken? Subsidiary { get; }

			[JsonPropertyName("transaction_id")]
			public string PaymentSerialId { get; set; }

			[JsonPropertyName("out_trade_no")]
			public string PaymentVoucherCode { get; set; }

			[JsonPropertyName("refund_id")]
			public string RefundId { get; set; }

			[JsonPropertyName("out_refund_no")]
			public string RefundVoucherCode { get; set; }

			[JsonPropertyName("user_received_account")]
			public string ReceiverAccount { get; set; }

			[JsonPropertyName("channel")]
			public string Channel { get; set; }

			[JsonPropertyName("status")]
			public virtual string Status { get; set; }

			[JsonPropertyName("create_time")]
			public DateTime CreatedTime { get; set; }

			[JsonPropertyName("success_time")]
			public DateTime? RefundedTime { get; set; }

			[JsonPropertyName("amount")]
			public AmountInfo Amount { get; set; }

			[JsonPropertyName("promotion_detail")]
			public PromotionInfo[] Promotions { get; set; }
			#endregion

			#region 嵌套结构
			public struct AmountInfo
			{
				#region 构造函数
				public AmountInfo(int value, int paidAmount, string currency = null)
				{
					this.Value = value;
					this.PaidAmount = paidAmount;
					this.Currency = currency;
					this.Discount = 0;
					this.ActualTotal = 0;
					this.ActualRefund = 0;
					this.SettlementTotal = 0;
					this.SettlementRefund = 0;
				}

				public AmountInfo(decimal value, decimal paidAmount, string currency = null)
				{
					this.Value = Money.Convert(value, nameof(value), true);
					this.PaidAmount = Money.Convert(paidAmount, nameof(paidAmount), true);
					this.Currency = currency;
					this.Discount = 0;
					this.ActualTotal = 0;
					this.ActualRefund = 0;
					this.SettlementTotal = 0;
					this.SettlementRefund = 0;
				}
				#endregion

				#region 公共属性
				[JsonPropertyName("refund")]
				public int Value { get; set; }

				[JsonPropertyName("total")]
				public int PaidAmount { get; set; }

				[JsonPropertyName("payer_total")]
				public int ActualTotal { get; set; }

				[JsonPropertyName("payer_refund")]
				public int ActualRefund { get; set; }

				[JsonPropertyName("settlement_total")]
				public int SettlementTotal { get; set; }

				[JsonPropertyName("settlement_refund")]
				public int SettlementRefund { get; set; }

				[JsonPropertyName("discount_refund")]
				public int Discount { get; set; }

				[JsonPropertyName("currency")]
				public string Currency { get; set; }
				#endregion

				#region 重写方法
				public override string ToString() => string.IsNullOrEmpty(this.Currency) ? $"{this.Value}/{this.PaidAmount}" : $"{this.Currency}:{this.Value}/{this.PaidAmount}";
				#endregion
			}

			public struct PromotionInfo
			{
				[JsonPropertyName("promotion_id")]
				public string PromotionId { get; set; }

				[JsonPropertyName("type")]
				public string PromotionType { get; set; }

				[JsonPropertyName("scope")]
				public string PromotionScope { get; set; }

				[JsonPropertyName("amount")]
				public int Amount { get; set; }

				[JsonPropertyName("refund_amount")]
				public int RefundAmount { get; set; }

				[JsonPropertyName("goods_detail")]
				public OrderDetailInfo[] Details { get; set; }
			}

			public struct OrderDetailInfo
			{
				[JsonPropertyName("merchant_goods_id")]
				public string ItemCode { get; set; }

				[JsonPropertyName("goods_name")]
				public string ItemName { get; set; }

				[JsonPropertyName("refund_amount")]
				public int Amount { get; set; }

				[JsonPropertyName("refund_quantity")]
				public int Quantity { get; set; }

				[JsonPropertyName("unit_price")]
				public int UnitPrice { get; set; }
			}
			#endregion
		}
		#endregion

		internal partial class DirectRefundmentService : RefundmentService
		{
			#region 常量定义
			/// <summary>表示直连商户数据格式的标识。</summary>
			internal const string FORMAT = "direct";
			#endregion

			#region 成员字段
			private HttpClient _client;
			#endregion

			#region 构造函数
			public DirectRefundmentService(IAuthority authority) : base(authority)
			{
				this.Request = new DirectBuilder(authority);
			}
			#endregion

			#region 重写属性
			protected override HttpClient Client
			{
				get
				{
					if(_client != null)
						return _client;

					lock(this)
					{
						if(_client == null)
							_client = HttpClientFactory.GetHttpClient(_authority);
					}

					return _client;
				}
			}
			#endregion

			#region 重写方法
			protected override string GetArgument() => null;
			#endregion

			#region 嵌套子类
			private sealed class DirectBuilder : RequestBuilder
			{
				private readonly IAuthority _authority;
				public DirectBuilder(IAuthority authority) => _authority = authority;

				public override RefundmentRequest Create(string paymentKey, string voucher, decimal amount, decimal paidAmount, string currency, string description = null)
				{
					return new DirectRequest(paymentKey, voucher, amount, paidAmount, currency, _authority.Code, description)
					{
						FallbackUrl = GetFallback(_authority.Code),
					};
				}
			}

			private sealed class DirectRequest : RefundmentRequest
			{
				#region 构造函数
				internal DirectRequest(string paymentKey, string voucher, decimal amount, decimal paidAmount, string merchantId, string description = null) : this(paymentKey, voucher, amount, paidAmount, null, merchantId, description) { }
				internal DirectRequest(string paymentKey, string voucher, decimal amount, decimal paidAmount, string currency, string merchantId, string description = null) : base(paymentKey, voucher, amount, paidAmount, currency, description)
				{
					this.MerchantId = merchantId;
				}
				#endregion

				#region 公共属性
				[JsonPropertyName("mchid")]
				public string MerchantId { get; set; }
				#endregion
			}

			public sealed class DirectOrder : RefundmentNotification
			{
				#region 重写属性
				public override AuthorityToken Merchant { get => new AuthorityToken(this.MerchantId, this.AppId); }
				#endregion

				#region 公共属性
				[JsonPropertyName("mchid")]
				public string MerchantId { get; set; }

				[JsonPropertyName("appid")]
				public string AppId { get; set; }
				#endregion
			}
			#endregion
		}

		internal partial class BrokerRefundmentService : RefundmentService
		{
			#region 常量定义
			/// <summary>表示服务商数据格式的标识。</summary>
			internal const string FORMAT = "broker";
			#endregion

			#region 成员字段
			private HttpClient _client;
			#endregion

			#region 构造函数
			public BrokerRefundmentService(IAuthority master, IAuthority subsidiary) : base(master, subsidiary)
			{
				ArgumentNullException.ThrowIfNull(subsidiary);
				this.Request = new BrokerBuilder(master, subsidiary);
			}
			#endregion

			#region 重写属性
			protected override HttpClient Client
			{
				get
				{
					if(_client != null)
						return _client;

					lock(this)
					{
						if(_client == null)
							_client = HttpClientFactory.GetHttpClient(_authority);
					}

					return _client;
				}
			}
			#endregion

			#region 重写方法
			protected override string GetArgument() => $"sub_mchid={Uri.EscapeDataString(_subsidiary.Code)}";
			#endregion

			#region 嵌套子类
			private sealed class BrokerBuilder : RequestBuilder
			{
				private readonly IAuthority _master;
				private readonly IAuthority _subsidiary;

				public BrokerBuilder(IAuthority master, IAuthority subsidiary)
				{
					_master = master;
					_subsidiary = subsidiary;
				}

				public override RefundmentRequest Create(string paymentKey, string voucher, decimal amount, decimal paidAmount, string currency, string description = null)
				{
					return new BrokerRequest(paymentKey, voucher, amount, paidAmount, currency, _subsidiary.Code, description)
					{
						FallbackUrl = GetFallback(_master.Code),
						MasterId = _master.Code,
					};
				}
			}

			private sealed class BrokerRequest : RefundmentRequest
			{
				#region 构造函数
				internal BrokerRequest(string paymentKey, string voucher, decimal amount, decimal paidAmount, string merchantId, string description = null) : this(paymentKey, voucher, amount, paidAmount, null, merchantId, description) { }
				internal BrokerRequest(string paymentKey, string voucher, decimal amount, decimal paidAmount, string currency, string merchantId, string description = null) : base(paymentKey, voucher, amount, paidAmount, currency, description)
				{
					this.MerchantId = merchantId;
				}
				#endregion

				#region 公共属性
				[JsonIgnore]
				public string MasterId { get; init; }

				[JsonPropertyName("sub_mchid")]
				public string MerchantId { get; set; }
				#endregion
			}

			public sealed class BrokerOrder : RefundmentNotification
			{
				#region 重写属性
				public override AuthorityToken Merchant { get => new AuthorityToken(this.MerchantId, this.AppId); }
				public override AuthorityToken? Subsidiary { get => new AuthorityToken(this.SubMerchantId, this.SubAppId); }
				#endregion

				#region 公共属性
				[JsonPropertyName("sp_mchid")]
				public string MerchantId { get; set; }

				[JsonPropertyName("sp_appid")]
				public string AppId { get; set; }

				[JsonPropertyName("sub_mchid")]
				public string SubMerchantId { get; set; }

				[JsonPropertyName("sub_appid")]
				public string SubAppId { get; set; }
				#endregion
			}
			#endregion
		}
	}
}
