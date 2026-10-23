using System;
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public abstract partial class PaymentService
	{
		protected virtual IDictionary<string, object> GetPaymentRequest(PaymentRequest request, Scenario scenario, out AuthorityToken application)
		{
			ArgumentNullException.ThrowIfNull(request);
			if(request is PaymentRequest.TicketRequest)
				throw new ArgumentException(null, nameof(request));
			if(scenario != Scenario.Web && scenario != Scenario.App && scenario != Scenario.Native)
				throw new ArgumentOutOfRangeException(nameof(scenario));
			if(string.IsNullOrWhiteSpace(request.VoucherCode) || request.VoucherCode.Length > 32 || string.IsNullOrWhiteSpace(request.Description))
				throw new ArgumentException(null, nameof(request));
			if(!Uri.TryCreate(request.FallbackUrl, UriKind.Absolute, out var fallback) ||
				fallback.Scheme != Uri.UriSchemeHttps && fallback.Scheme != Uri.UriSchemeHttp || fallback.UserInfo.Length != 0 || fallback.Fragment.Length != 0)
				throw new ArgumentException(null, nameof(request));

			Money.Validate(request.Amount.Value, nameof(request));
			Money.Currency(request.Amount.Currency);
			application = default;
			var data = new Dictionary<string, object>
			{
				["out_trade_no"] = request.VoucherCode,
				["description"] = request.Description,
				["notify_url"] = request.FallbackUrl,
				["amount"] = request.Amount,
				["settle_info"] = request.Settlement,
			};

			if(request.Expiration.HasValue)
				data.Add("time_expire", request.Expiration.Value);
			if(request.Extra != null)
				data.Add("attach", request.Extra);
			if(request.Order.HasValue)
				data.Add("detail", request.Order.Value);

			if(request.Place.DeviceIp != null || request.Place.DeviceId != null || request.Place.Store.HasValue)
			{
				if(!System.Net.IPAddress.TryParse(request.Place.DeviceIp, out _))
					throw new ArgumentException(null, nameof(request));

				data.Add("scene_info", request.Place);
			}

			return data;
		}

		private static bool ContainsApplication(IAuthority authority, string code) => !string.IsNullOrEmpty(code) &&
			(authority.Accounts.Default.Code == code || authority.Accounts.Any(account => account.Code == code));

		private static Account GetApplication(IAuthority authority, string code, Scenario? scenario = null)
		{
			Account account;
			if(string.IsNullOrEmpty(code))
			{
				account = authority.Accounts.Default;
				if(!Matches(account))
				{
					var candidates = authority.Accounts.Where(Matches).Take(2).ToArray();
					account = candidates.Length == 1 ? candidates[0] : default;
				}
			}
			else
				account = authority.Accounts.Default.Code == code ? authority.Accounts.Default : authority.Accounts.Get(code);

			if(!Matches(account))
				throw new ArgumentException(null, nameof(code));

			return account;

			bool Matches(Account candidate) => !candidate.IsEmpty && (scenario switch
			{
				Scenario.App => candidate.Type == AccountType.Application,
				Scenario.Web => candidate.Type == AccountType.Applet || candidate.Type == AccountType.Channel,
				_ => true,
			});
		}

		internal partial class DirectPaymentService
		{
			protected override IDictionary<string, object> GetPaymentRequest(PaymentRequest request, Scenario scenario, out AuthorityToken application)
			{
				var data = base.GetPaymentRequest(request, scenario, out application);
				if(request is not DirectRequest direct || direct.MerchantId != _authority.Code)
					throw new ArgumentException(null, nameof(request));

				var account = GetApplication(_authority, direct.AppId, scenario);
				application = new(_authority.Code, account.Code);
				data.Add("mchid", _authority.Code);
				data.Add("appid", account.Code);

				if(scenario == Scenario.Web)
				{
					ArgumentException.ThrowIfNullOrWhiteSpace(direct.Payer.OpenId);
					data.Add("payer", direct.Payer);
				}

				return data;
			}
		}

		internal partial class BrokerPaymentService
		{
			protected override IDictionary<string, object> GetPaymentRequest(PaymentRequest request, Scenario scenario, out AuthorityToken application)
			{
				var data = base.GetPaymentRequest(request, scenario, out application);
				if(request is not BrokerRequest broker || broker.MerchantId != _authority.Code || broker.SubMerchantId != _subsidiary.Code)
					throw new ArgumentException(null, nameof(request));

				var subsidiary = !string.IsNullOrEmpty(broker.SubAppId);
				var account = GetApplication(_authority, subsidiary ? null : broker.AppId, scenario);
				application = new(_authority.Code, account.Code);
				data.Add("sp_mchid", _authority.Code);
				data.Add("sp_appid", account.Code);
				data.Add("sub_mchid", _subsidiary.Code);

				if(subsidiary)
				{
					var subAccount = GetApplication(_subsidiary, broker.SubAppId, scenario);
					application = new(_subsidiary.Code, subAccount.Code);
					data.Add("sub_appid", subAccount.Code);
				}

				if(scenario == Scenario.Web)
				{
					ArgumentException.ThrowIfNullOrWhiteSpace(subsidiary ? broker.Payer.SubOpenId : broker.Payer.OpenId);
					data.Add("payer", broker.Payer);
				}

				return data;
			}
		}
	}
}
