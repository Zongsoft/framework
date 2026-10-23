using System;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public abstract partial class PaymentService
	{
		protected virtual void ValidateOrder(PaymentOrder order)
		{
			if(order == null || order.Merchant.Identifier != _authority.Code || !ContainsApplication(_authority, order.Merchant.Applet) ||
				order.Amount.Value <= 0 || order.Amount.PaidValue < 0 || order.Amount.PaidValue > order.Amount.Value ||
				!string.Equals(order.Amount.Currency, "CNY", StringComparison.Ordinal))
				throw new FormatException();

			var status = PaymentUtility.GetStatus(order.Status);
			if((status == PaymentStatus.Succeed || status == PaymentStatus.Refund) && string.IsNullOrWhiteSpace(order.Identifier))
				throw new FormatException();
		}

		internal partial class BrokerPaymentService
		{
			protected override void ValidateOrder(PaymentOrder order)
			{
				base.ValidateOrder(order);
				if(order.Subsidiary?.Identifier != _subsidiary.Code ||
					!string.IsNullOrEmpty(order.Subsidiary?.Applet) && !ContainsApplication(_subsidiary, order.Subsidiary.Value.Applet))
					throw new FormatException();
			}
		}
	}
}
