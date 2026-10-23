using System;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public abstract partial class RefundmentService
	{
		#region 结果模型
		public sealed class RefundmentResult
		{
			internal RefundmentResult(RefundmentOrder order) => this.Order = order;
			public RefundmentOrder Order { get; }
		}

		public class RefundmentNotification : RefundmentOrder
		{
			[JsonPropertyName("refund_status")]
			public override string Status { get; set; }
		}
		#endregion

		#region 响应验证
		private void ValidateResponse(Response response, string voucher)
		{
			if(response == null || response.RefundVoucherCode != voucher || string.IsNullOrWhiteSpace(response.RefundId) ||
				string.IsNullOrWhiteSpace(response.PaymentSerialId) || string.IsNullOrWhiteSpace(response.PaymentVoucherCode) || string.IsNullOrWhiteSpace(response.Status) ||
				response.CreatedTime == default || response.Status == "SUCCESS" && !response.RefundedTime.HasValue ||
				response.Amount.Value <= 0 || response.Amount.PaidAmount <= 0 || response.Amount.Value > response.Amount.PaidAmount ||
				response.Amount.ActualRefund < 0 || response.Amount.ActualRefund > response.Amount.Value || response.Amount.ActualTotal < response.Amount.ActualRefund ||
				response.Amount.ActualTotal > response.Amount.PaidAmount || response.Amount.Currency != "CNY")
				throw new FormatException();

			var subsidiary = _subsidiary?.Code;
			if(response.MerchantId != null && response.MerchantId != _authority.Code || response.ServiceProviderId != null && response.ServiceProviderId != _authority.Code ||
				response.SubMerchantId != null && response.SubMerchantId != subsidiary)
				throw new FormatException();

			response.MerchantContext = new AuthorityToken(_authority.Code);
			response.SubsidiaryContext = subsidiary == null ? null : new AuthorityToken(subsidiary);
		}
		#endregion

		#region 协议模型
		private sealed class Response : RefundmentOrder
		{
			[JsonIgnore]
			public AuthorityToken MerchantContext { get; set; }
			[JsonIgnore]
			public AuthorityToken? SubsidiaryContext { get; set; }
			public override AuthorityToken Merchant => this.MerchantContext;
			public override AuthorityToken? Subsidiary => this.SubsidiaryContext;

			[JsonPropertyName("mchid")]
			public string MerchantId { get; set; }
			[JsonPropertyName("sp_mchid")]
			public string ServiceProviderId { get; set; }
			[JsonPropertyName("sub_mchid")]
			public string SubMerchantId { get; set; }
		}
		#endregion
	}
}
