using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public partial class PartnerService
	{
		#region 嵌套类型
		public sealed class Applyment
		{
			[JsonPropertyName("applyment_id")]
			public long ApplymentId { get; set; }
			[JsonPropertyName("business_code")]
			public string BusinessCode { get; set; }
			[JsonPropertyName("sub_mchid")]
			public string MerchantId { get; set; }
			[JsonPropertyName("applyment_state")]
			public string State { get; set; }
			[JsonPropertyName("applyment_state_msg")]
			public string Description { get; set; }
			[JsonPropertyName("sign_url")]
			public string SignUrl { get; set; }
			[JsonPropertyName("audit_detail")]
			public Rejection[] Rejections { get; set; }
			[JsonIgnore]
			public bool IsCompleted => this.State == "APPLYMENT_STATE_FINISHED";
		}

		public sealed class Rejection
		{
			[JsonPropertyName("field")]
			public string Field { get; set; }
			[JsonPropertyName("field_name")]
			public string Name { get; set; }
			[JsonPropertyName("reject_reason")]
			public string Reason { get; set; }
		}
		#endregion
	}
}
