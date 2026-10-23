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
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Collections.Generic;

using Zongsoft.Common;
using System.Text.Json;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public abstract partial class PaymentService
	{
		#region 私有变量
		private readonly IAuthority _authority;
		#endregion

		#region 构造函数
		protected PaymentService(IAuthority authority, IAuthority subsidiary = null)
		{
			_authority = authority ?? throw new ArgumentNullException(nameof(authority));
			this.Tickets = new TicketService(this, subsidiary);
		}
		#endregion

		#region 公共属性
		public IAuthority Authority { get => _authority; }
		public RequestBuilder Request { get; protected set; }
		public TicketService Tickets { get; }
		#endregion

		#region 保护属性
		protected abstract HttpClient Client { get; }
		#endregion

		#region 公共方法
		public virtual ValueTask CancelAsync(string voucher, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(voucher))
				throw new ArgumentNullException(nameof(voucher));

			return this.Client.PostAsync(this.GetUrl($"transactions/out-trade-no/{Uri.EscapeDataString(voucher)}/close"), this.GetCancellation(), cancellation);
		}

		public abstract ValueTask<PaymentResult> PayAsync(PaymentRequest request, Scenario scenario, CancellationToken cancellation = default);
		public abstract ValueTask<PaymentOrder> GetAsync(string voucher, CancellationToken cancellation = default);
		public abstract ValueTask<PaymentOrder> GetCompletedAsync(string key, CancellationToken cancellation = default);

		/// <summary>初始化人脸识别设备的在线刷脸验证请求。</summary>
		/// <param name="applet">发起刷脸验证的应用编号。</param>
		/// <param name="data">人脸识别设备采集的原始验证数据。</param>
		/// <param name="device">人脸识别设备编号。</param>
		/// <param name="store">门店编号。</param>
		/// <param name="title">门店名称。</param>
		/// <param name="extra">附加验证信息。</param>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回人脸识别验证凭证。</returns>
		public async ValueTask<object> AuthenticateOnlineAsync(string applet, string data, string device, string store, string title, string extra = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(data))
				throw new ArgumentNullException(nameof(data), Properties.Resources.Recognition_DataRequired_Message);

			var request = this.GetAuthenticationOnlineRequest(applet, data, device, store, title, extra);

			var result = await HttpClientFactory.Xml.SendAsync(_authority, "https://payapp.weixin.qq.com/face/get_wxpayface_authinfo", request, false, cancellation);
			TicketService.ValidateIdentity(result, request, true);
			var seconds = 0;

			if(result.TryGetValue("expires_in", out var expires) && (!int.TryParse(expires, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out seconds) || seconds <= 0))
				throw new FormatException();

			if(result.TryGetValue("authinfo", out var value) && !string.IsNullOrWhiteSpace(value) && value.Length <= 4096)
				return new
				{
					Value = value,
					AppId = result.TryGetValue("appid", out var appId) ? (string)appId : string.Empty,
					MerchantId = (string)request["mch_id"],
					SubAppId = result.TryGetValue("sub_appid", out var subAppId) ? (string)subAppId : string.Empty,
					SubMerchantId = result.TryGetValue("sub_mch_id", out var subMerchantId) ? (string)subMerchantId : string.Empty,
					Expires = seconds,
				};

			throw new FormatException();
		}

		/// <summary>初始化人脸识别设备的离线刷脸验证请求。</summary>
		/// <param name="applet">发起刷脸验证的应用编号。</param>
		/// <param name="data">人脸识别设备采集的原始验证数据。</param>
		/// <param name="device">人脸识别设备编号。</param>
		/// <param name="organization">刷脸支付业务的机构编号。</param>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回人脸识别验证凭证。</returns>
		public async ValueTask<object> AuthenticateOfflineAsync(string applet, string data, string device, string organization, CancellationToken cancellation = default)
		{
			var request = this.GetAuthenticationOfflineRequest(applet, data, device, organization);
			var result = await this.Client.PostAsync<IDictionary<string, object>, AuthenticateOfflineResult>("offlineface/authinfo", request, cancellation);
			if(string.IsNullOrWhiteSpace(result.Value) || result.Value.Length > 4096)
				throw new FormatException();

			return new
			{
				Value = result.Value,
				OrganizationId = organization,
				AppId = (string)request["sp_appid"],
				MerchantId = _authority.Code,
				SubAppId = request.TryGetValue("sub_appid", out var subAppId) ? (string)subAppId : null,
				SubMerchantId = (string)request["sub_mchid"],
			};
		}
		#endregion

		#region 保护方法
		protected async ValueTask<T> GetAsync<T>(string voucher, string arguments, CancellationToken cancellation = default) where T : PaymentOrder
		{
			if(string.IsNullOrEmpty(voucher))
				throw new ArgumentNullException(nameof(voucher));

			var order = await this.Client.GetAsync<T>(this.GetUrl($"transactions/out-trade-no/{Uri.EscapeDataString(voucher)}?{arguments}"), cancellation);
			this.ValidateOrder(order);
			if(!string.Equals(order.VoucherCode, voucher, StringComparison.Ordinal))
				throw new FormatException();

			return order;
		}

		public async ValueTask<PaymentResult> PayV3Async(PaymentRequest request, Scenario scenario, CancellationToken cancellation = default)
		{
			if(request == null)
				throw new ArgumentNullException(nameof(request));

			var data = this.GetPaymentRequest(request, scenario, out var application);
			var result = await this.Client.PostAsync<IDictionary<string, object>, PrepaymentResponse>(this.GetUrl("transactions", scenario), data, cancellation);
			return new PaymentResult(result.PrepaymentId, result.CodeUrl, scenario, _authority, application);
		}

		protected async ValueTask<T> GetCompletedAsync<T>(string key, string arguments, CancellationToken cancellation = default) where T : PaymentOrder
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			var order = await this.Client.GetAsync<T>(this.GetUrl($"transactions/id/{Uri.EscapeDataString(key)}?{arguments}"), cancellation);
			this.ValidateOrder(order);
			if(!string.Equals(order.Identifier, key, StringComparison.Ordinal))
				throw new FormatException();

			return order;
		}

		protected virtual IDictionary<string, object> GetAuthenticationOnlineRequest(string appId, string data, string device, string store, string title, string extra = null)
		{
			if(string.IsNullOrWhiteSpace(data) || data.Length > 2048 || string.IsNullOrWhiteSpace(device) || device.Length > 32 ||
				string.IsNullOrWhiteSpace(store) || store.Length > 32 || string.IsNullOrWhiteSpace(title) || System.Text.Encoding.UTF8.GetByteCount(title) > 128)
				throw new ArgumentException();

			var request = this.Tickets.GetIdentity(appId);
			request["now"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			request["version"] = "1";
			request["device_id"] = device;
			request["store_id"] = store;
			request["store_name"] = title;
			request["rawdata"] = data;
			request["attach"] = extra;
			return request;
		}

		protected virtual IDictionary<string, object> GetAuthenticationOfflineRequest(string appId, string data, string device, string organization) => throw new NotSupportedException();
		#endregion

		#region 抽象方法
		protected abstract string GetUrl(string path, Scenario? scenario = null);
		protected abstract object GetCancellation();
		#endregion

		#region 模型类型
		private struct PrepaymentResponse
		{
			[JsonPropertyName("prepay_id")]
			public string PrepaymentId { get; set; }

			[JsonPropertyName("code_url")]
			public string CodeUrl { get; set; }
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

			public PaymentRequest.TicketRequest Ticket(string appId, string voucher, decimal amount, string ticketCode, string description = null) => this.Ticket(appId, voucher, amount, null, ticketCode, description);
			public abstract PaymentRequest.TicketRequest Ticket(string appId, string voucher, decimal amount, string currency, string ticketCode, string description = null);
			public PaymentRequest.TicketRequest Ticket(string appId, string voucher, decimal amount, string ticketCode, PaymentRequest.TicketRequest.BusinessInfo business, string description = null) => this.Ticket(appId, voucher, amount, null, ticketCode, business, description);
			public abstract PaymentRequest.TicketRequest Ticket(string appId, string voucher, decimal amount, string currency, string ticketCode, PaymentRequest.TicketRequest.BusinessInfo business, string description = null);

			internal abstract string GetFallback();
			internal static string GetFallback(string key)
			{
				var url = Utility.GetOptions<Options.FallbackOptions>($"/Externals/Wechat/Fallback")?.Url;

				if(string.IsNullOrWhiteSpace(url))
					return null;

				return string.Format(url, "payment", key);
			}
		}

		public abstract class PaymentRequest
		{
			#region 成员字段
			private string _description;
			#endregion

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
			public string Description { get => _description; set => _description = Utility.Truncate(value, 127); }

			[JsonPropertyName("time_expire")]
			public DateTime? Expiration { get; set; }

			[JsonPropertyName("attach")]
			public string Extra { get; set; }

			[JsonPropertyName("notify_url")]
			public string FallbackUrl { get; set; }

			[JsonPropertyName("amount")]
			public AmountInfo Amount
			{
				get;
				set
				{
					Money.Validate(value.Value, nameof(value));
					Money.Currency(value.Currency);
					field = value;
				}
			}

			[JsonPropertyName("settle_info")]
			public SettlementInfo Settlement { get; set; }

			[JsonPropertyName("detail")]
			public OrderInfo? Order { get; set; }

			[JsonPropertyName("scene_info")]
			public PlaceInfo Place { get; set; }
			#endregion

			#region 嵌套结构
			public readonly struct AmountInfo
			{
				#region 构造函数
				[JsonConstructor]
				public AmountInfo(int value, string currency = null)
				{
					this.Value = Money.Validate(value, nameof(value));
					this.Currency = Money.Currency(currency);
				}

				public AmountInfo(decimal value, string currency = null) : this(Money.Convert(value, nameof(value)), currency) { }
				#endregion

				#region 公共属性
				[JsonPropertyName("total")]
				public int Value { get; }

				[JsonPropertyName("currency")]
				public string Currency { get; }
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
				private string _itemName;

				[JsonPropertyName("merchant_goods_id")]
				public string ItemCode { get; set; }

				[JsonPropertyName("goods_name")]
				public string ItemName { get => _itemName; set => _itemName = Utility.Truncate(value, 256); }

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
					this.DeviceIp = null;
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
				protected TicketRequest(string voucher, decimal amount, string ticketCode, string description = null) : base(voucher, amount, null, description)
				{
					this.TicketCode = ticketCode;
				}

				protected TicketRequest(string voucher, decimal amount, string currency, string ticketCode, string description = null) : base(voucher, amount, currency, description)
				{
					this.TicketCode = ticketCode;
				}
				#endregion

				#region 公共属性
				[JsonPropertyName("auth_code")]
				public string TicketCode { get; set; }

				[JsonIgnore]
				[Serialization.SerializationMember(Ignored = true)]
				[JsonPropertyName("business")]
				public BusinessInfo? Business { get; set; }
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

				[JsonConverter(typeof(BusinessProductConverter))]
				public enum BusinessProduct
				{
					K12 = 2,
					Groupon = 11,
				}

				private class BusinessProductConverter : JsonConverter<BusinessProduct>
				{
					public override BusinessProduct Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
					{
						if(reader.TokenType == JsonTokenType.Number)
							return (BusinessProduct)reader.GetInt32();
						else if(reader.TokenType == JsonTokenType.String)
							return Enum.TryParse<BusinessProduct>(reader.GetString(), true, out var result) ? result : BusinessProduct.K12;
						else
							return BusinessProduct.K12;
					}

					public override void Write(Utf8JsonWriter writer, BusinessProduct value, JsonSerializerOptions options)
					{
						writer.WriteNumberValue((int)value);
					}
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
			public virtual string StatusDescription { get; set; }

			[JsonPropertyName("bank_type")]
			public string BankType { get; set; }

			[JsonPropertyName("out_trade_no")]
			public string VoucherCode { get; set; }

			[JsonPropertyName("success_time")]
			public DateTime? PaidTime { get; set; }

			[JsonPropertyName("attach")]
			public string Extra { get; set; }

			[JsonPropertyName("amount")]
			public virtual AmountInfo Amount { get; set; }

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

		internal partial class DirectPaymentService : PaymentService
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
							_client = HttpClientFactory.GetHttpClient(_authority);
					}

					return _client;
				}
			}
			#endregion

			#region 重写方法
			public override async ValueTask<PaymentResult> PayAsync(PaymentRequest request, Scenario scenario, CancellationToken cancellation = default)
			{
				if(request is PaymentRequest.TicketRequest)
					return new PaymentResult(await this.Tickets.PayAsync((PaymentRequest.TicketRequest)request, cancellation));

				return await this.PayV3Async(request, scenario, cancellation);
			}

			public override async ValueTask<PaymentOrder> GetAsync(string voucher, CancellationToken cancellation = default)
			{
				return await base.GetAsync<DirectOrder>(voucher, $"mchid={Uri.EscapeDataString(_authority.Code)}", cancellation);
			}

			public override async ValueTask<PaymentOrder> GetCompletedAsync(string key, CancellationToken cancellation = default)
			{
				return await base.GetCompletedAsync<DirectOrder>(key, $"mchid={Uri.EscapeDataString(_authority.Code)}", cancellation);
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
					_ => throw new ArgumentOutOfRangeException(nameof(scenario)),
				};
			}

			protected override object GetCancellation() => new { mchid = _authority.Code };
			#endregion

			private sealed class DirectBuilder : RequestBuilder
			{
				private readonly IAuthority _authority;
				public DirectBuilder(IAuthority authority) => _authority = authority;

				#region 重写方法
				internal override string GetFallback() => GetFallback(_authority.Code);

				public override PaymentRequest Create(string appId, string voucher, decimal amount, string currency, string payer, string description = null)
				{
					appId = GetApplication(_authority, appId).Code;

					return new DirectRequest(voucher, amount, currency, payer, _authority.Code, appId, description)
					{
						FallbackUrl = this.GetFallback(),
					};
				}

				public override PaymentRequest.TicketRequest Ticket(string appId, string voucher, decimal amount, string currency, string ticketCode, string description = null)
				{
					appId = GetApplication(_authority, appId).Code;

					return new DirectTicketRequest(voucher, amount, currency, ticketCode, _authority.Code, appId, description)
					{
						FallbackUrl = this.GetFallback(),
					};
				}

				public override PaymentRequest.TicketRequest Ticket(string appId, string voucher, decimal amount, string currency, string ticketCode, PaymentRequest.TicketRequest.BusinessInfo business, string description = null)
				{
					appId = GetApplication(_authority, appId).Code;

					return new DirectTicketRequest(voucher, amount, currency, ticketCode, _authority.Code, appId, description)
					{
						Business = business,
						FallbackUrl = this.GetFallback(),
					};
				}
				#endregion
			}

			private sealed class DirectRequest : PaymentRequest
			{
				#region 构造函数
				internal DirectRequest(string voucher, decimal amount, string payer, string merchantId, string appId, string description = null) : this(voucher, amount, null, payer, merchantId, appId, description) { }
				internal DirectRequest(string voucher, decimal amount, string currency, string payer, string merchantId, string appId, string description = null) : base(voucher, amount, currency, description)
				{
					this.MerchantId = merchantId;
					this.AppId = appId;
					this.Payer = new PayerInfo(payer);
				}
				#endregion

				#region 公共属性
				[JsonPropertyName("mchid")]
				public string MerchantId { get; set; }

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
				internal DirectTicketRequest(string voucher, decimal amount, string ticket, string merchantId, string appId, string description = null) : this(voucher, amount, null, ticket, merchantId, appId, description) { }
				internal DirectTicketRequest(string voucher, decimal amount, string currency, string ticket, string merchantId, string appId, string description = null) : base(voucher, amount, currency, ticket, description)
				{
					this.MerchantId = merchantId;
					this.AppId = appId;
					this.Amount = new AmountInfo(amount, currency);
				}
				#endregion

				#region 公共属性
				[JsonPropertyName("mchid")]
				public string MerchantId { get; set; }

				[JsonPropertyName("appid")]
				public string AppId { get; set; }
				#endregion
			}

			public sealed class DirectOrder : PaymentOrder
			{
				#region 重写属性
				public override AuthorityToken Merchant { get => new AuthorityToken(this.MerchantId, this.AppId); }
				public override string PayerCode => this.Payer.OpenId;
				public override string PayerType => "OpenId";
				#endregion

				#region 公共属性
				[JsonPropertyName("mchid")]
				public string MerchantId { get; set; }

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

		internal partial class BrokerPaymentService : PaymentService
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
			public BrokerPaymentService(IAuthority master, IAuthority subsidiary) : base(master, subsidiary)
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
							_client = HttpClientFactory.GetHttpClient(_authority);
					}

					return _client;
				}
			}
			#endregion

			#region 重写方法
			public override async ValueTask<PaymentResult> PayAsync(PaymentRequest request, Scenario scenario, CancellationToken cancellation = default)
			{
				if(request is PaymentRequest.TicketRequest ticketRequest)
				{
					if(ticketRequest.Business.HasValue)
						return new PaymentResult(await this.PayOfflineTicketAsync(ticketRequest, cancellation));
					else
						return new PaymentResult(await this.Tickets.PayAsync((PaymentRequest.TicketRequest)request, cancellation));
				}

				return await this.PayV3Async(request, scenario, cancellation);
			}

			public override async ValueTask<PaymentOrder> GetAsync(string voucher, CancellationToken cancellation = default)
			{
				return await base.GetAsync<BrokerOrder>(voucher, $"sp_mchid={Uri.EscapeDataString(_authority.Code)}&sub_mchid={Uri.EscapeDataString(_subsidiary.Code)}", cancellation);
			}

			public override async ValueTask<PaymentOrder> GetCompletedAsync(string key, CancellationToken cancellation = default)
			{
				return await base.GetCompletedAsync<BrokerOrder>(key, $"sp_mchid={Uri.EscapeDataString(_authority.Code)}&sub_mchid={Uri.EscapeDataString(_subsidiary.Code)}", cancellation);
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
					_ => throw new ArgumentOutOfRangeException(nameof(scenario)),
				};
			}

			protected override object GetCancellation() => new { sp_mchid = _authority.Code, sub_mchid = _subsidiary.Code };
			#endregion

			private sealed class BrokerBuilder : RequestBuilder
			{
				private readonly IAuthority _master;
				private readonly IAuthority _subsidiary;

				public BrokerBuilder(IAuthority master, IAuthority subsidiary)
				{
					_master = master;
					_subsidiary = subsidiary;
				}

				#region 重写方法
				internal override string GetFallback() => GetFallback(_master.Code);

				public override PaymentRequest Create(string appId, string voucher, decimal amount, string currency, string payer, string description = null)
				{
					var application = GetApplication(_master, null);
					string subAppId = null;

					if(!string.IsNullOrEmpty(appId))
					{
						if(ContainsApplication(_master, appId))
							application = GetApplication(_master, appId);
						else
							subAppId = GetApplication(_subsidiary, appId).Code;
					}

					return new BrokerRequest(voucher, amount, currency, payer, _master.Code, application.Code, _subsidiary.Code, subAppId, description)
					{
						FallbackUrl = this.GetFallback(),
					};
				}

				public override PaymentRequest.TicketRequest Ticket(string appId, string voucher, decimal amount, string currency, string ticketCode, string description = null)
				{
					string subAppId = null;
					var application = string.IsNullOrEmpty(appId) || ContainsApplication(_master, appId) ? GetApplication(_master, appId) : GetApplication(_subsidiary, appId);

					if(!ContainsApplication(_master, application.Code))
					{
						subAppId = application.Code;
						application = GetApplication(_master, null);
					}

					return new BrokerTicketRequest(voucher, amount, currency, ticketCode, _master.Code, application.Code, _subsidiary.Code, subAppId, description)
					{
						FallbackUrl = this.GetFallback(),
					};
				}

				public override PaymentRequest.TicketRequest Ticket(string appId, string voucher, decimal amount, string currency, string ticketCode, PaymentRequest.TicketRequest.BusinessInfo business, string description = null)
				{
					string subAppId = null;
					var application = string.IsNullOrEmpty(appId) || ContainsApplication(_master, appId) ? GetApplication(_master, appId) : GetApplication(_subsidiary, appId);

					if(!ContainsApplication(_master, application.Code))
					{
						subAppId = application.Code;
						application = GetApplication(_master, null);
					}

					return new BrokerTicketRequest(voucher, amount, currency, ticketCode, _master.Code, application.Code, _subsidiary.Code, subAppId, description)
					{
						Business = business,
						FallbackUrl = this.GetFallback(),
					};
				}
				#endregion
			}

			private sealed class BrokerRequest : PaymentRequest
			{
				#region 构造函数
				internal BrokerRequest(string voucher, decimal amount, string payer, string merchantId, string appId, string subMerchantId, string subAppId = null, string description = null) : this(voucher, amount, null, payer, merchantId, appId, subMerchantId, subAppId, description) { }
				internal BrokerRequest(string voucher, decimal amount, string currency, string payer, string merchantId, string appId, string subMerchantId, string subAppId = null, string description = null) : base(voucher, amount, currency, description)
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
				public string MerchantId { get; set; }

				[JsonPropertyName("sp_appid")]
				public string AppId { get; set; }

				[JsonPropertyName("sub_mchid")]
				public string SubMerchantId { get; set; }

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
				internal BrokerTicketRequest(string voucher, decimal amount, string ticket, string merchantId, string appId, string subMerchantId, string subAppId, string description = null) : this(voucher, amount, null, ticket, merchantId, appId, subMerchantId, subAppId, description) { }
				internal BrokerTicketRequest(string voucher, decimal amount, string currency, string ticket, string merchantId, string appId, string subMerchantId, string subAppId, string description = null) : base(voucher, amount, currency, ticket, description)
				{
					this.MerchantId = merchantId;
					this.AppId = appId;
					this.SubMerchantId = subMerchantId;
					this.SubAppId = subAppId;
				}
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

			public sealed class BrokerOrder : PaymentOrder
			{
				#region 重写属性
				public override AuthorityToken Merchant { get => new AuthorityToken(this.MerchantId, this.AppId); }
				public override AuthorityToken? Subsidiary { get => new AuthorityToken(this.SubMerchantId, this.SubAppId); }
				public override string PayerCode => this.Payer.ToString();
				public override string PayerType => "OpenId";
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
