using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Externals.Wechat;

public partial class Membership
{
	public class TemplateService
	{
		#region 成员字段
		private readonly Membership _membership;
		#endregion

		#region 构造函数
		internal TemplateService(Membership membership) => _membership = membership;
		#endregion

		#region 公共方法
		public async ValueTask<CardTemplateResponse> GetAsync(string id, CancellationToken cancellation = default)
		{
			ValidateText(id, 32, nameof(id));
			var result = await Paying.HttpUtility.GetAsync<CardTemplateResponse>(Paying.HttpClientFactory.GetHttpClient(_membership.Authority),
				$"marketing/membercard-open/cards/{Uri.EscapeDataString(id)}", cancellation);
			this.ValidateResult(result, id);
			return result;
		}

		public async ValueTask<CardTemplateResponse> CreateAsync(CardTemplate template, CancellationToken cancellation = default)
		{
			ArgumentNullException.ThrowIfNull(template);
			if(template.AppId != _membership.Account.Code || template.Brand.BrandId != _membership.BrandId)
				throw new ArgumentException(null, nameof(template));

			ValidateText(template.RequestNo, 128, nameof(template));
			ValidateText(template.Title, 10, nameof(template));
			ValidateText(template.Description, 500, nameof(template));
			ValidateText(template.Brand.Title, 10, nameof(template));
			ValidateText(template.LogoUrl, 128, nameof(template));
			ValidateText(template.BackgroundUrl, 128, nameof(template));

			if(!Enum.IsDefined(template.CodeMode) || template.CodeType is not ("QRCODE" or "BAR_CODE" or "BAR_CODE_AND_QRCODE" or "NONE_CODE") ||
				template.Quantity is < 0 || template.CodeMode != CardMode.Auto && template.Quantity.HasValue)
				throw new ArgumentException(null, nameof(template));

			var validity = template.Validation;
			if(validity.Type switch
			{
				"FIX_TIME_RANGE" => !validity.StartTime.HasValue || !validity.FinalTime.HasValue || validity.FinalTime <= validity.StartTime || validity.AvailableDays.HasValue || validity.WaitDays.HasValue,
				"FIX_TERM" => validity.AvailableDays is null or < 0 or > 10957 || validity.WaitDays is < 0 || validity.StartTime.HasValue || validity.FinalTime.HasValue,
				"PERMANENT" => validity.StartTime.HasValue || validity.FinalTime.HasValue || validity.AvailableDays.HasValue || validity.WaitDays.HasValue,
				_ => true,
			})
				throw new ArgumentException(null, nameof(template));

			var result = await Paying.HttpUtility.PostAsync<CardTemplate, CardTemplateResponse>(Paying.HttpClientFactory.GetHttpClient(_membership.Authority),
				"marketing/membercard-open/cards", template, cancellation);
			this.ValidateResult(result);
			return result;
		}

		public async ValueTask DeleteAsync(string id, CancellationToken cancellation = default)
		{
			await this.GetAsync(id, cancellation);
			using var request = new HttpRequestMessage(HttpMethod.Delete, $"marketing/membercard-open/cards/{Uri.EscapeDataString(id)}");
			using var response = await Paying.HttpClientFactory.GetHttpClient(_membership.Authority).SendAsync(request, cancellation);
			await Paying.HttpUtility.EnsureResultAsync(response, cancellation);
		}
		#endregion

		#region 私有方法
		private void ValidateResult(CardTemplateResponse result, string id = null)
		{
			if(result == null || string.IsNullOrWhiteSpace(result.TemplateId) || id != null && result.TemplateId != id ||
				result.AppId != _membership.Account.Code || result.Brand.BrandId != _membership.BrandId)
				throw new FormatException();
		}
		#endregion
	}
}
