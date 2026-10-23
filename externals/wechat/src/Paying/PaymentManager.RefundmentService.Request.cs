using System;
using System.Text;
using System.Collections.Generic;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public abstract partial class RefundmentService
	{
		protected virtual IDictionary<string, object> GetRefundmentRequest(RefundmentRequest request)
		{
			ArgumentNullException.ThrowIfNull(request);
			ValidateIdentifier(request.PaymentKey, 32);
			ValidateIdentifier(request.VoucherCode, 64);
			Money.Validate(request.Amount.Value, nameof(request));
			Money.Validate(request.Amount.PaidAmount, nameof(request));
			Money.Currency(request.Amount.Currency);

			if(request.Amount.Value > request.Amount.PaidAmount || request.Description != null && Encoding.UTF8.GetByteCount(request.Description) > 80 ||
				!Uri.TryCreate(request.FallbackUrl, UriKind.Absolute, out var fallback) || request.FallbackUrl.Length > 256 ||
				fallback.Scheme != Uri.UriSchemeHttps && fallback.Scheme != Uri.UriSchemeHttp ||
				fallback.Query.Length != 0 || fallback.Fragment.Length != 0 || fallback.UserInfo.Length != 0)
				throw new ArgumentException(null, nameof(request));

			var data = new Dictionary<string, object>
			{
				["out_trade_no"] = request.PaymentKey,
				["out_refund_no"] = request.VoucherCode,
				["notify_url"] = request.FallbackUrl,
				["amount"] = request.Amount,
			};

			if(!string.IsNullOrEmpty(request.Description))
				data.Add("reason", request.Description);

			if(request.Details != null && request.Details.Length > 0)
			{
				foreach(var detail in request.Details)
				{
					ValidateIdentifier(detail.ItemCode, 32);
					if(detail.Quantity <= 0 || detail.UnitPrice <= 0 || detail.Amount <= 0 || detail.Amount > request.Amount.Value ||
						detail.Amount > (long)detail.UnitPrice * detail.Quantity || detail.ItemName != null && Encoding.UTF8.GetByteCount(detail.ItemName) > 256)
						throw new ArgumentException(null, nameof(request));
				}

				data.Add("goods_detail", request.Details);
			}

			return data;
		}

		private static void ValidateIdentifier(string value, int length)
		{
			if(string.IsNullOrWhiteSpace(value) || Encoding.UTF8.GetByteCount(value) > length)
				throw new ArgumentException(null, nameof(value));

			foreach(var character in value)
			{
				if(char.IsControl(character))
					throw new ArgumentException(null, nameof(value));
			}
		}

		internal partial class DirectRefundmentService
		{
			protected override IDictionary<string, object> GetRefundmentRequest(RefundmentRequest request)
			{
				if(request is not DirectRequest direct || direct.MerchantId != _authority.Code)
					throw new ArgumentException(null, nameof(request));

				return base.GetRefundmentRequest(request);
			}
		}

		internal partial class BrokerRefundmentService
		{
			protected override IDictionary<string, object> GetRefundmentRequest(RefundmentRequest request)
			{
				if(request is not BrokerRequest broker || broker.MerchantId != _subsidiary.Code || broker.MasterId != _authority.Code)
					throw new ArgumentException(null, nameof(request));

				var data = base.GetRefundmentRequest(request);
				data.Add("sub_mchid", _subsidiary.Code);
				return data;
			}
		}
	}
}
