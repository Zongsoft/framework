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
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Security;
using Zongsoft.Services;

namespace Zongsoft.Externals.Wechat.Paying
{
	partial class Director
	{
		public class PaymentService
		{
			#region 私有变量
			private readonly IAuthority _authority;
			#endregion

			#region 构造函数
			internal PaymentService(IAuthority authority)
			{
				_authority = authority ?? throw new ArgumentNullException(nameof(authority));
				this.Request = new RequestBuilder(_authority);
			}
			#endregion

			#region 公共属性
			public RequestBuilder Request { get; }
			#endregion

			#region 公共方法
			public async Task<OperationResult<string>> PayAsync(PaymentRequest request, Scenario scenario, CancellationToken cancellation = default)
			{
				if(request == null)
					throw new ArgumentNullException(nameof(request));

				var client = _authority.GetHttpClient();
				var response = await client.PostAsJsonAsync(GetUrl(request.GetType().Name, "transactions", scenario), request, Json.Default, cancellation);

				if(response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<PayResult>(null, cancellation);
					return OperationResult.Success(result.Code);
				}

				return OperationResult.Fail((int)response.StatusCode, response.ReasonPhrase, await response.Content.ReadAsStringAsync(cancellation));
			}

			public async Task<OperationResult> Cancel(string voucher, Scenario scenario, CancellationToken cancellation = default)
			{
				if(string.IsNullOrEmpty(voucher))
					throw new ArgumentNullException(nameof(voucher));

				var client = _authority.GetHttpClient();
				var response = await client.PostAsJsonAsync(GetUrl(null, $"transactions/out-trade-no/{voucher}/close", scenario), new { mchid = _authority.Code }, Json.Default, cancellation);
				return await response.GetResultAsync(cancellation);
			}

			public async Task<OperationResult> Cancel(string subsidiary, string voucher, Scenario scenario, CancellationToken cancellation = default)
			{
				if(string.IsNullOrEmpty(voucher))
					throw new ArgumentNullException(nameof(voucher));

				var client = _authority.GetHttpClient();
				var response = await client.PostAsJsonAsync(GetUrl("Broker", $"transactions/out-trade-no/{voucher}/close", scenario), new { sp_mchid = _authority.Code, sub_mchid = subsidiary }, Json.Default, cancellation);
				return await response.GetResultAsync(cancellation);
			}

			public async Task<OperationResult<T>> GetAsync<T>(string voucher, Scenario scenario, CancellationToken cancellation = default) where T : PaymentOrder
			{
				if(string.IsNullOrEmpty(voucher))
					throw new ArgumentNullException(nameof(voucher));

				var client = _authority.GetHttpClient();
				var response = await client.GetAsync(GetUrl(typeof(T).Name, $"transactions/out-trade-no/{voucher}", scenario), cancellation);
				return await response.GetResultAsync<T>(cancellation);
			}

			public async Task<OperationResult<T>> GetCompletedAsync<T>(string id, Scenario scenario, CancellationToken cancellation = default) where T : PaymentOrder
			{
				if(string.IsNullOrEmpty(id))
					throw new ArgumentNullException(nameof(id));

				var client = _authority.GetHttpClient();
				var response = await client.GetAsync(GetUrl(typeof(T).Name, $"transactions/id/{id}", scenario), cancellation);
				return await response.GetResultAsync<T>();
			}
			#endregion

			#region 私有方法
			private static string GetUrl(string mode, string path, Scenario scenario)
			{
				if(mode != null && mode.EndsWith("Broker"))
					path = "partner/" + path;

				return scenario switch
				{
					Scenario.App => $"{path}/app",
					Scenario.Web => $"{path}/jsapi",
					Scenario.Native => "{path}/native",
					_ => $"{path}/jsapi",
				};
			}
			#endregion

			#region 嵌套子类
			private struct PayResult
			{
				[JsonPropertyName("prepay_id")]
				public string Code { get; set; }
			}

			public class RequestBuilder
			{
				private readonly IAuthority _authority;
				public RequestBuilder(IAuthority authority) => _authority = authority;

				public PaymentRequest Create(string payer, uint merchantId, string appId = null)
				{
					return new PaymentRequest.BrokerRequest()
					{
						MerchantId = uint.Parse(_authority.Code),
						AppId = _authority.Applet.Name,
						SubMerchantId = merchantId,
						SubAppId = appId,
						Payer = new PaymentRequest.BrokerRequest.PayerInfo() { OpenId = payer },
					};
				}

				public PaymentRequest.TicketRequest Ticket(string ticket, uint merchantId, string appId = null)
				{
					return new PaymentRequest.TicketRequest.BrokerTicketRequest(ticket)
					{
						MerchantId = uint.Parse(_authority.Code),
						AppId = _authority.Applet.Name,
						SubMerchantId = merchantId,
						SubAppId = appId,
					};
				}
			}

			public abstract class PaymentRequest
			{
				#region 构造函数
				protected PaymentRequest() { }
				#endregion

				#region 公共属性
				[JsonPropertyName("description")]
				public string Description { get; set; }

				[JsonPropertyName("out_trade_no")]
				public string VoucherCode { get; set; }

				[JsonPropertyName("time_expire")]
				public DateTime Expiration { get; set; }

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
					public static implicit operator AmountInfo (int value) => new AmountInfo(value);
					public static implicit operator AmountInfo (decimal value) => new AmountInfo(value);
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
				public sealed class DirectRequest : PaymentRequest
				{
					#region 构造函数
					public DirectRequest(string payer, uint merchantId, string appId)
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
					}
					#endregion
				}

				public sealed class BrokerRequest : PaymentRequest
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
						public PayerInfo(string openId)
						{
							this.OpenId = openId;
							this.SubOpenId = null;
						}

						[JsonPropertyName("sp_openid")]
						public string OpenId { get; set; }

						[JsonPropertyName("sub_openid")]
						public string SubOpenId { get; set; }
					}
					#endregion
				}

				public abstract class TicketRequest : PaymentRequest
				{
					#region 构造函数
					protected TicketRequest(string ticketId) => this.TicketId = ticketId;
					#endregion

					#region 公共属性
					[JsonPropertyName("auth_code")]
					public string TicketId { get; set; }
					#endregion

					#region 嵌套子类
					public sealed class DirectTicketRequest : TicketRequest
					{
						#region 构造函数
						public DirectTicketRequest(string ticket, uint merchantId, string appId) : base(ticket)
						{
							this.MerchantId = merchantId;
							this.AppId = appId;
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

					public sealed class BrokerTicketRequest : TicketRequest
					{
						#region 构造函数
						public BrokerTicketRequest(string ticket) : base(ticket) { }
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
				#endregion
			}

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
						public PayerInfo(string openId) => this.OpenId = openId;

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
			#endregion
		}
	}
}
