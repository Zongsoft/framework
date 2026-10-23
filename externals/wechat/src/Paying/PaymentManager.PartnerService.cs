using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public partial class PartnerService
	{
		internal PartnerService(IAuthority authority) => this.Authority = authority;
		public IAuthority Authority { get; }

		/// <summary>提交普通服务商特约商户申请。</summary>
		/// <param name="request">申请资料。</param>
		/// <param name="cancellation">取消标记。</param>
		/// <returns>申请单号，不代表商户已经开通。</returns>
		public async ValueTask<string> RegisterAsync(Registration request, CancellationToken cancellation = default)
		{
			ArgumentNullException.ThrowIfNull(request);
			request.Validate();
			ValidateRequest(request);
			var key = await this.Authority.GetCertificateAsync(cancellation);
			var result = await HttpClientFactory.GetHttpClient(this.Authority).PostAsync<Registration, Applyment>("applyment4sub/applyment/", request, key, cancellation);

			if(result == null || result.ApplymentId <= 0)
				throw new FormatException();

			return result.ApplymentId.ToString(System.Globalization.CultureInfo.InvariantCulture);
		}

		public async ValueTask<Applyment> GetByRequestAsync(string businessCode, CancellationToken cancellation = default)
		{
			ValidateCode(businessCode);
			var result = await HttpClientFactory.GetHttpClient(this.Authority).GetAsync<Applyment>($"applyment4sub/applyment/business_code/{Uri.EscapeDataString(businessCode)}", cancellation);
			ValidateResult(result);

			if(result.BusinessCode != businessCode)
				throw new FormatException();

			return result;
		}

		public async ValueTask<Applyment> GetByApplicationAsync(string applicationId, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(applicationId) || applicationId.Any(character => !char.IsAsciiDigit(character)) || !long.TryParse(applicationId, out var id) || id <= 0)
				throw new ArgumentException(null, nameof(applicationId));

			var result = await HttpClientFactory.GetHttpClient(this.Authority).GetAsync<Applyment>($"applyment4sub/applyment/applyment_id/{applicationId}", cancellation);
			ValidateResult(result);

			if(result.ApplymentId != id)
				throw new FormatException();

			return result;
		}

		private static void ValidateCode(string code)
		{
			if(string.IsNullOrWhiteSpace(code) || code.Length > 124 || code.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
				throw new ArgumentException(null, nameof(code));
		}

		private static void ValidateResult(Applyment result)
		{
			if(result == null || result.ApplymentId <= 0 || string.IsNullOrWhiteSpace(result.BusinessCode) || string.IsNullOrWhiteSpace(result.State) || result.IsCompleted && string.IsNullOrWhiteSpace(result.MerchantId))
				throw new FormatException();
		}

		private static void ValidateRequest(Registration request)
		{
			ValidateCode(request.BusinessCode);
			var subject = request.SubjectInfo;

			if(subject.SubjectType is not ("SUBJECT_TYPE_INDIVIDUAL" or "SUBJECT_TYPE_ENTERPRISE" or "SUBJECT_TYPE_GOVERNMENT" or "SUBJECT_TYPE_INSTITUTIONS" or "SUBJECT_TYPE_OTHERS"))
				throw new ArgumentException(null, nameof(request));
			if(subject.SubjectType is "SUBJECT_TYPE_INDIVIDUAL" or "SUBJECT_TYPE_ENTERPRISE" ? subject.BusinessLicenseInfo == null : subject.CertificateInfo == null)
				throw new ArgumentException(null, nameof(request));
			if(subject.FinanceInstitution == true && subject.FinanceInstitutionInfo == null || subject.IdentityInfo.IdCardInfo == null && subject.IdentityInfo.IdDocInfo == null)
				throw new ArgumentException(null, nameof(request));
			if(request.ContactInfo.ContactType is not ("LEGAL" or "SUPER") || request.BankAccountInfo.BankAccountType is not ("BANK_ACCOUNT_TYPE_CORPORATE" or "BANK_ACCOUNT_TYPE_PERSONAL"))
				throw new ArgumentException(null, nameof(request));

			var sales = request.BusinessInfo.SalesInfo;

			foreach(var scene in sales.SalesScenesType)
			{
				var valid = scene switch
				{
					"SALES_SCENES_STORE" => sales.BizStoreInfo != null,
					"SALES_SCENES_MP" => sales.MpInfo != null && App(sales.MpInfo.MpAppid, sales.MpInfo.MpSubAppid),
					"SALES_SCENES_MINI_PROGRAM" => sales.MiniProgramInfo != null && App(sales.MiniProgramInfo.MiniProgramAppid, sales.MiniProgramInfo.MiniProgramSubAppid),
					"SALES_SCENES_APP" => sales.AppInfo != null && App(sales.AppInfo.AppAppid, sales.AppInfo.AppSubAppid),
					"SALES_SCENES_WEB" => sales.WebInfo != null,
					"SALES_SCENES_WEWORK" => sales.WeworkInfo != null,
					_ => false,
				};

				if(!valid)
					throw new ArgumentException(null, nameof(request));
			}

			static bool App(string master, string subsidiary) => !string.IsNullOrWhiteSpace(master) || !string.IsNullOrWhiteSpace(subsidiary);
		}
	}
}
