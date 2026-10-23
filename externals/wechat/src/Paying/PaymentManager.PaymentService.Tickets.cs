using System;
using System.Net;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public abstract partial class PaymentService
	{
		/// <summary>付款码与在线刷脸支付的 V2 查单和撤销服务。</summary>
		public sealed partial class TicketService
		{
			#region 成员字段
			private readonly PaymentService _service;
			private readonly IAuthority _subsidiary;
			#endregion

			#region 构造函数
			internal TicketService(PaymentService service, IAuthority subsidiary)
			{
				_service = service;
				_subsidiary = subsidiary;
			}
			#endregion

			#region 公共方法
			public ValueTask<PaymentOrder> GetAsync(string appId, string voucher, CancellationToken cancellation = default) => this.QueryAsync(appId, "out_trade_no", voucher, cancellation);
			public ValueTask<PaymentOrder> GetCompletedAsync(string appId, string identifier, CancellationToken cancellation = default) => this.QueryAsync(appId, "transaction_id", identifier, cancellation);

			public async ValueTask<ReversalResult> ReverseAsync(string appId, string voucher, CancellationToken cancellation = default)
			{
				ValidateVoucher(voucher);
				var request = this.GetIdentity(appId);
				request["out_trade_no"] = voucher;
				var result = await HttpClientFactory.Xml.SendAsync(_service.Authority, "https://api.mch.weixin.qq.com/secapi/pay/reverse", request, true, cancellation);
				ValidateIdentity(result, request);

				if(!result.TryGetValue("result_code", out var status))
					throw new FormatException();
				if(status == "SUCCESS")
					return new ReversalResult(true, false, null);
				if(status != "FAIL" || !result.TryGetValue("recall", out var recall) || recall != "Y" && recall != "N")
					throw new FormatException();

				return new ReversalResult(false, recall == "Y", result.TryGetValue("err_code", out var error) ? error : null);
			}
			#endregion

			#region 内部方法
			internal async ValueTask<PaymentOrder> PayAsync(PaymentRequest.TicketRequest request, CancellationToken cancellation)
			{
				ArgumentNullException.ThrowIfNull(request);
				ValidateVoucher(request.VoucherCode);
				if(request.Business.HasValue || string.IsNullOrWhiteSpace(request.TicketCode) || string.IsNullOrWhiteSpace(request.Description) ||
					!IPAddress.TryParse(request.Place.DeviceIp, out _))
					throw new ArgumentException(null, nameof(request));

				Money.Validate(request.Amount.Value, nameof(request));
				Money.Currency(request.Amount.Currency);
				var data = request switch
				{
					DirectPaymentService.DirectTicketRequest direct when _service is DirectPaymentService && direct.MerchantId == _service.Authority.Code => this.GetIdentity(direct.AppId),
					BrokerPaymentService.BrokerTicketRequest broker when _service is BrokerPaymentService service && broker.MerchantId == service.Authority.Code && broker.SubMerchantId == _subsidiary.Code => this.GetIdentity(broker.SubAppId ?? broker.AppId),
					_ => throw new ArgumentException(null, nameof(request)),
				};

				if(request is BrokerPaymentService.BrokerTicketRequest identity &&
					((string)data["appid"] != identity.AppId || (data.TryGetValue("sub_appid", out var subAppId) ? subAppId as string : null) != identity.SubAppId))
					throw new ArgumentException(null, nameof(request));

				data["out_trade_no"] = request.VoucherCode;
				data["auth_code"] = request.TicketCode;
				data["body"] = request.Description;
				data["total_fee"] = request.Amount.Value;
				data["fee_type"] = request.Amount.Currency;
				data["spbill_create_ip"] = request.Place.DeviceIp;
				data["device_info"] = request.Place.DeviceId;
				data["attach"] = request.Extra;
				data["profit_sharing"] = request.Settlement.ProfitSharingEnabled ? "Y" : "N";
				var result = await HttpClientFactory.Xml.SendAsync(_service.Authority, "https://api.mch.weixin.qq.com/pay/micropay", data, false, cancellation);
				ValidateIdentity(result, data);

				if(!result.TryGetValue("result_code", out var status))
					throw new FormatException();

				if(status == "SUCCESS")
				{
					var order = this.GetOrder(result, "SUCCESS");
					if(order.VoucherCode != request.VoucherCode || order.Amount.Value != request.Amount.Value)
						throw new FormatException();

					return order;
				}

				if(status != "FAIL" || !result.TryGetValue("err_code", out var error) || string.IsNullOrWhiteSpace(error))
					throw new FormatException();

				//只有明确的拒付状态才是失败；系统错误、银行超时和新错误码均须查原单。
				var state = error switch
				{
					"USERPAYING" => "USERPAYING",
					"ORDERREVERSED" => "REVOKED",
					"ORDERCLOSED" => "CLOSED",
					"AUTHCODEEXPIRE" or "AUTH_CODE_INVALID" or "NOTENOUGH" or "NOTSUPORTCARD" => "PAYERROR",
					_ => "UNKNOWN",
				};

				result["out_trade_no"] = request.VoucherCode;
				var pending = this.CreateOrder(result);
				pending.VoucherCode = request.VoucherCode;
				pending.Status = state;
				pending.StatusDescription = error;
				pending.Amount = new PaymentOrder.AmountInfo(request.Amount.Value, 0, request.Amount.Currency);
				return pending;
			}

			private async ValueTask<PaymentOrder> QueryAsync(string appId, string field, string value, CancellationToken cancellation)
			{
				ValidateVoucher(value);
				var request = this.GetIdentity(appId);
				request[field] = value;
				var result = await HttpClientFactory.Xml.SendAsync(_service.Authority, "https://api.mch.weixin.qq.com/pay/orderquery", request, false, cancellation);
				ValidateIdentity(result, request);

				if(!result.TryGetValue("result_code", out var status) || status != "SUCCESS")
					throw new Zongsoft.Common.OperationException(result.TryGetValue("err_code", out var error) ? error : "V2_QUERY_FAILED", null);
				if(!result.TryGetValue(field, out var actual) || actual != value || !result.TryGetValue("trade_state", out var state) || string.IsNullOrWhiteSpace(state))
					throw new FormatException();

				return this.GetOrder(result, state);
			}

			internal IDictionary<string, object> GetIdentity(string appId)
			{
				var request = new Dictionary<string, object>(StringComparer.Ordinal)
				{
					["mch_id"] = _service.Authority.Code,
					["sign_type"] = "MD5",
					["nonce_str"] = Guid.NewGuid().ToString("N"),
				};

				if(_service is BrokerPaymentService broker)
				{
					request["sub_mch_id"] = _subsidiary.Code;
					if(!string.IsNullOrEmpty(appId) && !ContainsApplication(_service.Authority, appId))
					{
						request["sub_appid"] = GetApplication(_subsidiary, appId).Code;
						appId = null;
					}
				}

				request["appid"] = GetApplication(_service.Authority, appId).Code;
				return request;
			}

			internal static void ValidateIdentity(IDictionary<string, string> result, IDictionary<string, object> request, bool optionalMerchant = false)
			{
				foreach(var field in new[] { "appid", "mch_id", "sub_appid", "sub_mch_id" })
				{
					request.TryGetValue(field, out var expected);
					result.TryGetValue(field, out var actual);
					if(optionalMerchant && field == "mch_id" && string.IsNullOrEmpty(actual))
						continue;
					if(!string.Equals(actual ?? string.Empty, expected as string ?? string.Empty, StringComparison.Ordinal))
						throw new FormatException();
				}
			}

			private static void ValidateVoucher(string value)
			{
				if(string.IsNullOrWhiteSpace(value) || value.Length > 32)
					throw new ArgumentException(null, nameof(value));
			}
			#endregion

			#region 嵌套结构
			public readonly struct ReversalResult
			{
				internal ReversalResult(bool succeeded, bool requiresRetry, string errorCode)
				{
					this.Succeeded = succeeded;
					this.RequiresRetry = requiresRetry;
					this.ErrorCode = errorCode;
				}

				public bool Succeeded { get; }
				public bool RequiresRetry { get; }
				public string ErrorCode { get; }
			}
			#endregion
		}
	}
}
