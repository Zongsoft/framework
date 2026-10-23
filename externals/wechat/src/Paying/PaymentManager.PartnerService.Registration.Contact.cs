using System;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public partial class PartnerService
	{
		public partial class Registration
		{
			#region 嵌套类型
			public sealed class Contact
			{
				#region 公共属性
				[JsonPropertyName("contact_type")]
				public string ContactType { get; set; }

				[JsonPropertyName("contact_name")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string ContactName { get; set; }

				[JsonPropertyName("contact_id_doc_type")]
				public string ContactIdDocType { get; set; }

				[JsonPropertyName("contact_id_number")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string ContactIdNumber { get; set; }

				[JsonPropertyName("contact_id_doc_copy")]
				public string ContactIdDocCopy { get; set; }

				[JsonPropertyName("contact_id_doc_copy_back")]
				public string ContactIdDocCopyBack { get; set; }

				[JsonPropertyName("contact_period_begin")]
				public string ContactPeriodBegin { get; set; }

				[JsonPropertyName("contact_period_end")]
				public string ContactPeriodEnd { get; set; }

				[JsonPropertyName("openid")]
				public string Openid { get; set; }

				[JsonPropertyName("mobile_phone")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string MobilePhone { get; set; }

				[JsonPropertyName("contact_email")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string ContactEmail { get; set; }
				#endregion

				internal void Validate()
				{
					if(string.IsNullOrWhiteSpace(this.ContactType))
						throw new ArgumentException(null, nameof(this.ContactType));

					if(string.IsNullOrWhiteSpace(this.ContactName))
						throw new ArgumentException(null, nameof(this.ContactName));

					if(string.IsNullOrWhiteSpace(this.MobilePhone))
						throw new ArgumentException(null, nameof(this.MobilePhone));

					if(string.IsNullOrWhiteSpace(this.ContactEmail))
						throw new ArgumentException(null, nameof(this.ContactEmail));
				}
			}
			#endregion
		}
	}
}
