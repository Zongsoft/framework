using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public partial class MerchantService
	{
		#region 实体模型
		private struct RegistrationResult
		{
			[JsonPropertyName("out_request_no")]
			public string RequestId { get; set; }
			[JsonPropertyName("applyment_id")]
			public long ApplymentId { get; set; }
		}

		public class Applyment
		{
			[JsonPropertyName("out_request_no")]
			public string RequestId { get; set; }
			[JsonPropertyName("applyment_id")]
			public long ApplymentId { get; set; }
			[JsonPropertyName("sub_mchid")]
			public string MerchantId { get; set; }
			[JsonPropertyName("applyment_state")]
			public string Status { get; set; }
			[JsonPropertyName("applyment_state_desc")]
			public string StatusDescription { get; set; }
			[JsonPropertyName("sign_state")]
			public string SignStatus { get; set; }
			[JsonPropertyName("sign_url")]
			public string SignUrl { get; set; }
			[JsonPropertyName("legal_validation_url")]
			public string ValidationUrl { get; set; }
			[JsonPropertyName("account_validation")]
			public AccountInfo Account { get; set; }
			[JsonPropertyName("audit_detail")]
			public RejectionInfo[] Rejects { get; set; }

			public string GetRejection()
			{
				var rejects = this.Rejects;
				if(rejects == null || rejects.Length == 0)
					return null;

				var text = new System.Text.StringBuilder();

				for(int i = 0; i < rejects.Length; i++)
				{
					text.AppendLine($"{rejects[i].Name}:{rejects[i].Reason}");
				}

				return text.ToString();
			}

			public struct RejectionInfo
			{
				[JsonPropertyName("param_name")]
				public string Name { get; set; }

				[JsonPropertyName("reject_reason")]
				public string Reason { get; set; }
			}

			public struct AccountInfo
			{
				[JsonPropertyName("pay_amount")]
				public int Amount { get; set; }

				[JsonPropertyName("account_no")]
				public string Code { get; set; }

				[JsonPropertyName("account_name")]
				public string Name { get; set; }

				[JsonPropertyName("deadline")]
				[JsonConverter(typeof(DeadlineConverter))]
				public DateTime Deadline { get; set; }

				[JsonPropertyName("destination_account_number")]
				public string ReceiverCode { get; set; }

				[JsonPropertyName("destination_account_name")]
				public string ReceiverName { get; set; }

				[JsonPropertyName("destination_account_bank")]
				public string ReceiverBank { get; set; }

				[JsonPropertyName("city")]
				public string ReceiverCity { get; set; }

				[JsonPropertyName("remark")]
				public string Remark { get; set; }
			}
		}
		private sealed class DeadlineConverter : JsonConverter<DateTime>
		{
			public override DateTime Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
				DateTime.TryParseExact(reader.GetString(), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var value) ? value : throw new JsonException();
			public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
		}
		#endregion
	}
}
