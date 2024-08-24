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

namespace Zongsoft.Externals.Wechat.Paying
{
	public partial class PaymentManager
	{
		public abstract class RefundmentService
		{
			#region 私有变量
			private readonly IAuthority _authority;
			#endregion

			#region 构造函数
			protected RefundmentService(IAuthority authority)
			{
				_authority = authority ?? throw new ArgumentNullException(nameof(authority));
			}
			#endregion

			#region 公共属性
			public RequestBuilder Request { get; protected set; }
			#endregion

			#region 保护属性
			protected abstract HttpClient Client { get; }
			#endregion

			#region 公共方法
			public ValueTask<RefundmentOrder> RefundAsync(RefundmentRequest request, CancellationToken cancellation = default)
			{
				if(request == null)
					throw new ArgumentNullException(nameof(request));

				return this.Client.PostAsync<RefundmentRequest, RefundmentOrder>("refund/domestic/refunds", request, cancellation);
			}

			public ValueTask<RefundmentOrder> GetAsync(string voucher, CancellationToken cancellation = default)
			{
				if(string.IsNullOrEmpty(voucher))
					throw new ArgumentNullException(nameof(voucher));

				var url = $"refund/domestic/refunds/{voucher}";

				var argument = this.GetArgument();
				if(!string.IsNullOrEmpty(argument))
					url += "?" + argument;

				return this.Client.GetAsync<RefundmentOrder>(url, cancellation);
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

				internal static string GetFallback(string key, string format)
				{
					var url = Utility.GetOptions<Options.FallbackOptions>($"/Externals/Wechat/Fallback")?.Url;

					if(string.IsNullOrWhiteSpace(url))
						return null;

					return string.Format(url, "refundment", string.IsNullOrEmpty(format) ? key : $"{key}:{format}");
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
				public AmountInfo Amount { get; set; }

				[JsonPropertyName("goods_detail")]
				public OrderDetailInfo[] Details { get; set; }
				#endregion

				#region 嵌套结构
				public struct AmountInfo
				{
					#region 构造函数
					public AmountInfo(int value, int paidAmount, string currency = null)
					{
						this.Value = value;
						this.PaidAmount = paidAmount;
						this.Currency = string.IsNullOrEmpty(currency) ? "CNY" : currency;
					}

					public AmountInfo(decimal value, decimal paidAmount, string currency = null)
					{
						this.Value = (int)(value * 100);
						this.PaidAmount = (int)(paidAmount * 100);
						this.Currency = string.IsNullOrEmpty(currency) ? "CNY" : currency;
					}
					#endregion

					#region 公共属性
					[JsonPropertyName("refund")]
					public int Value { get; set; }

					[JsonPropertyName("total")]
					public int PaidAmount { get; set; }

					[JsonPropertyName("currency")]
					public string Currency { get; set; }
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
				#region 成员字段
				private string _status;
				#endregion

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
				public string Status { get => string.IsNullOrEmpty(_status) ? this.RefundStatus : _status; set => _status = value; }

				[JsonPropertyName("refund_status")]
				public string RefundStatus { get; set; }

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
						this.Value = (int)(value * 100);
						this.PaidAmount = (int)(paidAmount * 100);
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

			internal class DirectRefundmentService : RefundmentService
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
								_client = HttpClientFactory.GetHttpClient(_authority.Certificate);
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
						return new DirectRequest(paymentKey, voucher, amount, paidAmount, currency, uint.Parse(_authority.Code), description)
						{
							FallbackUrl = GetFallback(_authority.Code, FORMAT),
						};
					}
				}

				private sealed class DirectRequest : RefundmentRequest
				{
					#region 构造函数
					internal DirectRequest(string paymentKey, string voucher, decimal amount, decimal paidAmount, uint merchantId, string description = null) : this(paymentKey, voucher, amount, paidAmount, null, merchantId, description) { }
					internal DirectRequest(string paymentKey, string voucher, decimal amount, decimal paidAmount, string currency, uint merchantId, string description = null) : base(paymentKey, voucher, amount, paidAmount, currency, description)
					{
						this.MerchantId = merchantId;
					}
					#endregion

					#region 公共属性
					[JsonPropertyName("mchid")]
					[JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
					public uint MerchantId { get; set; }
					#endregion
				}

				public sealed class DirectOrder : RefundmentOrder
				{
					#region 重写属性
					public override AuthorityToken Merchant { get => new AuthorityToken(this.MerchantId.ToString(), this.AppId); }
					#endregion

					#region 公共属性
					[JsonPropertyName("mchid")]
					[JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
					public uint MerchantId { get; set; }

					[JsonPropertyName("appid")]
					public string AppId { get; set; }
					#endregion
				}
				#endregion
			}

			internal class BrokerRefundmentService : RefundmentService
			{
				#region 常量定义
				/// <summary>表示服务商数据格式的标识。</summary>
				internal const string FORMAT = "broker";
				#endregion

				#region 成员字段
				private HttpClient _client;
				private readonly IAuthority _subsidiary;
				#endregion

				#region 构造函数
				public BrokerRefundmentService(IAuthority master, IAuthority subsidiary) : base(master)
				{
					_subsidiary = subsidiary ?? throw new ArgumentNullException(nameof(subsidiary));
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
								_client = HttpClientFactory.GetHttpClient(_authority.Certificate);
						}

						return _client;
					}
				}
				#endregion

				#region 重写方法
				protected override string GetArgument() => $"sub_mchid={_subsidiary.Code}";
				#endregion

				#region 嵌套子类
				private sealed class BrokerBuilder : RequestBuilder
				{
					private readonly IAuthority _master;
					private readonly IAuthority _subsidiary;

					public BrokerBuilder(IAuthority master, IAuthority subsidiary) { _master = master; _subsidiary = subsidiary; }

					public override RefundmentRequest Create(string paymentKey, string voucher, decimal amount, decimal paidAmount, string currency, string description = null)
					{
						return new BrokerRequest(paymentKey, voucher, amount, paidAmount, currency, uint.Parse(_subsidiary.Code), description)
						{
							FallbackUrl = GetFallback(_master.Code, FORMAT),
						};
					}
				}

				private sealed class BrokerRequest : RefundmentRequest
				{
					#region 构造函数
					internal BrokerRequest(string paymentKey, string voucher, decimal amount, decimal paidAmount, uint merchantId, string description = null) : this(paymentKey, voucher, amount, paidAmount, null, merchantId, description) { }
					internal BrokerRequest(string paymentKey, string voucher, decimal amount, decimal paidAmount, string currency, uint merchantId, string description = null) : base(paymentKey, voucher, amount, paidAmount, currency, description)
					{
						this.MerchantId = merchantId;
					}
					#endregion

					#region 公共属性
					[JsonPropertyName("sub_mchid")]
					[JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
					public uint MerchantId { get; set; }
					#endregion
				}

				public sealed class BrokerOrder : RefundmentOrder
				{
					#region 重写属性
					public override AuthorityToken Merchant { get => new AuthorityToken(this.MerchantId.ToString(), this.AppId); }
					public override AuthorityToken? Subsidiary { get => new AuthorityToken(this.SubMerchantId.ToString(), this.SubAppId); }
					#endregion

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
					#endregion
				}
				#endregion
			}
		}
	}
}
