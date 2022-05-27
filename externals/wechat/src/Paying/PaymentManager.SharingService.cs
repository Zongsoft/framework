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
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat.Paying
{
    public partial class PaymentManager
    {
        public abstract class SharingService
		{
			#region 私有变量
			private readonly IAuthority _authority;
			#endregion

			#region 构造函数
			protected SharingService(IAuthority authority)
			{
				_authority = authority ?? throw new ArgumentNullException(nameof(authority));
			}
			#endregion

			#region 公共属性
			public RequestBuilder Request { get; protected set; }
			public ReturnService Returing { get; protected set; }
			#endregion

			#region 保护属性
			protected abstract HttpClient Client { get; }
			#endregion

			#region 公共方法
			public ValueTask<OperationResult<SharingOrder>> GetAsync(string voucher, string tradeId, CancellationToken cancellation = default)
			{
				if(string.IsNullOrEmpty(voucher))
					throw new ArgumentNullException(nameof(voucher));
				if(string.IsNullOrEmpty(tradeId))
					throw new ArgumentNullException(nameof(tradeId));

				var url = $"profitsharing/orders/{voucher}?sub_mchid={GetSubMerchantId()}&transaction_id={tradeId}";
				return this.Client.GetAsync<SharingOrder>(url, cancellation);
			}

			public ValueTask<OperationResult<SharingOrder>> ShareAsync(SharingRequest request, CancellationToken cancellation = default)
			{
				if(request == null)
					throw new ArgumentNullException(nameof(request));

				return this.Client.PostAsync<SharingRequest, SharingOrder>("profitsharing/orders", request, cancellation);
			}

			public ValueTask<OperationResult<SharingOrder>> FinishAsync(string voucher, string tradeId, string description = null, CancellationToken cancellation = default)
			{
				if(string.IsNullOrEmpty(voucher))
					throw new ArgumentNullException(nameof(voucher));
				if(string.IsNullOrEmpty(tradeId))
					throw new ArgumentNullException(nameof(tradeId));

				var url = $"profitsharing/orders/unfreeze";
				var request = this.GetFinishRequest(voucher, tradeId, description);
				return this.Client.PostAsync<object, SharingOrder>(url, request, cancellation);
			}

			public ValueTask<OperationResult> Register(string accountType, string accountCode, string accountName, SharingParticipantRelation relation, CancellationToken cancellation = default)
			{
				return this.Register(accountType, accountCode, accountName, relation switch
				{
					SharingParticipantRelation.User => "USER",
					SharingParticipantRelation.Staff => "STAFF",
					SharingParticipantRelation.ServiceProvider => "SERVICE_PROVIDER",
					SharingParticipantRelation.Store => "STORE",
					SharingParticipantRelation.StoreOwner => "STORE_OWNER",
					SharingParticipantRelation.Partner => "PARTNER",
					SharingParticipantRelation.Headquarter => "HEADQUARTER",
					SharingParticipantRelation.Brand => "BRAND",
					SharingParticipantRelation.Distributor => "DISTRIBUTOR",
					SharingParticipantRelation.Supplier => "SUPPLIER",
					_ => "CUSTOM",
				}, "Other", cancellation);
			}

			public ValueTask<OperationResult> Register(string accountType, string accountCode, string accountName, string relation, CancellationToken cancellation = default) =>
				this.Register(accountType, accountCode, accountName, "CUSTOM", relation, cancellation);

			private async ValueTask<OperationResult> Register(string accountType, string accountCode, string accountName, string relationType, string relationName, CancellationToken cancellation = default)
			{
				if(string.IsNullOrEmpty(accountType))
					throw new ArgumentNullException(nameof(accountType));
				if(string.IsNullOrEmpty(accountCode))
					throw new ArgumentNullException(nameof(accountCode));

				var url = $"profitsharing/receivers/add";
				var request = this.GetRegisterRequest(accountType, accountCode, accountName, relationType, relationName);
				var result = await this.Client.PostAsync<object, RegisterResult>(url, request, cancellation);
				return result.Succeed ? OperationResult.Success() : result.Failure;
			}

			public async ValueTask<OperationResult> Unregister(string accountType, string accountCode, CancellationToken cancellation = default)
			{
				if(string.IsNullOrEmpty(accountType))
					throw new ArgumentNullException(nameof(accountType));
				if(string.IsNullOrEmpty(accountCode))
					throw new ArgumentNullException(nameof(accountCode));

				var url = $"profitsharing/receivers/delete";
				var request = this.GetUnregisterRequest(accountType, accountCode);
				var result = await this.Client.PostAsync<object, UnregisterResult>(url, request, cancellation);
				return result.Succeed ? OperationResult.Success() : result.Failure;
			}
			#endregion

			#region 抽象方法
			protected abstract string GetSubMerchantId();
			protected abstract object GetFinishRequest(string voucher, string tradeId, string description = null);
			protected abstract object GetRegisterRequest(string accountType, string accountCode, string accountName, string relationType, string relationName);
			protected abstract object GetUnregisterRequest(string accountType, string accountCode);
			#endregion

			#region 模型类型
			public abstract class RequestBuilder
			{
				public SharingRequest Create(string appId, string tradeId, string voucher, IEnumerable<SharingRequest.Receiver> receivers) => this.Create(appId, tradeId, voucher, false, receivers);
				public SharingRequest Create(string appId, string tradeId, string voucher, params SharingRequest.Receiver[] receivers) => this.Create(appId, tradeId, voucher, false, (IEnumerable<SharingRequest.Receiver>)receivers);
				public SharingRequest Create(string appId, string tradeId, string voucher, bool finished, params SharingRequest.Receiver[] receivers) => this.Create(appId, tradeId, voucher, finished, (IEnumerable<SharingRequest.Receiver>)receivers);
				public abstract SharingRequest Create(string appId, string tradeId, string voucher, bool finished, IEnumerable<SharingRequest.Receiver> receivers);
			}

			public abstract class SharingRequest
			{
				#region 构造函数
				protected SharingRequest(string tradeId, string voucher, bool finished, IEnumerable<Receiver> receivers)
				{
					this.TradeId = tradeId;
					this.VoucherCode = voucher;
					this.Finished = finished;
					this.Receivers = receivers ?? throw new ArgumentNullException(nameof(receivers));
				}
				#endregion

				#region 公共属性
				[JsonPropertyName("transaction_id")]
				public string TradeId { get; set; }

				[JsonPropertyName("out_order_no")]
				public string VoucherCode { get; set; }

				[JsonPropertyName("unfreeze_unsplit")]
				public bool Finished { get; set; }

				[JsonPropertyName("receivers")]
				public IEnumerable<Receiver> Receivers { get; set; }
				#endregion

				#region 嵌套结构
				public struct Receiver
				{
					#region 构造函数
					public Receiver(int amount, string accountType, string accountCode, string accountName, string description = null)
					{
						this.Amount = amount;
						this.AccountType = accountType;
						this.AccountCode = accountCode;
						this.AccountName = accountName;
						this.Description = description;
					}

					public Receiver(decimal amount, string accountType, string accountCode, string accountName, string description = null)
					{
						this.Amount = (int)(amount * 100);
						this.AccountType = accountType;
						this.AccountCode = accountCode;
						this.AccountName = accountName;
						this.Description = description;
					}
					#endregion

					#region 公共属性
					[JsonPropertyName("amount")]
					public int Amount { get; set; }

					[JsonPropertyName("type")]
					public string AccountType { get; set; }

					[JsonPropertyName("account")]
					public string AccountCode { get; set; }

					[JsonPropertyName("name")]
					public string AccountName { get; set; }

					[JsonPropertyName("description")]
					public string Description { get; set; }
					#endregion

					#region 重写方法
					public override string ToString() => string.IsNullOrEmpty(this.AccountName) ?
						$"{this.AccountType}:{this.AccountCode}={this.Amount}" :
						$"{this.AccountType}:{this.AccountCode}({this.AccountName})={this.Amount}";
					#endregion
				}
				#endregion
			}

			public abstract class SharingOrder
			{
				#region 构造函数
				protected SharingOrder() { }
				#endregion

				#region 公共属性
				public abstract AuthorityToken Merchant { get; }
				public virtual AuthorityToken? Subsidiary { get; }

				[JsonPropertyName("order_id")]
				public string Identifier { get; set; }

				[JsonPropertyName("transaction_id")]
				public string TradeId { get; set; }

				[JsonPropertyName("out_order_no")]
				public string VoucherCode { get; set; }

				[JsonPropertyName("state")]
				public string Status { get; set; }

				[JsonPropertyName("receivers")]
				public IEnumerable<Receiver> Receivers { get; set; }
				#endregion

				#region 嵌套结构
				public struct Receiver
				{
					#region 公共属性
					[JsonPropertyName("detail_id")]
					public string Identifier { get; set; }

					[JsonPropertyName("amount")]
					public int Amount { get; set; }

					[JsonPropertyName("type")]
					public string AccountType { get; set; }

					[JsonPropertyName("account")]
					public string AccountCode { get; set; }

					[JsonPropertyName("name")]
					public string AccountName { get; set; }

					[JsonPropertyName("result")]
					public string Status { get; set; }

					[JsonPropertyName("fail_reason")]
					public string Reason { get; set; }

					[JsonPropertyName("create_time")]
					public DateTime CreatedTime { get; set; }

					[JsonPropertyName("finish_time")]
					public DateTime FinishedTime { get; set; }

					[JsonPropertyName("description")]
					public string Description { get; set; }
					#endregion

					#region 重写方法
					public override string ToString() => string.IsNullOrEmpty(this.AccountName) ?
						$"{this.Identifier}#{this.Status}|{this.AccountType}:{this.AccountCode}={this.Amount}" :
						$"{this.Identifier}#{this.Status}|{this.AccountType}:{this.AccountCode}({this.AccountName})={this.Amount}";
					#endregion
				}
				#endregion
			}

			private struct RegisterResult
			{
				[JsonPropertyName("sub_mchid")]
				public string SubMerchantId { get; set; }
				[JsonPropertyName("type")]
				public string AccountType { get; set; }
				[JsonPropertyName("account")]
				public string AccountCode { get; set; }
				[JsonPropertyName("name")]
				public string AccountName { get; set; }
				[JsonPropertyName("relation_type")]
				public string RelationType { get; set; }
				[JsonPropertyName("custom_relation")]
				public string RelationName { get; set; }

				[JsonIgnore]
				public bool IsEmpty => string.IsNullOrEmpty(this.AccountType) || string.IsNullOrEmpty(this.AccountCode);
			}

			private struct UnregisterResult
			{
				[JsonPropertyName("sub_mchid")]
				public string SubMerchantId { get; set; }
				[JsonPropertyName("type")]
				public string AccountType { get; set; }
				[JsonPropertyName("account")]
				public string AccountCode { get; set; }

				[JsonIgnore]
				public bool IsEmpty => string.IsNullOrEmpty(this.AccountType) || string.IsNullOrEmpty(this.AccountCode);
			}
			#endregion

			public class ReturnService
			{
				private readonly SharingService _sharing;
				protected ReturnService(SharingService sharing) => _sharing = sharing ?? throw new ArgumentNullException(nameof(sharing));

				public ValueTask<OperationResult<ReturnResult>> ReturnAsync(ReturnRequest request, CancellationToken cancellation = default)
				{
					if(request == null)
						throw new ArgumentNullException(nameof(request));

					return _sharing.Client.PostAsync<ReturnRequest, ReturnResult>("profitsharing/return-orders", request, cancellation);
				}

				public ValueTask<OperationResult<ReturnResult>> GetAsync(string voucher, string sharingVoucherCode, CancellationToken cancellation = default)
				{
					if(string.IsNullOrEmpty(voucher))
						throw new ArgumentNullException(nameof(voucher));
					if(string.IsNullOrEmpty(sharingVoucherCode))
						throw new ArgumentNullException(nameof(sharingVoucherCode));

					var url = $"profitsharing/return-orders/{voucher}?sub_mchid={_sharing.GetSubMerchantId()}&out_order_no={sharingVoucherCode}";
					return _sharing.Client.GetAsync<ReturnResult>(url, cancellation);
				}

				public abstract class ReturnRequest
				{
					[JsonPropertyName("out_return_no")]
					public string VoucherCode { get; set; }

					[JsonPropertyName("out_order_no")]
					public string SharingVoucherCode { get; set; }

					[JsonPropertyName("return_mchid")]
					public string ReturnMerchantId { get; set; }

					[JsonPropertyName("amount")]
					public int Amount { get; set; }

					[JsonPropertyName("description")]
					public string Description { get; set; }
				}

				public struct ReturnResult
				{
					[JsonPropertyName("return_id")]
					public string Identifier { get; set; }

					[JsonPropertyName("out_return_no")]
					public string VoucherCode { get; set; }

					[JsonPropertyName("order_id")]
					public string SharingId { get; set; }

					[JsonPropertyName("out_order_no")]
					public string SharingVoucherCode { get; set; }

					[JsonPropertyName("return_mchid")]
					public string RefundMerchantId { get; set; }

					[JsonPropertyName("amount")]
					public int Amount { get; set; }

					[JsonPropertyName("result")]
					public string Status { get; set; }

					[JsonPropertyName("fail_reason")]
					public string Reason { get; set; }

					[JsonPropertyName("create_time")]
					public DateTime CreatedTime { get; set; }

					[JsonPropertyName("finish_time")]
					public DateTime FinishedTime { get; set; }

					[JsonPropertyName("description")]
					public string Description { get; set; }
				}
			}

			internal class DirectSharingService : SharingService
			{
				#region 常量定义
				/// <summary>表示直连商户数据格式的标识。</summary>
				internal const string FORMAT = "direct";
				#endregion

				#region 成员字段
				private HttpClient _client;
				#endregion

				#region 构造函数
				public DirectSharingService(IAuthority authority) : base(authority)
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
				protected override string GetSubMerchantId() => null;
				protected override object GetFinishRequest(string voucher, string tradeId, string description = null) => new FinishRequest(voucher, tradeId, description);
				protected override object GetRegisterRequest(string accountType, string accountCode, string accountName, string relationType, string relationName) => new RegisterRequest(accountType, accountCode, accountName, relationType, relationName);
				protected override object GetUnregisterRequest(string appId, string accountType, string accountCode) => new UnregisterRequest(appId, accountType, accountCode);
				#endregion

				#region 嵌套子类
				private sealed class DirectBuilder : RequestBuilder
				{
					private readonly IAuthority _authority;
					public DirectBuilder(IAuthority authority) => _authority = authority;

					public override SharingRequest Create(string appId, string tradeId, string voucher, bool finished, IEnumerable<SharingRequest.Receiver> receivers) =>
						new DirectRequest(appId, tradeId, voucher, finished, receivers);
				}

				private sealed class DirectRequest : SharingRequest
				{
					#region 构造函数
					internal DirectRequest(string appId, string tradeId, string voucher, IEnumerable<Receiver> receivers) : this(appId, tradeId, voucher, false, receivers) { }
					internal DirectRequest(string appId, string tradeId, string voucher, bool finished, IEnumerable<Receiver> receivers) : base(tradeId, voucher, finished, receivers)
					{
						this.AppId = appId;
					}
					#endregion

					#region 公共属性
					[JsonPropertyName("appid")]
					public string AppId { get; set; }
					#endregion
				}

				public sealed class DirectOrder : SharingOrder
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

				private readonly struct FinishRequest
				{
					public FinishRequest(string voucher, string tradeId, string description = null)
					{
						this.VoucherCode = voucher;
						this.TradeId = tradeId;
						this.Description = description;
					}

					[JsonPropertyName("transaction_id")]
					public string TradeId { get; }
					[JsonPropertyName("out_order_no")]
					public string VoucherCode { get; }
					[JsonPropertyName("description")]
					public string Description { get; }
				}

				private readonly struct RegisterRequest
				{
					public RegisterRequest(string appId, string accountType, string accountCode, string accountName, string relationType, string relationName = null)
					{
						this.AppId = appId;
						this.AccountType = accountType;
						this.AccountCode = accountCode;
						this.AccountName = accountName;
						this.RelationType = relationType;
						this.RelationName = relationName;
					}

					[JsonPropertyName("appid")]
					public string AppId { get; }
					[JsonPropertyName("type")]
					public string AccountType { get; }
					[JsonPropertyName("account")]
					public string AccountCode { get; }
					[JsonPropertyName("name")]
					public string AccountName { get; }
					[JsonPropertyName("relation_type")]
					public string RelationType { get; }
					[JsonPropertyName("custom_relation")]
					public string RelationName { get; }
				}

				private readonly struct UnregisterRequest
				{
					public UnregisterRequest(string appId, string accountType, string accountCode)
					{
						this.AppId = appId;
						this.AccountType = accountType;
						this.AccountCode = accountCode;
					}

					[JsonPropertyName("appid")]
					public string AppId { get; }
					[JsonPropertyName("type")]
					public string AccountType { get; }
					[JsonPropertyName("account")]
					public string AccountCode { get; }
				}
				#endregion
			}

			internal class BrokerSharingService : SharingService
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
				public BrokerSharingService(IAuthority master, IAuthority subsidiary) : base(master)
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

				private sealed class BrokerRequest : SharingRequest
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

				public sealed class BrokerOrder : SharingOrder
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

		public enum SharingParticipantRelation
		{
			/// <summary>自定义</summary>
			Custom,
			/// <summary>用户</summary>
			User,
			/// <summary>员工</summary>
			Staff,
			/// <summary>服务商</summary>
			ServiceProvider,
			/// <summary>门店</summary>
			Store,
			/// <summary>店主</summary>
			StoreOwner,
			/// <summary>合作伙伴</summary>
			Partner,
			/// <summary>总部</summary>
			Headquarter,
			/// <summary>品牌方</summary>
			Brand,
			/// <summary>分销商</summary>
			Distributor,
			/// <summary>供应商</summary>
			Supplier,
		}
	}
}
