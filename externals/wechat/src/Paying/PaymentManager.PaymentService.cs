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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat.Paying
{
	public partial class PaymentManager
	{
		public abstract class PaymentService
		{
			#region 私有变量
			private readonly IAuthority _authority;
			#endregion

			#region 构造函数
			protected PaymentService(IAuthority authority)
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
			public byte[] Signature(string identifier, out string applet, out string nonce, out long timestamp)
			{
				applet = _authority.Account.Code;
				nonce = Guid.NewGuid().ToString("N");
				timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();

				if(string.IsNullOrEmpty(applet) || _authority.Certificate == null)
					return null;

				var plaintext = $"{applet}\n{timestamp}\n{nonce}\nprepay_id={identifier}\n";
				return _authority.Certificate.Signature(System.Text.Encoding.UTF8.GetBytes(plaintext));
			}

			public virtual async ValueTask<OperationResult<(string identifier, string payer)>> PayAsync(PaymentRequest request, Scenario scenario, CancellationToken cancellation = default)
			{
				if(request == null)
					throw new ArgumentNullException(nameof(request));

				if(request is PaymentRequest.TicketRequest ticketRequest)
					return await this.PayTicketAsync(ticketRequest, cancellation);

				var result = await this.Client.PostAsync<PaymentRequest, PayResult>(this.GetUrl("transactions", scenario), request, cancellation);
				return result.Succeed ? OperationResult.Success((result.Value.Code, (string)null)) : (OperationResult)result.Failure;
			}

			public virtual async ValueTask<OperationResult> CancelAsync(string voucher, CancellationToken cancellation = default)
			{
				if(string.IsNullOrEmpty(voucher))
					throw new ArgumentNullException(nameof(voucher));

				return await this.Client.PostAsync(this.GetUrl($"transactions/out-trade-no/{voucher}/close"), this.GetCancellation(), cancellation);
			}

			public abstract ValueTask<OperationResult<PaymentOrder>> GetAsync(string voucher, CancellationToken cancellation = default);
			public abstract ValueTask<OperationResult<PaymentOrder>> GetCompletedAsync(string key, CancellationToken cancellation = default);
			#endregion

			#region 保护方法
			protected async ValueTask<OperationResult<T>> GetAsync<T>(string voucher, string arguments, CancellationToken cancellation = default) where T : PaymentOrder
			{
				if(string.IsNullOrEmpty(voucher))
					throw new ArgumentNullException(nameof(voucher));

				return await this.Client.GetAsync<T>(this.GetUrl($"transactions/out-trade-no/{voucher}?{arguments}"), cancellation);
			}

			protected async ValueTask<OperationResult<T>> GetCompletedAsync<T>(string key, string arguments, CancellationToken cancellation = default) where T : PaymentOrder
			{
				if(string.IsNullOrEmpty(key))
					throw new ArgumentNullException(nameof(key));

				return await this.Client.GetAsync<T>(this.GetUrl($"transactions/id/{key}?{arguments}"), cancellation);
			}

			protected ValueTask<OperationResult<(string identifier, string payer)>> PayTicketAsync(PaymentRequest.TicketRequest request, CancellationToken cancellation = default)
			{
				throw new NotImplementedException();
			}
			#endregion

			#region 抽象方法
			protected abstract string GetUrl(string path, Scenario? scenario = null);
			protected abstract object GetCancellation();
			#endregion

			#region 模型类型
			private struct PayResult
			{
				[JsonPropertyName("prepay_id")]
				public string Code { get; set; }
			}

			public abstract class RequestBuilder
			{
				public PaymentRequest Create(string voucher, decimal amount, string payer, string description = null) => this.Create(voucher, amount, null, payer, description);
				public abstract PaymentRequest Create(string voucher, decimal amount, string currency, string payer, string description = null);

				public PaymentRequest.TicketRequest Ticket(string voucher, decimal amount, string ticket, string description = null) => this.Ticket(voucher, amount, null, ticket, description);
				public abstract PaymentRequest.TicketRequest Ticket(string voucher, decimal amount, string currency, string ticket, string description = null);

				internal static string GetFallback(string key, string format)
				{
					var url = Utility.GetOptions<Options.FallbackOptions>($"/Externals/Wechat/Fallback")?.Url;

					if(string.IsNullOrWhiteSpace(url))
						return null;

					return string.Format(url, "payment", string.IsNullOrEmpty(format) ? key : $"{key}:{format}");
				}
			}

			public abstract class PaymentRequest
			{
				#region 构造函数
				protected PaymentRequest(string voucher, decimal amount, string currency = null, string description = null)
				{
					this.VoucherCode = voucher;
					this.Amount = new AmountInfo(amount, currency);
					this.Place = new PlaceInfo(null);
					this.Description = description;
				}
				#endregion

				#region 公共属性
				[JsonPropertyName("out_trade_no")]
				public string VoucherCode { get; set; }

				[JsonPropertyName("description")]
				public string Description { get; set; }

				[JsonPropertyName("time_expire")]
				public DateTime? Expiration { get; set; }

				[JsonPropertyName("attach")]
				public string Extra { get; set; }

				[JsonPropertyName("notify_url")]
				public string FallbackUrl { get; set; }

				[JsonPropertyName("amount")]
				public AmountInfo Amount { get; set; }

				[JsonPropertyName("settle_info")]
				public SettlementInfo Settlement { get; set; }

				[JsonPropertyName("detail")]
				public OrderInfo? Order { get; set; }

				[JsonPropertyName("scene_info")]
				public PlaceInfo Place { get; set; }
				#endregion

				#region 嵌套结构
				public struct AmountInfo
				{
					#region 构造函数
					public AmountInfo(int value, string currency = null)
					{
						this.Value = value;
						this.Currency = currency;
					}

					public AmountInfo(decimal value, string currency = null)
					{
						this.Value = (int)(value * 100);
						this.Currency = currency;
					}
					#endregion

					#region 公共属性
					[JsonPropertyName("total")]
					public int Value { get; set; }

					[JsonPropertyName("currency")]
					public string Currency { get; set; }
					#endregion

					#region 重写方法
					public override string ToString() => string.IsNullOrEmpty(this.Currency) ? this.Value.ToString() : $"{this.Currency}:{this.Value}";
					#endregion

					#region 符号重写
					public static implicit operator AmountInfo(int value) => new AmountInfo(value);
					public static implicit operator AmountInfo(decimal value) => new AmountInfo(value);
					#endregion
				}

				public struct SettlementInfo
				{
					public SettlementInfo(bool profitSharingEnabled = false)
					{
						this.ProfitSharingEnabled = profitSharingEnabled;
					}

					[JsonPropertyName("profit_sharing")]
					public bool ProfitSharingEnabled { get; set; }
				}

				public struct OrderInfo
				{
					[JsonPropertyName("invoice_id")]
					public string OrderId { get; set; }

					[JsonPropertyName("cost_price")]
					public int Amount { get; set; }

					[JsonPropertyName("goods_detail")]
					public OrderDetailInfo[] Details { get; set; }
				}

				public struct OrderDetailInfo
				{
					[JsonPropertyName("merchant_goods_id")]
					public string ItemCode { get; set; }

					[JsonPropertyName("goods_name")]
					public string ItemName { get; set; }

					[JsonPropertyName("quantity")]
					public int Quantity { get; set; }

					[JsonPropertyName("unit_price")]
					public int UnitPrice { get; set; }
				}

				public struct PlaceInfo
				{
					public PlaceInfo(string deviceId, StoreInfo? store = null)
					{
						this.DeviceId = deviceId;
						this.DeviceIp = "127.0.0.1";
						this.Store = store;
					}

					[JsonPropertyName("device_id")]
					public string DeviceId { get; set; }
					[JsonPropertyName("payer_client_ip")]
					public string DeviceIp { get; set; }
					[JsonPropertyName("store_info")]
					public StoreInfo? Store { get; set; }
				}

				public struct StoreInfo
				{
					[JsonPropertyName("id")]
					public string StoreId { get; set; }
					[JsonPropertyName("name")]
					public string Name { get; set; }
					[JsonPropertyName("address")]
					public string Address { get; set; }
				}
				#endregion

				#region 嵌套子类
				public abstract class TicketRequest : PaymentRequest
				{
					#region 构造函数
					protected TicketRequest(string voucher, decimal amount, string ticketId, string description = null) : base(voucher, amount, null, description) => this.TicketId = ticketId;
					protected TicketRequest(string voucher, decimal amount, string currency, string ticketId, string description = null) : base(voucher, amount, currency, description) => this.TicketId = ticketId;
					#endregion

					#region 公共属性
					[JsonPropertyName("auth_code")]
					public string TicketId { get; set; }
					#endregion
				}
				#endregion
			}

			public abstract class PaymentOrder
			{
				#region 构造函数
				protected PaymentOrder() { }
				#endregion

				#region 公共属性
				public abstract AuthorityToken Merchant { get; }
				public virtual AuthorityToken? Subsidiary { get; }
				public abstract string Payer { get; }

				[JsonPropertyName("transaction_id")]
				public string Key { get; set; }

				[JsonPropertyName("trade_type")]
				public string Kind { get; set; }

				[JsonPropertyName("trade_state")]
				public string Status { get; set; }

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
			}
			#endregion

			internal class DirectPaymentService : PaymentService
			{
				#region 常量定义
				/// <summary>表示直连商户数据格式的标识。</summary>
				internal const string FORMAT = "direct";
				#endregion

				#region 成员字段
				private HttpClient _client;
				#endregion

				#region 构造函数
				public DirectPaymentService(IAuthority authority) : base(authority)
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
				public override async ValueTask<OperationResult<PaymentOrder>> GetAsync(string voucher, CancellationToken cancellation = default)
				{
					var result = await base.GetAsync<DirectOrder>(voucher, $"mchid={_authority.Code}", cancellation);
					return result.Succeed ? OperationResult.Success((PaymentOrder)result.Value) : (OperationResult)result.Failure;
				}

				public override async ValueTask<OperationResult<PaymentOrder>> GetCompletedAsync(string key, CancellationToken cancellation = default)
				{
					var result = await base.GetCompletedAsync<DirectOrder>(key, $"mchid={_authority.Code}", cancellation);
					return result.Succeed ? OperationResult.Success((PaymentOrder)result.Value) : (OperationResult)result.Failure;
				}

				protected override string GetUrl(string path, Scenario? scenario)
				{
					if(scenario == null)
						return path;

					return scenario switch
					{
						Scenario.App => $"{path}/app",
						Scenario.Web => $"{path}/jsapi",
						Scenario.Native => $"{path}/native",
						_ => $"{path}/jsapi",
					};
				}

				protected override object GetCancellation() => new { mchid = _authority.Code };
				#endregion

				private sealed class DirectBuilder : RequestBuilder
				{
					private readonly IAuthority _authority;
					public DirectBuilder(IAuthority authority) => _authority = authority;

					public override PaymentRequest Create(string voucher, decimal amount, string currency, string payer, string description = null)
					{
						return new DirectRequest(voucher, amount, currency, payer, uint.Parse(_authority.Code), _authority.Account.Code, description)
						{
							FallbackUrl = GetFallback(_authority.Code, FORMAT),
						};
					}

					public override PaymentRequest.TicketRequest Ticket(string voucher, decimal amount, string currency, string ticket, string description = null)
					{
						return new DirectTicketRequest(voucher, amount, currency, ticket, uint.Parse(_authority.Code), _authority.Account.Code, description)
						{
							FallbackUrl = GetFallback(_authority.Code, FORMAT),
						};
					}
				}

				private sealed class DirectRequest : PaymentRequest
				{
					#region 构造函数
					internal DirectRequest(string voucher, decimal amount, string payer, uint merchantId, string appId, string description = null) : this(voucher, amount, null, payer, merchantId, appId, description) { }
					internal DirectRequest(string voucher, decimal amount, string currency, string payer, uint merchantId, string appId, string description = null) : base(voucher, amount, currency, description)
					{
						this.MerchantId = merchantId;
						this.AppId = appId;
						this.Payer = new PayerInfo(payer);
					}
					#endregion

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
						public PayerInfo(string openId) => this.OpenId = openId;

						[JsonPropertyName("openid")]
						public string OpenId { get; set; }

						public override string ToString() => this.OpenId;
					}
					#endregion
				}

				public sealed class DirectTicketRequest : PaymentRequest.TicketRequest
				{
					#region 构造函数
					internal DirectTicketRequest(string voucher, decimal amount, string ticket, uint merchantId, string appId, string description = null) : this(voucher, amount, null, ticket, merchantId, appId, description) { }
					internal DirectTicketRequest(string voucher, decimal amount, string currency, string ticket, uint merchantId, string appId, string description = null) : base(voucher, amount, currency, ticket, description)
					{
						this.MerchantId = merchantId;
						this.AppId = appId;
						this.Amount = new AmountInfo(amount, currency);
					}
					#endregion

					#region 公共属性
					[JsonPropertyName("mchid")]
					[JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
					public uint MerchantId { get; set; }

					[JsonPropertyName("appid")]
					public string AppId { get; set; }
					#endregion
				}

				public sealed class DirectOrder : PaymentOrder
				{
					#region 重写属性
					public override AuthorityToken Merchant { get => new AuthorityToken(this.MerchantId.ToString(), this.AppId); }
					public override string Payer { get => this.PayerToken.ToString(); }
					#endregion

					#region 公共属性
					[JsonPropertyName("mchid")]
					[JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
					public uint MerchantId { get; set; }

					[JsonPropertyName("appid")]
					public string AppId { get; set; }

					[JsonPropertyName("payer")]
					public PayerInfo PayerToken { get; set; }
					#endregion

					#region 嵌套结构
					public struct PayerInfo
					{
						public PayerInfo(string openId) => this.OpenId = openId;

						[JsonPropertyName("openid")]
						public string OpenId { get; set; }

						public override string ToString() => this.OpenId;
					}
					#endregion
				}
			}

			internal class BrokerPaymentService : PaymentService
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
				public BrokerPaymentService(IAuthority master, IAuthority subsidiary) : base(master)
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
				public override async ValueTask<OperationResult<PaymentOrder>> GetAsync(string voucher, CancellationToken cancellation = default)
				{
					var result = await base.GetAsync<BrokerOrder>(voucher, $"sp_mchid={_authority.Code}&sub_mchid={_subsidiary.Code}", cancellation);
					return result.Succeed ? OperationResult.Success((PaymentOrder)result.Value) : (OperationResult)result.Failure;
				}

				public override async ValueTask<OperationResult<PaymentOrder>> GetCompletedAsync(string key, CancellationToken cancellation = default)
				{
					var result = await base.GetCompletedAsync<BrokerOrder>(key, $"sp_mchid={_authority.Code}&sub_mchid={_subsidiary.Code}", cancellation);
					return result.Succeed ? OperationResult.Success((PaymentOrder)result.Value) : (OperationResult)result.Failure;
				}

				protected override string GetUrl(string path, Scenario? scenario)
				{
					if(scenario == null)
						return $"partner/{path}";

					return scenario switch
					{
						Scenario.App => $"partner/{path}/app",
						Scenario.Web => $"partner/{path}/jsapi",
						Scenario.Native => $"partner/{path}/native",
						_ => $"partner/{path}/jsapi",
					};
				}

				protected override object GetCancellation() => new { sp_mchid = _authority.Code, sub_mchid = _subsidiary.Code };
				#endregion

				private sealed class BrokerBuilder : RequestBuilder
				{
					private readonly IAuthority _master;
					private readonly IAuthority _subsidiary;

					public BrokerBuilder(IAuthority master, IAuthority subsidiary) { _master = master; _subsidiary = subsidiary; }

					public override PaymentRequest Create(string voucher, decimal amount, string currency, string payer, string description = null)
					{
						return new BrokerRequest(voucher, amount, currency, payer, uint.Parse(_master.Code), _master.Account.Code, uint.Parse(_subsidiary.Code), _subsidiary.Account.Code, description)
						{
							FallbackUrl = GetFallback(_master.Name, FORMAT),
						};
					}

					public override PaymentRequest.TicketRequest Ticket(string voucher, decimal amount, string currency, string ticket, string description = null)
					{
						return new BrokerTicketRequest(voucher, amount, currency, ticket, uint.Parse(_master.Code), _master.Account.Code, uint.Parse(_subsidiary.Code), _subsidiary.Account.Code, description)
						{
							FallbackUrl = GetFallback(_master.Name, FORMAT),
						};
					}
				}

				private sealed class BrokerRequest : PaymentRequest
				{
					#region 构造函数
					internal BrokerRequest(string voucher, decimal amount, string payer, uint merchantId, string appId, uint subMerchantId, string subAppId = null, string description = null) : this(voucher, amount, null, payer, merchantId, appId, subMerchantId, subAppId, description) { }
					internal BrokerRequest(string voucher, decimal amount, string currency, string payer, uint merchantId, string appId, uint subMerchantId, string subAppId = null, string description = null) : base(voucher, amount, currency, description)
					{
						this.MerchantId = merchantId;
						this.AppId = appId;
						this.SubMerchantId = subMerchantId;
						this.SubAppId = subAppId;

						if(string.IsNullOrEmpty(subAppId))
							this.Payer = new PayerInfo(payer);
						else
							this.Payer = new PayerInfo() { SubOpenId = payer };
					}
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

					[JsonPropertyName("payer")]
					public PayerInfo Payer { get; set; }
					#endregion

					#region 嵌套结构
					public struct PayerInfo
					{
						public PayerInfo(string openId)
						{
							this.OpenId = openId;
							this.SubOpenId = null;
						}

						[JsonPropertyName("sp_openid")]
						public string OpenId { get; set; }

						[JsonPropertyName("sub_openid")]
						public string SubOpenId { get; set; }

						public override string ToString()
						{
							if(string.IsNullOrEmpty(this.OpenId))
								return string.IsNullOrEmpty(this.SubOpenId) ? string.Empty : this.SubOpenId;
							else
								return string.IsNullOrEmpty(this.SubOpenId) ? this.OpenId : $"{this.OpenId}:{this.SubOpenId}";
						}
					}
					#endregion
				}

				public sealed class BrokerTicketRequest : PaymentRequest.TicketRequest
				{
					#region 构造函数
					internal BrokerTicketRequest(string voucher, decimal amount, string ticket, uint merchantId, string appId, uint subMerchantId, string subAppId, string description = null) : this(voucher, amount, null, ticket, merchantId, appId, subMerchantId, subAppId, description) { }
					internal BrokerTicketRequest(string voucher, decimal amount, string currency, string ticket, uint merchantId, string appId, uint subMerchantId, string subAppId, string description = null) : base(voucher, amount, currency, ticket, description)
					{
						this.MerchantId = merchantId;
						this.AppId = appId;
						this.SubMerchantId = subMerchantId;
						this.SubAppId = subAppId;
					}
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

				public sealed class BrokerOrder : PaymentOrder
				{
					#region 重写属性
					public override AuthorityToken Merchant { get => new AuthorityToken(this.MerchantId.ToString(), this.AppId); }
					public override AuthorityToken? Subsidiary { get => new AuthorityToken(this.SubMerchantId.ToString(), this.SubAppId); }
					public override string Payer { get => this.PayerToken.ToString(); }
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

					[JsonPropertyName("payer")]
					public PayerInfo PayerToken { get; set; }
					#endregion

					#region 嵌套结构
					public struct PayerInfo
					{
						[JsonPropertyName("sp_openid")]
						public string OpenId { get; set; }

						[JsonPropertyName("sub_openid")]
						public string SubOpenId { get; set; }

						public override string ToString()
						{
							if(string.IsNullOrEmpty(this.OpenId))
								return string.IsNullOrEmpty(this.SubOpenId) ? string.Empty : this.SubOpenId;
							else
								return string.IsNullOrEmpty(this.SubOpenId) ? this.OpenId : $"{this.OpenId}:{this.SubOpenId}";
						}
					}
					#endregion
				}
			}
		}
	}
}
