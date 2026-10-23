using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public abstract partial class PaymentService
	{
		/// <summary>表示下单受理的凭据或已经返回的实际支付单。</summary>
		public sealed class PaymentResult
		{
			internal PaymentResult(PaymentOrder order) => this.Order = order ?? throw new ArgumentNullException(nameof(order));

			internal PaymentResult(string prepaymentId, string codeUrl, Scenario scenario, IAuthority authority, AuthorityToken application)
			{
				if(scenario == Scenario.Native)
				{
					if(string.IsNullOrWhiteSpace(codeUrl) || !string.IsNullOrEmpty(prepaymentId))
						throw new FormatException();
				}
				else if(string.IsNullOrWhiteSpace(prepaymentId) || prepaymentId.Length > 64 || prepaymentId.IndexOfAny(['\r', '\n']) >= 0 || !string.IsNullOrEmpty(codeUrl))
					throw new FormatException();

				this.PrepaymentId = prepaymentId;
				this.CodeUrl = codeUrl;
				if(scenario == Scenario.Native)
					return;

				var nonce = Guid.NewGuid().ToString("N");
				var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
				var package = scenario == Scenario.App ? prepaymentId : $"prepay_id={prepaymentId}";
				var signature = Convert.ToBase64String(authority.Signing.Sign(Encoding.UTF8.GetBytes($"{application.Applet}\n{timestamp}\n{nonce}\n{package}\n")));
				var parameters = new Dictionary<string, string>
				{
					["appId"] = application.Applet,
					["timeStamp"] = timestamp,
					["nonceStr"] = nonce,
				};

				if(scenario == Scenario.App)
				{
					parameters.Add("partnerId", application.Identifier);
					parameters.Add("prepayId", prepaymentId);
					parameters.Add("packageValue", "Sign=WXPay");
					parameters.Add("sign", signature);
				}
				else
				{
					parameters.Add("package", package);
					parameters.Add("signType", "RSA");
					parameters.Add("paySign", signature);
				}

				this.Parameters = new ReadOnlyDictionary<string, string>(parameters);
			}

			/// <summary>获取实际支付单；下单受理时为空，订单状态仍须单独判断。</summary>
			public PaymentOrder Order { get; }
			/// <summary>获取 JSAPI 或 APP 预支付凭据；它不是实际交易流水号。</summary>
			public string PrepaymentId { get; }
			/// <summary>获取 Native 支付二维码内容。</summary>
			public string CodeUrl { get; }
			/// <summary>获取与本次下单身份和场景匹配的客户端调起参数；Native 和实际订单为空。</summary>
			public IReadOnlyDictionary<string, string> Parameters { get; }
		}
	}
}
