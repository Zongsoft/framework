using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public abstract partial class PaymentService
	{
		#region 公共方法
		public virtual ValueTask<OfflineOrder> GetOfflineAsync(string voucher, PaymentRequest.TicketRequest.BusinessProduct product, CancellationToken cancellation = default) => throw new NotSupportedException();
		#endregion

		#region 离线支付
		internal partial class BrokerPaymentService
		{
			public override async ValueTask<OfflineOrder> GetOfflineAsync(string voucher, PaymentRequest.TicketRequest.BusinessProduct product, CancellationToken cancellation = default)
			{
				ValidateText(voucher, 32, nameof(voucher));
				if(!Enum.IsDefined(product))
					throw new ArgumentOutOfRangeException(nameof(product));

				var order = await this.Client.GetAsync<OfflineOrder>($"offlineface/transactions/out-trade-no/{Uri.EscapeDataString(voucher)}?sp_mchid={Uri.EscapeDataString(_authority.Code)}&sub_mchid={Uri.EscapeDataString(_subsidiary.Code)}&business_product_id={(int)product}", cancellation);
				this.ValidateOffline(order, voucher);
				return order;
			}

			protected override IDictionary<string, object> GetAuthenticationOfflineRequest(string appId, string data, string device, string organization)
			{
				ValidateText(data, 2048, nameof(data));
				ValidateText(device, 32, nameof(device));
				ValidateText(organization, 64, nameof(organization));

				if(device.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
					throw new ArgumentException(null, nameof(device));

				var application = GetApplication(_authority, appId);
				var request = new Dictionary<string, object>
				{
					["sp_appid"] = application.Code,
					["sub_mchid"] = _subsidiary.Code,
					["device_id"] = device,
					["organization_id"] = organization,
					["raw_data"] = data,
				};

				if(!_subsidiary.Accounts.Default.IsEmpty)
					request.Add("sub_appid", _subsidiary.Accounts.Default.Code);

				return request;
			}

			private async ValueTask<PaymentOrder> PayOfflineTicketAsync(PaymentRequest.TicketRequest request, CancellationToken cancellation)
			{
				if(request is not BrokerTicketRequest ticket || ticket.MerchantId != _authority.Code || ticket.SubMerchantId != _subsidiary.Code ||
					!ContainsApplication(_authority, ticket.AppId) || !string.IsNullOrEmpty(ticket.SubAppId) && !ContainsApplication(_subsidiary, ticket.SubAppId) || !ticket.Business.HasValue)
					throw new ArgumentException(null, nameof(request));

				var business = ticket.Business.Value;
				if(business.Product switch
				{
					PaymentRequest.TicketRequest.BusinessProduct.K12 => business.SceneId is < 3 or > 6,
					PaymentRequest.TicketRequest.BusinessProduct.Groupon => business.SceneId is < 124 or > 126,
					_ => true,
				})
					throw new ArgumentException(null, nameof(request));

				ValidateText(ticket.VoucherCode, 32, nameof(request));
				ValidateText(ticket.Description, 128, nameof(request));
				ValidateText(ticket.Extra, 128, nameof(request));

				if(string.IsNullOrWhiteSpace(ticket.TicketCode) || !System.Net.IPAddress.TryParse(ticket.Place.DeviceIp, out _))
					throw new ArgumentException(null, nameof(request));

				Money.Validate(ticket.Amount.Value, nameof(request));
				Money.Currency(ticket.Amount.Currency);
				if(ticket.Amount.Value > 1_000_000_000)
					throw new ArgumentOutOfRangeException(nameof(request));

				var body = new Dictionary<string, object>
				{
					["sp_mchid"] = ticket.MerchantId,
					["sp_appid"] = ticket.AppId,
					["sub_mchid"] = ticket.SubMerchantId,
					["out_trade_no"] = ticket.VoucherCode,
					["auth_code"] = ticket.TicketCode,
					["amount"] = ticket.Amount,
					["description"] = ticket.Description,
					["attach"] = ticket.Extra,
					["scene_info"] = new { device_ip = ticket.Place.DeviceIp },
					["settle_info"] = ticket.Settlement,
					["business"] = business,
				};

				if(!string.IsNullOrEmpty(ticket.SubAppId))
					body.Add("sub_appid", ticket.SubAppId);

				var order = await this.Client.PostAsync<IDictionary<string, object>, OfflineOrder>("offlineface/transactions", body, cancellation);
				this.ValidateOffline(order, ticket.VoucherCode);
				if(order.Merchant.Applet != ticket.AppId || order.Subsidiary?.Applet != ticket.SubAppId || order.PaymentAmount.HasValue && order.Amount.Value != ticket.Amount.Value)
					throw new FormatException();

				return order;
			}

			private void ValidateOffline(OfflineOrder order, string voucher)
			{
				if(order == null || order.Merchant.Identifier != _authority.Code || order.Subsidiary?.Identifier != _subsidiary.Code ||
					!ContainsApplication(_authority, order.Merchant.Applet) || !string.IsNullOrEmpty(order.Subsidiary?.Applet) && !ContainsApplication(_subsidiary, order.Subsidiary.Value.Applet) ||
					order.VoucherCode != voucher || string.IsNullOrWhiteSpace(order.Status))
					throw new FormatException();

				if(order.PaymentAmount.HasValue && (order.Amount.Value <= 0 || order.Amount.PaidValue < 0 || order.Amount.PaidValue > order.Amount.Value ||
					order.Amount.Currency != "CNY" || order.PaymentAmount.Value.PaidCurrency != "CNY"))
					throw new FormatException();

				if(PaymentUtility.GetStatus(order.Status) is PaymentStatus.Succeed or PaymentStatus.Refund &&
					(!order.PaymentAmount.HasValue || string.IsNullOrWhiteSpace(order.Identifier)))
					throw new FormatException();
			}

			private static void ValidateText(string value, int length, string parameter)
			{
				if(string.IsNullOrWhiteSpace(value) || Encoding.UTF8.GetByteCount(value) > length || value.Any(char.IsControl))
					throw new ArgumentException(null, parameter);
			}
		}
		#endregion

		#region 订单模型
		public sealed class OfflineOrder : PaymentOrder
		{
			public override AuthorityToken Merchant => new(this.MerchantId, this.AppId);
			public override AuthorityToken? Subsidiary => new AuthorityToken(this.SubMerchantId, this.SubAppId);
			public override string PayerType => "OpenId";
			public override string PayerCode => this.Payer.SubOpenId ?? this.Payer.OpenId;
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
			[JsonPropertyName("trade_state_description")]
			public override string StatusDescription { get; set; }
			[JsonPropertyName("debt_state")]
			public string DebtState { get; set; }
			[JsonPropertyName("repayment_transaction_id")]
			public string RepaymentId { get; set; }
			[JsonPropertyName("error_type")]
			public string ErrorType { get; set; }
			[JsonPropertyName("amount")]
			public OfflineAmountInfo? PaymentAmount { get; set; }
			[JsonIgnore]
			public override AmountInfo Amount
			{
				get => this.PaymentAmount.HasValue ? new(this.PaymentAmount.Value.Total, this.PaymentAmount.Value.Paid, this.PaymentAmount.Value.Currency) : default;
				set => this.PaymentAmount = new() { Total = value.Value, Paid = value.PaidValue, Currency = value.Currency, PaidCurrency = value.Currency };
			}

			public struct OfflineAmountInfo
			{
				[JsonPropertyName("total")]
				public int Total { get; set; }
				[JsonPropertyName("pay_total")]
				public int Paid { get; set; }
				[JsonPropertyName("currency")]
				public string Currency { get; set; }
				[JsonPropertyName("pay_currency")]
				public string PaidCurrency { get; set; }
			}

			public struct PayerInfo
			{
				[JsonPropertyName("sp_openid")]
				public string OpenId { get; set; }
				[JsonPropertyName("sub_openid")]
				public string SubOpenId { get; set; }
			}
		}
		#endregion
	}
}
