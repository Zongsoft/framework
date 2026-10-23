using System;
using System.Globalization;
using System.Collections.Generic;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public abstract partial class PaymentService
	{
		public sealed partial class TicketService
		{
			private PaymentOrder CreateOrder(IDictionary<string, string> data)
			{
				data.TryGetValue("appid", out var appId);
				data.TryGetValue("mch_id", out var merchantId);
				data.TryGetValue("openid", out var payer);

				if(_service is BrokerPaymentService)
				{
					data.TryGetValue("sub_appid", out var subAppId);
					data.TryGetValue("sub_mch_id", out var subMerchantId);
					data.TryGetValue("sub_openid", out var subPayer);
					return new BrokerPaymentService.BrokerOrder
					{
						AppId = appId,
						MerchantId = merchantId,
						SubAppId = subAppId,
						SubMerchantId = subMerchantId,
						Payer = new BrokerPaymentService.BrokerOrder.PayerInfo(payer, subPayer),
					};
				}

				return new DirectPaymentService.DirectOrder
				{
					AppId = appId,
					MerchantId = merchantId,
					Payer = new DirectPaymentService.DirectOrder.PayerInfo(payer),
				};
			}

			private PaymentOrder GetOrder(IDictionary<string, string> data, string status)
			{
				var order = this.CreateOrder(data);
				order.Status = status;
				order.Identifier = Get("transaction_id");
				order.VoucherCode = Get("out_trade_no");
				order.Kind = Get("trade_type");
				order.BankType = Get("bank_type");
				order.Extra = Get("attach");
				order.StatusDescription = Get("trade_state_desc");

				if(order.Kind != "MICROPAY" && order.Kind != "FACEPAY" || string.IsNullOrWhiteSpace(order.VoucherCode))
					throw new FormatException();

				var paid = PaymentUtility.GetStatus(status) is PaymentStatus.Succeed or PaymentStatus.Refund;
				if(!int.TryParse(Get("total_fee"), NumberStyles.None, CultureInfo.InvariantCulture, out var total))
					throw new FormatException();

				var cash = 0;
				if((paid || data.ContainsKey("cash_fee")) && !int.TryParse(Get("cash_fee"), NumberStyles.None, CultureInfo.InvariantCulture, out cash))
					throw new FormatException();

				order.Amount = new PaymentOrder.AmountInfo(total, paid ? cash : 0, Get("fee_type") ?? "CNY");
				if(data.TryGetValue("time_end", out var time))
				{
					if(!DateTime.TryParseExact(time, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
						throw new FormatException();

					order.PaidTime = timestamp;
				}

				_service.ValidateOrder(order);
				return order;

				string Get(string key) => data.TryGetValue(key, out var value) ? value : null;
			}
		}
	}
}
