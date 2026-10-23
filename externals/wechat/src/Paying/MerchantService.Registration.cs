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
		public class Registration
		{
			#region 公共属性
			[JsonPropertyName("out_request_no")]
			public string RequestId { get; set; }

			[JsonPropertyName("organization_type")]
			[JsonNumberHandling(JsonNumberHandling.WriteAsString)]
			public int MerchantKind { get; set; }

			[JsonPropertyName("merchant_shortname")]
			public string MerchantAbbr { get; set; }

			[JsonPropertyName("business_license_info")]
			public BusinessLicenseInfo? BusinessLicense { get; set; }

			[JsonPropertyName("id_doc_type")]
			public string IdentityType { get; set; }

			[JsonPropertyName("id_card_info")]
			public IdentityCardInfo? IdentityCard { get; set; }

			[JsonPropertyName("id_doc_info")]
			public IdentityLicenseInfo? IdentityLicense { get; set; }

			[JsonPropertyName("sales_scene_info")]
			public StoreInfo Store { get; set; }

			[JsonPropertyName("contact_info")]
			public ContactInfo Contact { get; set; }

			[JsonPropertyName("account_info")]
			public BankAccountInfo? BankAccount { get; set; }

			[JsonPropertyName("qualifications")]
			[JsonConverter(typeof(MediaListConverter))]
			public string[] Qualifications { get; set; }

			[JsonPropertyName("business_addition_pics")]
			[JsonConverter(typeof(MediaListConverter))]
			public string[] Attachments { get; set; }

			[JsonPropertyName("business_addition_desc")]
			public string Remark { get; set; }
			#endregion

			#region 嵌套结构
			public struct BusinessLicenseInfo
			{
				[JsonPropertyName("cert_type")]
				public string CertificateType { get; set; }

				[JsonPropertyName("business_license_number")]
				public string LicenseCode { get; set; }
				[JsonPropertyName("merchant_name")]
				public string MerchantName { get; set; }
				[JsonPropertyName("legal_person")]
				public string PrincipalName { get; set; }
				[JsonPropertyName("company_address")]
				public string Address { get; set; }
				[JsonPropertyName("business_license_copy")]
				public string Photo { get; set; }
				[JsonPropertyName("business_time")]
				public string Period { get; set; }
			}

			public struct IdentityCardInfo
			{
				[JsonPropertyName("id_card_name")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string Name { get; set; }

				[JsonPropertyName("id_card_number")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string Code { get; set; }

				[JsonPropertyName("id_card_valid_time_begin")]
				[JsonConverter(typeof(Json.DateConverter))]
				public DateTime? IssuedTime { get; set; }

				[JsonPropertyName("id_card_valid_time")]
				[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
				[JsonConverter(typeof(Json.DateConverter))]
				public DateTime? Expration { get; set; }

				[JsonPropertyName("id_card_copy")]
				public string PhotoA { get; set; }

				[JsonPropertyName("id_card_national")]
				public string PhotoB { get; set; }
			}

			public struct IdentityLicenseInfo
			{
				[JsonPropertyName("id_doc_name")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string Name { get; set; }
				[JsonPropertyName("id_doc_number")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string Code { get; set; }
				[JsonPropertyName("doc_period_begin")]
				[JsonConverter(typeof(Json.DateConverter))]
				public DateTime? IssuedTime { get; set; }

				[JsonPropertyName("doc_period_end")]
				[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
				[JsonConverter(typeof(Json.DateConverter))]
				public DateTime? Expration { get; set; }
				[JsonPropertyName("id_doc_copy")]
				public string Photo { get; set; }
				[JsonPropertyName("id_doc_copy_back")]
				public string PhotoBack { get; set; }
			}

			public struct StoreInfo
			{
				[JsonPropertyName("store_name")]
				public string Name { get; set; }
				[JsonPropertyName("store_qr_code")]
				public string QrCode { get; set; }
				[JsonPropertyName("store_url")]
				public string WebUrl { get; set; }
				[JsonPropertyName("mini_program_sub_appid")]
				public string AppletCode { get; set; }
			}

			public struct ContactInfo
			{
				[JsonPropertyName("contact_type")]
				[JsonNumberHandling(JsonNumberHandling.WriteAsString)]
				public int ContactKind { get; set; }

				[JsonPropertyName("contact_name")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string Name { get; set; }

				[JsonPropertyName("contact_id_card_number")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string IdentityNo { get; set; }

				[JsonPropertyName("mobile_phone")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string Phone { get; set; }

			}

			public struct BankAccountInfo
			{
				[JsonPropertyName("bank_account_type")]
				[JsonNumberHandling(JsonNumberHandling.WriteAsString)]
				public int AccountKind { get; set; }

				[JsonPropertyName("account_name")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string AccountName { get; set; }

				[JsonPropertyName("account_number")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string AccountCode { get; set; }

				[JsonPropertyName("account_bank")]
				public string BankAbbr { get; set; }

				[JsonPropertyName("bank_name")]
				public string BankName { get; set; }

				[JsonPropertyName("bank_branch_id")]
				public string BankCode { get; set; }
			}
			#endregion
		}
		#endregion

		#region 私有方法
		private static void ValidateRegistration(Registration request)
		{
			ValidateRegistrationText(request.RequestId, 124, nameof(request));
			ValidateRegistrationText(request.MerchantAbbr, 64, nameof(request));
			if(request.MerchantKind is not (2401 or 2500 or 4 or 2 or 3 or 2502 or 1708) || request.Contact.ContactKind is not (65 or 66))
				throw new ArgumentException(null, nameof(request));

			ValidateRegistrationText(request.Contact.Name, 100, nameof(request));
			ValidateRegistrationText(request.Contact.Phone, 32, nameof(request));
			ValidateRegistrationText(request.Store.Name, 256, nameof(request));

			if(string.IsNullOrWhiteSpace(request.Store.QrCode) &&
				(!Uri.TryCreate(request.Store.WebUrl, UriKind.Absolute, out var url) || url.Scheme is not ("https" or "http") || !string.IsNullOrEmpty(url.UserInfo)))
				throw new ArgumentException(null, nameof(request));
			if(!request.BankAccount.HasValue || request.BankAccount.Value.AccountKind is not (74 or 75))
				throw new ArgumentException(null, nameof(request));

			ValidateRegistrationText(request.BankAccount.Value.AccountName, 128, nameof(request));
			ValidateRegistrationText(request.BankAccount.Value.AccountCode, 128, nameof(request));
			ValidateRegistrationText(request.BankAccount.Value.BankAbbr, 128, nameof(request));

			if(request.MerchantKind is 2401 or 2500)
			{
				if(request.BusinessLicense.HasValue || request.Contact.ContactKind != 65)
					throw new ArgumentException(null, nameof(request));
			}
			else
			{
				if(!request.BusinessLicense.HasValue)
					throw new ArgumentException(null, nameof(request));

				ValidateRegistrationText(request.BusinessLicense.Value.LicenseCode, 128, nameof(request));
				ValidateRegistrationText(request.BusinessLicense.Value.Photo, 512, nameof(request));
				ValidateRegistrationText(request.BusinessLicense.Value.MerchantName, 128, nameof(request));
				ValidateRegistrationText(request.BusinessLicense.Value.PrincipalName, 100, nameof(request));

				var certificate = request.BusinessLicense.Value.CertificateType;
				if(request.MerchantKind == 3 && certificate != "CERTIFICATE_TYPE_2388" ||
					request.MerchantKind == 2502 && certificate != "CERTIFICATE_TYPE_2389" ||
					request.MerchantKind == 1708 && certificate is not ("CERTIFICATE_TYPE_2389" or "CERTIFICATE_TYPE_2394" or "CERTIFICATE_TYPE_2395" or "CERTIFICATE_TYPE_2396" or "CERTIFICATE_TYPE_2399" or "CERTIFICATE_TYPE_2400" or "CERTIFICATE_TYPE_2520" or "CERTIFICATE_TYPE_2521" or "CERTIFICATE_TYPE_2522"))
					throw new ArgumentException(null, nameof(request));
			}

			if(string.IsNullOrEmpty(request.IdentityType) || request.IdentityType == "IDENTIFICATION_TYPE_MAINLAND_IDCARD")
			{
				if(!request.IdentityCard.HasValue || request.IdentityLicense.HasValue || !request.IdentityCard.Value.IssuedTime.HasValue)
					throw new ArgumentException(null, nameof(request));

				ValidateRegistrationText(request.IdentityCard.Value.Name, 100, nameof(request));
				ValidateRegistrationText(request.IdentityCard.Value.Code, 64, nameof(request));
				ValidateRegistrationText(request.IdentityCard.Value.PhotoA, 512, nameof(request));
				ValidateRegistrationText(request.IdentityCard.Value.PhotoB, 512, nameof(request));
			}
			else
			{
				if(!request.IdentityLicense.HasValue || request.IdentityCard.HasValue || !request.IdentityLicense.Value.IssuedTime.HasValue || request.MerchantKind is 2401 or 2500 or 2502)
					throw new ArgumentException(null, nameof(request));

				ValidateRegistrationText(request.IdentityLicense.Value.Name, 100, nameof(request));
				ValidateRegistrationText(request.IdentityLicense.Value.Code, 64, nameof(request));
				ValidateRegistrationText(request.IdentityLicense.Value.Photo, 512, nameof(request));
			}

			ValidateMedia(request.Qualifications, 5);
			ValidateMedia(request.Attachments, 15);

			static void ValidateMedia(string[] values, int maximum)
			{
				if(values != null && (values.Length > maximum || values.Any(string.IsNullOrWhiteSpace) || Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(values)) > 1024))
					throw new ArgumentException(null, nameof(request));
			}
		}

		private static void ValidateRegistrationText(string text, int maximum, string parameter)
		{
			if(string.IsNullOrWhiteSpace(text) || Encoding.UTF8.GetByteCount(text) > maximum || text.Any(char.IsControl))
				throw new ArgumentException(null, parameter);
		}
		#endregion

		#region 序列化
		private sealed class MediaListConverter : JsonConverter<string[]>
		{
			public override string[] Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => JsonSerializer.Deserialize<string[]>(reader.GetString());
			public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options) => writer.WriteStringValue(JsonSerializer.Serialize(value));
		}
		#endregion
	}
}
