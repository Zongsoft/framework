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
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Collections.Generic;

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
			public IAuthority Authority { get => _authority; }
			public RequestBuilder Request { get; protected set; }
			#endregion

			#region 保护属性
			protected abstract HttpClient Client { get; }
			#endregion

			#region 公共方法
			public byte[] Signature(string appId, string identifier, out string applet, out string nonce, out long timestamp)
			{
				if(_authority.Certificate == null)
					throw new WechatException($"The '{_authority.Code}' WeChat authority has no credential.");

				nonce = Guid.NewGuid().ToString("N");
				timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();

				if(string.IsNullOrEmpty(appId))
				{
					appId = _authority.Accounts.Default.Code;

					if(string.IsNullOrEmpty(appId))
						throw new WechatException($"The AppId parameter is not specified, the '{_authority.Code}' WeChat authority has no default account defined.");
				}
				else if(_authority.Accounts.TryGetValue(appId, out var account))
					appId = account.Code;
				else
					throw new WechatException($"The specified '{appId}' AppId is undefined in the '{_authority.Code}' WeChat authority.");

				applet = appId;

				var plaintext = $"{appId}\n{timestamp}\n{nonce}\nprepay_id={identifier}\n";
				return _authority.Certificate.Signaturer.Signature(System.Text.Encoding.UTF8.GetBytes(plaintext));
			}

			public virtual async ValueTask<OperationResult> CancelAsync(string voucher, CancellationToken cancellation = default)
			{
				if(string.IsNullOrEmpty(voucher))
					throw new ArgumentNullException(nameof(voucher));

				return await this.Client.PostAsync(this.GetUrl($"transactions/out-trade-no/{voucher}/close"), this.GetCancellation(), cancellation);
			}

			public abstract ValueTask<OperationResult<PaymentOrder>> PayAsync(PaymentRequest request, Scenario scenario, CancellationToken cancellation = default);
			public abstract ValueTask<OperationResult<PaymentOrder>> GetAsync(string voucher, CancellationToken cancellation = default);
			public abstract ValueTask<OperationResult<PaymentOrder>> GetCompletedAsync(string key, CancellationToken cancellation = default);

			/// <summary>初始化人脸识别设备的在线刷脸验证请求。</summary>
			/// <returns>返回人脸识别验证凭证。</returns>
			public async ValueTask<OperationResult> AuthenticateOnlineAsync(string applet, string data, string device, string store, string title, string extra = null, CancellationToken cancellation = default)
			{
				if(string.IsNullOrEmpty(data))
					return OperationResult.Fail("Argument", $"Missing the required data of the recognition authenticate.");

				var request = this.GetAuthenticationOnlineRequest(applet, data, device, store, title, extra);

				if(!request.ContainsKey("sign"))
					request.TryAdd("sign", Utility.Postmark(request, _authority.Secret));

				var response = await HttpClientFactory.Xml.Client.PostAsync(@"https://payapp.weixin.qq.com/face/get_wxpayface_authinfo", request.CreateXmlContent(), cancellation);
				var result = await response.GetXmlContentAsync(cancellation);

				return result != null && result.TryGetValue("authinfo", out var value) && value != null ?
					OperationResult.Success(new
					{
						Value = value,
						AppId = result.TryGetValue("appid", out var appId) ? (string)appId : string.Empty,
						MerchantId = result.TryGetValue("mch_id", out var merchantId) ? (string)merchantId : string.Empty,
						SubAppId = result.TryGetValue("sub_appid", out var subAppId) ? (string)subAppId : string.Empty,
						SubMerchantId = result.TryGetValue("sub_mch_id", out var subMerchantId) ? (string)subMerchantId : string.Empty,
						Expires = result.TryGetValue("expires_in", out var expires) && int.TryParse(expires, out var seconds) ? seconds : 0,
					}) :
					OperationResult.Fail(result.TryGetValue("return_code", out var failureCode) ? failureCode : "Unknown", result.TryGetValue("return_msg", out var message) ? message : null);
			}

			/// <summary>初始化人脸识别设备的离线刷脸验证请求。</summary>
			/// <returns>返回人脸识别验证凭证。</returns>
			public async ValueTask<OperationResult> AuthenticateOfflineAsync(string applet, string data, string device, string organization, CancellationToken cancellation = default)
			{
				if(string.IsNullOrEmpty(data))
					return OperationResult.Fail("Argument", $"Missing the required data of the recognition authenticate.");

				var request = this.GetAuthenticationOfflineRequest(applet, data, device, organization);

				var response = await this.Client.PostAsJsonAsync(@"https://api.mch.weixin.qq.com/v3/offlineface/authinfo", request, cancellation);
				return await response.GetResultAsync<AuthenticateOfflineResult>(cancellation);
			}
			#endregion

			#region 保护方法
			protected async ValueTask<OperationResult<T>> GetAsync<T>(string voucher, string arguments, CancellationToken cancellation = default) where T : PaymentOrder
			{
				if(string.IsNullOrEmpty(voucher))
					throw new ArgumentNullException(nameof(voucher));

				return await this.Client.GetAsync<T>(this.GetUrl($"transactions/out-trade-no/{voucher}?{arguments}"), cancellation);
			}

			public async ValueTask<OperationResult<string>> PayV3Async(PaymentRequest request, Scenario scenario, CancellationToken cancellation = default)
			{
				if(request == null)
					throw new ArgumentNullException(nameof(request));

				var result = await this.Client.PostAsync<PaymentRequest, PayResult>(this.GetUrl("transactions", scenario), request, cancellation);
				return result.Succeed ? OperationResult.Success(result.Value.Code) : (OperationResult)result.Failure;
			}

			protected async ValueTask<OperationResult<T>> GetCompletedAsync<T>(string key, string arguments, CancellationToken cancellation = default) where T : PaymentOrder
			{
				if(string.IsNullOrEmpty(key))
					throw new ArgumentNullException(nameof(key));

				return await this.Client.GetAsync<T>(this.GetUrl($"transactions/id/{key}?{arguments}"), cancellation);
			}

			protected virtual IDictionary<string, object> GetAuthenticationOnlineRequest(string appId, string data, string device, string store, string title, string extra = null)
			{
				return new SortedDictionary<string, object>()
				{
					{ "appid", string.IsNullOrEmpty(appId) ? _authority.Accounts.Default.Code : appId },
					{ "mch_id", _authority.Code },
					{ "now", DateTimeOffset.Now.ToUnixTimeSeconds() },
					{ "version", "1" },
					{ "sign_type", "MD5" },
					{ "nonce_str", Guid.NewGuid().ToString("N") },
					{ "device_id", device },
					{ "store_id", store },
					{ "store_name", title },
					{ "rawdata", data },
					{ "attach", string.IsNullOrEmpty(extra) ? null : extra },
				};
			}

			protected virtual IDictionary<string, object> GetAuthenticationOfflineRequest(string appId, string data, string device, string organization)
			{
				return new SortedDictionary<string, object>()
				{
					{ "sp_appid", string.IsNullOrEmpty(appId) ? _authority.Accounts.Default.Code : appId },
					{ "device_id", device },
					{ "organization_id", organization },
					{ "rawdata", data },
				};
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

			private struct AuthenticateOfflineResult
			{
				[JsonPropertyName("authinfo")]
				[Zongsoft.Serialization.SerializationMember("authinfo")]
				public string Value { get; set; }
			}

			public abstract class RequestBuilder
			{
				public PaymentRequest Create(string appId, string voucher, decimal amount, string payer, string description = null) => this.Create(appId, voucher, amount, null, payer, description);
				public abstract PaymentRequest Create(string appId, string voucher, decimal amount, string currency, string payer, string description = null);

				public PaymentRequest.TicketRequest Ticket(PaymentHandlerBase handler, string appId, string voucher, decimal amount, string ticketCode, string description = null) => this.Ticket(handler, appId, voucher, amount, null, ticketCode, description);
				public abstract PaymentRequest.TicketRequest Ticket(PaymentHandlerBase handler, string appId, string voucher, decimal amount, string currency, string ticketCode, string description = null);
				public PaymentRequest.TicketRequest Ticket(PaymentHandlerBase handler, string appId, string voucher, decimal amount, string ticketCode, PaymentRequest.TicketRequest.BusinessInfo business, string description = null) => this.Ticket(handler, appId, voucher, amount, null, ticketCode, business, description);
				public abstract PaymentRequest.TicketRequest Ticket(PaymentHandlerBase handler, string appId, string voucher, decimal amount, string currency, string ticketCode, PaymentRequest.TicketRequest.BusinessInfo business, string description = null);

				internal abstract string GetFallback();
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
					protected TicketRequest(PaymentHandlerBase handler, string voucher, decimal amount, string ticketCode, string description = null) : base(voucher, amount, null, description)
					{
						this.Handler = handler ?? throw new ArgumentNullException(nameof(handler));
						this.TicketCode = ticketCode;
					}

					protected TicketRequest(PaymentHandlerBase handler, string voucher, decimal amount, string currency, string ticketCode, string description = null) : base(voucher, amount, currency, description)
					{
						this.Handler = handler ?? throw new ArgumentNullException(nameof(handler));
						this.TicketCode = ticketCode;
					}
					#endregion

					#region 公共属性
					[JsonPropertyName("auth_code")]
					public string TicketCode { get; set; }

					[JsonIgnore]
					[Serialization.SerializationMember(Ignored = true)]
					[JsonPropertyName("business")]
					public BusinessInfo? Business { get; set;}

					[JsonIgnore]
					[Serialization.SerializationMember(Ignored = true)]
					public PaymentHandlerBase Handler { get; }
					#endregion

					#region 嵌套结构
					public struct BusinessInfo
					{
						public BusinessInfo(BusinessProduct product, int sceneId)
						{
							this.Product = product;
							this.SceneId = sceneId;
						}

						[JsonPropertyName("business_product_id")]
						public BusinessProduct Product { get; }

						[JsonPropertyName("business_scene_id")]
						public int SceneId { get; }
					}

					public enum BusinessProduct
					{
						K12 = 2,
						Groupon = 11,
					}
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
				public abstract string PayerType { get; }
				public abstract string PayerCode { get; }

				[JsonPropertyName("transaction_id")]
				public string Identifier { get; set; }

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
					public AmountInfo(int value, int paid, string currency = null)
					{
						this.Value = value;
						this.PaidValue = paid;
						this.Currency = string.IsNullOrEmpty(currency) ? "CNY" : currency;
					}

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

			protected abstract class CompatibilityBase<TService> where TService : PaymentService
			{
				#region 成员字段
				private readonly TService _service;
				#endregion

				#region 构造函数
				protected CompatibilityBase(TService service) => _service = service;
				#endregion

				#region 公共属性
				protected TService Service { get => _service; }
				#endregion

				#region 公共方法
				public async ValueTask<OperationResult<PaymentOrder>> PayAsync(PaymentRequest request, Scenario scenario, CancellationToken cancellation = default)
				{
					var dictionary = this.GetRequest(request, scenario);

					if(!dictionary.ContainsKey("sign"))
						dictionary.TryAdd("sign", Utility.Postmark(dictionary, _service._authority.Secret));

					var url = request is PaymentRequest.TicketRequest ?
						@"https://api.mch.weixin.qq.com/pay/micropay" :
						@"https://api.mch.weixin.qq.com/pay/unifiedorder";

					var response = await HttpClientFactory.Xml.Client.PostAsync(url, dictionary.CreateXmlContent(), cancellation);
					var content = await response.GetXmlContentAsync(cancellation);

					//注意：统一下单(Unified)返回的支付单号键名为“prepay_id”，因此需将其转存
					if(content.TryGetValue("prepay_id", out var prepayId))
						content.TryAdd("transaction_id", prepayId);

					if(content == null || !content.TryGetValue("transaction_id", out var id) || string.IsNullOrEmpty(id))
						return OperationResult.Fail(content.TryGetValue("return_code", out var failureCode) ? failureCode : "Unknown", content.TryGetValue("return_msg", out var message) ? message : null);

					//将返回的数据组装成支付单实体
					var order = this.GetOrder(content);

					//注意：因为微信的支付码（含刷脸）支付是无回调，因此必须立即进行支付完成处理
					if(request is PaymentRequest.TicketRequest ticketRequest)
					{
						//从后台线程中进行支付回调处理
						ThreadPool.QueueUserWorkItem(async state =>
							await state.Handler.HandleAsync(state.Order.Merchant.Identifier, state.Order, CancellationToken.None),
							(ticketRequest.Handler, Order:order),
							false);
					}

					return OperationResult.Success(order);
				}
				#endregion

				#region 虚拟方法
				protected virtual IDictionary<string, object> GetRequest(PaymentRequest request, Scenario scenario)
				{
					var dictionary = new SortedDictionary<string, object>
					{
						{ "mch_id", _service._authority.Code },
						{ "out_trade_no", request.VoucherCode },
						{ "sign_type", "MD5" },
						{ "nonce_str", Guid.NewGuid().ToString("N") },
						{ "attach", string.IsNullOrEmpty(request.Extra) ? null : request.Extra },
						{ "device_info", request.Place.DeviceId },
						{ "spbill_create_ip", request.Place.DeviceIp },
						{ "total_fee", request.Amount.Value },
						{ "fee_type", request.Amount.Currency },
						{ "body", request.Description },
						{ "profit_sharing", request.Settlement.ProfitSharingEnabled ? "Y" : "N" },
					};

					if(request is PaymentRequest.TicketRequest ticket)
						dictionary["auth_code"] = ticket.TicketCode;
					else
					{
						dictionary["notify_url"] = this.Service.Request.GetFallback();
						dictionary["trade_type"] = scenario switch
						{
							Scenario.Native => "NATIVE",
							Scenario.App => "APP",
							Scenario.Web => "JSAPI",
							_ => "JSAPI",
						};
					}

					return dictionary;
				}

				protected abstract PaymentOrder CreateOrder(IDictionary<string, string> data);
				#endregion

				#region 私有方法
				private PaymentOrder GetOrder(IDictionary<string, string> data)
				{
					var order = this.CreateOrder(data);
					if(order == null)
						return null;

					if(data.TryGetValue("transaction_id", out var id))
						order.Identifier = id;
					if(data.TryGetValue("trade_type", out var kind))
						order.Kind = kind;
					if(data.TryGetValue("bank_type", out var bankType))
						order.BankType = bankType;
					if(data.TryGetValue("out_trade_no", out var voucherCode))
						order.VoucherCode = voucherCode;
					if(data.TryGetValue("attach", out var extra))
						order.Extra = extra;
					if(data.TryGetValue("time_end", out var finalTime) && DateTime.TryParseExact(finalTime, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var paidTime))
						order.PaidTime = paidTime;
					if(data.TryGetValue("result_code", out var status))
						order.Status = status;
					if(data.TryGetValue("err_code", out var errCode))
						order.StatusDescription = data.TryGetValue("err_code_des", out var errMessage) ? $"[{errCode}]{errMessage}" : errCode;

					var amount = data.TryGetValue("total_fee", out var total) && int.TryParse(total, out var number) ? number : 0;
					var paidAmount = data.TryGetValue("cash_fee", out var cash) && int.TryParse(cash, out number) ? number : amount;
					order.Amount = new PaymentOrder.AmountInfo(amount, paidAmount, data.TryGetValue("fee_type", out var currency) ? currency : null);

					return order;
				}
				#endregion
			}

			internal class DirectPaymentService : PaymentService
			{
				#region 常量定义
				/// <summary>表示直连商户数据格式的标识。</summary>
				internal const string FORMAT = "direct";
				#endregion

				#region 成员字段
				private HttpClient _client;
				private readonly Compatibility _compatibility;
				#endregion

				#region 构造函数
				public DirectPaymentService(IAuthority authority) : base(authority)
				{
					_compatibility = new Compatibility(this);
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
				public override async ValueTask<OperationResult<PaymentOrder>> PayAsync(PaymentRequest request, Scenario scenario, CancellationToken cancellation = default)
				{
					if(_authority.Certificate == null || request is PaymentRequest.TicketRequest)
						return await _compatibility.PayAsync(request, scenario, cancellation);

					var result = await this.PayV3Async(request, scenario, cancellation);
					return result.Succeed ? OperationResult.Success((PaymentOrder)new DirectOrder(result.Value, request)) : result.Failure;
				}

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
						return $"pay/{path}";

					return scenario switch
					{
						Scenario.App => $"pay/{path}/app",
						Scenario.Web => $"pay/{path}/jsapi",
						Scenario.Native => $"pay/{path}/native",
						_ => $"pay/{path}/jsapi",
					};
				}

				protected override object GetCancellation() => new { mchid = _authority.Code };
				#endregion

				private sealed class Compatibility : CompatibilityBase<DirectPaymentService>
				{
					public Compatibility(DirectPaymentService service) : base(service) { }

					protected override IDictionary<string, object> GetRequest(PaymentRequest request, Scenario scenario)
					{
						var dictionary = base.GetRequest(request, scenario);

						if(dictionary != null)
						{
							switch(request)
							{
								case DirectRequest directRequest:
									dictionary["appid"] = directRequest.AppId;
									break;
								case DirectTicketRequest ticketRequest:
									dictionary["appid"] = ticketRequest.AppId;
									break;
							}
						}

						return dictionary;
					}

					protected override PaymentOrder CreateOrder(IDictionary<string, string> data)
					{
						var order = new DirectOrder();

						if(data.TryGetValue("mch_id", out var value) && uint.TryParse(value, out var merchantId))
							order.MerchantId = merchantId;
						if(data.TryGetValue("appid", out var appId))
							order.AppId = appId;
						if(data.TryGetValue("openid", out var openId))
							order.Payer = new DirectOrder.PayerInfo(openId);

						return order;
					}
				}

				private sealed class DirectBuilder : RequestBuilder
				{
					private readonly IAuthority _authority;
					public DirectBuilder(IAuthority authority) => _authority = authority;

					internal override string GetFallback() => GetFallback(_authority.Code, FORMAT);

					public override PaymentRequest Create(string appId, string voucher, decimal amount, string currency, string payer, string description = null)
					{
						if(string.IsNullOrEmpty(appId))
							appId = _authority.Accounts.Default.Code;

						return new DirectRequest(voucher, amount, currency, payer, uint.Parse(_authority.Code), appId, description)
						{
							FallbackUrl = this.GetFallback(),
						};
					}

					public override PaymentRequest.TicketRequest Ticket(PaymentHandlerBase handler, string appId, string voucher, decimal amount, string currency, string ticketCode, string description = null)
					{
						if(string.IsNullOrEmpty(appId))
							appId = _authority.Accounts.Default.Code;

						return new DirectTicketRequest(handler, voucher, amount, currency, ticketCode, uint.Parse(_authority.Code), appId, description)
						{
							FallbackUrl = this.GetFallback(),
						};
					}

					public override PaymentRequest.TicketRequest Ticket(PaymentHandlerBase handler, string appId, string voucher, decimal amount, string currency, string ticketCode, PaymentRequest.TicketRequest.BusinessInfo business, string description = null)
					{
						if(string.IsNullOrEmpty(appId))
							appId = _authority.Accounts.Default.Code;

						return new DirectTicketRequest(handler, voucher, amount, currency, ticketCode, uint.Parse(_authority.Code), appId, description)
						{
							Business = business,
							FallbackUrl = this.GetFallback(),
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
					internal DirectTicketRequest(PaymentHandlerBase handler, string voucher, decimal amount, string ticket, uint merchantId, string appId, string description = null) : this(handler, voucher, amount, null, ticket, merchantId, appId, description) { }
					internal DirectTicketRequest(PaymentHandlerBase handler, string voucher, decimal amount, string currency, string ticket, uint merchantId, string appId, string description = null) : base(handler, voucher, amount, currency, ticket, description)
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
					#region 构造函数
					public DirectOrder() { }
					public DirectOrder(string identifier, PaymentRequest request)
					{
						this.Identifier = identifier;

						if(request is DirectRequest directRequest)
						{
							this.Payer = new PayerInfo(directRequest.Payer.OpenId);
							this.AppId = directRequest.AppId;
							this.MerchantId = directRequest.MerchantId;
						}
					}
					#endregion

					#region 重写属性
					public override AuthorityToken Merchant { get => new AuthorityToken(this.MerchantId.ToString(), this.AppId); }
					public override string PayerCode => this.Payer.OpenId;
					public override string PayerType => "OpenId";
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
				private readonly Compatibility _compatibility;
				#endregion

				#region 构造函数
				public BrokerPaymentService(IAuthority master, IAuthority subsidiary) : base(master)
				{
					_compatibility = new Compatibility(this);
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
				public override async ValueTask<OperationResult<PaymentOrder>> PayAsync(PaymentRequest request, Scenario scenario, CancellationToken cancellation = default)
				{
					if(_authority.Certificate == null)
					{
						if(request is PaymentRequest.TicketRequest ticketRequest && ticketRequest.Business.HasValue)
							return await PayOfflineTicketAsync(ticketRequest, cancellation);
						else
							return await _compatibility.PayAsync(request, scenario, cancellation);
					}

					var result = await this.PayV3Async(request, scenario, cancellation);
					return result.Succeed ? OperationResult.Success((PaymentOrder)new BrokerOrder(result.Value, request)) : result.Failure;
				}

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
						return $"pay/partner/{path}";

					return scenario switch
					{
						Scenario.App => $"pay/partner/{path}/app",
						Scenario.Web => $"pay/partner/{path}/jsapi",
						Scenario.Native => $"pay/partner/{path}/native",
						_ => $"pay/partner/{path}/jsapi",
					};
				}

				protected override object GetCancellation() => new { sp_mchid = _authority.Code, sub_mchid = _subsidiary.Code };

				protected override IDictionary<string, object> GetAuthenticationOnlineRequest(string appId, string data, string device, string store, string title, string extra = null)
				{
					var request = base.GetAuthenticationOnlineRequest(appId, data, device, store, title, extra);

					if(request != null)
					{
						request["sub_mch_id"] = _subsidiary.Code;
						request["sub_appid"] = _subsidiary.Accounts.Default.Code;
					}

					return request;
				}

				protected override IDictionary<string, object> GetAuthenticationOfflineRequest(string appId, string data, string device, string organization)
				{
					var request = base.GetAuthenticationOfflineRequest(appId, data, device, organization);

					if(request != null)
					{
						request["sub_mchid"] = _subsidiary.Code;
						request["sub_appid"] = _subsidiary.Accounts.Default.Code;
					}

					return request;
				}
				#endregion

				#region 私有方法
				private async ValueTask<OperationResult<PaymentOrder>> PayOfflineTicketAsync(PaymentRequest.TicketRequest request, CancellationToken cancellation = default)
				{
					var result = await this.Client.PostAsync<OfflineTicketRequest, BrokerOrder>(@"https://api.mch.weixin.qq.com/v3/offlineface/transactions", OfflineTicketRequest.Create(request), cancellation);
					return result.Succeed ? OperationResult.Success((PaymentOrder)result.Value) : result.Failure;
				}

				private class OfflineTicketRequest
				{
					[JsonPropertyName("out_trade_no")]
					public string VoucherCode { get; set; }
					[JsonPropertyName("auth_code")]
					public string TicketCode { get; set; }
					[JsonPropertyName("amount")]
					public PaymentRequest.AmountInfo Amount { get; set; }
					[JsonPropertyName("scene_info")]
					public PaymentRequest.PlaceInfo Place { get; set; }
					[JsonPropertyName("attach")]
					public string Extra { get; set; }
					[JsonPropertyName("description")]
					public string Description { get; set; }
					[JsonPropertyName("settle_info")]
					public PaymentRequest.SettlementInfo Settlement { get; set; }
					[JsonPropertyName("business")]
					public PaymentRequest.TicketRequest.BusinessInfo? Business { get; set; }

					public static OfflineTicketRequest Create(PaymentRequest.TicketRequest request)
					{
						if(request == null || request.Business == null)
							return default;

						return new OfflineTicketRequest()
						{
							VoucherCode = request.VoucherCode,
							TicketCode = request.TicketCode,
							Amount = request.Amount,
							Place = request.Place,
							Settlement = request.Settlement,
							Extra = string.IsNullOrEmpty(request.Extra) ? request.Description : request.Extra,
							Description = string.IsNullOrEmpty(request.Description) ? request.Extra : request.Description,
							Business = request.Business.Value,
						};
					}
				}
				#endregion

				private sealed class Compatibility : CompatibilityBase<BrokerPaymentService>
				{
					public Compatibility(BrokerPaymentService service) : base(service) { }

					protected override IDictionary<string, object> GetRequest(PaymentRequest request, Scenario scenario)
					{
						var dictionary = base.GetRequest(request, scenario);

						if(dictionary != null)
						{
							switch(request)
							{
								case BrokerRequest brokerRequest:
									dictionary["appid"] = brokerRequest.AppId;
									break;
								case BrokerTicketRequest ticketRequest:
									dictionary["appid"] = ticketRequest.AppId;
									break;
							}

							dictionary["sub_mch_id"] = this.Service._subsidiary.Code;
							dictionary["sub_appid"] = this.Service._subsidiary.Accounts?.Default.Code;
						}

						return dictionary;
					}

					protected override PaymentOrder CreateOrder(IDictionary<string, string> data)
					{
						var order = new BrokerOrder();

						if(data.TryGetValue("mch_id", out var value) && uint.TryParse(value, out var merchantId))
							order.MerchantId = merchantId;
						if(data.TryGetValue("sub_mch_id", out value) && uint.TryParse(value, out var subMerchantId))
							order.SubMerchantId = subMerchantId;
						if(data.TryGetValue("appid", out var appId))
							order.AppId = appId;
						if(data.TryGetValue("sub_appid", out var subAppId))
							order.SubAppId = subAppId;
						if(data.TryGetValue("openid", out var openId))
						{
							if(data.TryGetValue("sub_openid", out var subOpenId))
								order.Payer = new BrokerOrder.PayerInfo(openId, subOpenId);
							else
								order.Payer = new BrokerOrder.PayerInfo(openId, null);
						}

						return order;
					}
				}

				private sealed class BrokerBuilder : RequestBuilder
				{
					private readonly IAuthority _master;
					private readonly IAuthority _subsidiary;

					public BrokerBuilder(IAuthority master, IAuthority subsidiary) { _master = master; _subsidiary = subsidiary; }

					internal override string GetFallback() => GetFallback(_master.Code, FORMAT);

					public override PaymentRequest Create(string appId, string voucher, decimal amount, string currency, string payer, string description = null)
					{
						if(string.IsNullOrEmpty(appId))
							appId = _master.Accounts.Default.Code;

						return new BrokerRequest(voucher, amount, currency, payer, uint.Parse(_master.Code), appId, uint.Parse(_subsidiary.Code), _subsidiary.Accounts?.Default.Code, description)
						{
							FallbackUrl = this.GetFallback(),
						};
					}

					public override PaymentRequest.TicketRequest Ticket(PaymentHandlerBase handler, string appId, string voucher, decimal amount, string currency, string ticketCode, string description = null)
					{
						if(string.IsNullOrEmpty(appId))
							appId = _master.Accounts.Default.Code;

						return new BrokerTicketRequest(handler, voucher, amount, currency, ticketCode, uint.Parse(_master.Code), appId, uint.Parse(_subsidiary.Code), _subsidiary.Accounts?.Default.Code, description)
						{
							FallbackUrl = this.GetFallback(),
						};
					}

					public override PaymentRequest.TicketRequest Ticket(PaymentHandlerBase handler, string appId, string voucher, decimal amount, string currency, string ticketCode, PaymentRequest.TicketRequest.BusinessInfo business, string description = null)
					{
						if(string.IsNullOrEmpty(appId))
							appId = _master.Accounts.Default.Code;

						return new BrokerTicketRequest(handler, voucher, amount, currency, ticketCode, uint.Parse(_master.Code), appId, uint.Parse(_subsidiary.Code), _subsidiary.Accounts?.Default.Code, description)
						{
							Business = business,
							FallbackUrl = this.GetFallback(),
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
					internal BrokerTicketRequest(PaymentHandlerBase handler, string voucher, decimal amount, string ticket, uint merchantId, string appId, uint subMerchantId, string subAppId, string description = null) : this(handler, voucher, amount, null, ticket, merchantId, appId, subMerchantId, subAppId, description) { }
					internal BrokerTicketRequest(PaymentHandlerBase handler, string voucher, decimal amount, string currency, string ticket, uint merchantId, string appId, uint subMerchantId, string subAppId, string description = null) : base(handler, voucher, amount, currency, ticket, description)
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
					#region 构造函数
					public BrokerOrder() { }
					public BrokerOrder(string identifier, PaymentRequest request)
					{
						this.Identifier = identifier;

						if(request is BrokerRequest brokerRequest)
						{
							this.Payer = new PayerInfo(brokerRequest.Payer.OpenId, brokerRequest.Payer.SubOpenId);
							this.AppId = brokerRequest.AppId;
							this.SubAppId = brokerRequest.SubAppId;
							this.MerchantId = brokerRequest.MerchantId;
							this.SubMerchantId = brokerRequest.SubMerchantId;
						}
					}
					#endregion

					#region 重写属性
					public override AuthorityToken Merchant { get => new AuthorityToken(this.MerchantId.ToString(), this.AppId); }
					public override AuthorityToken? Subsidiary { get => new AuthorityToken(this.SubMerchantId.ToString(), this.SubAppId); }
					public override string PayerCode => this.Payer.ToString();
					public override string PayerType => "OpenId";
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
						public PayerInfo(string openId, string subOpenId = null)
						{
							this.OpenId = openId;
							this.SubOpenId = subOpenId;
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
			}
		}
	}
}
