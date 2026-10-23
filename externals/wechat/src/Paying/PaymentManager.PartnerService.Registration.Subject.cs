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
			public sealed class Subject
			{
				[JsonPropertyName("subject_type")]
				public string SubjectType { get; set; }

				[JsonPropertyName("finance_institution")]
				public bool? FinanceInstitution { get; set; }

				[JsonPropertyName("business_license_info")]
				public BusinessLicense BusinessLicenseInfo { get; set; }

				[JsonPropertyName("certificate_info")]
				public Certificate CertificateInfo { get; set; }

				[JsonPropertyName("certificate_letter_copy")]
				public string CertificateLetterCopy { get; set; }

				[JsonPropertyName("finance_institution_info")]
				public FinanceInstitution FinanceInstitutionInfo { get; set; }

				[JsonPropertyName("identity_info")]
				public Identity IdentityInfo { get; set; }

				[JsonPropertyName("ubo_info_list")]
				public UboInfo[] UboInfoList { get; set; }

				internal void Validate()
				{
					if(string.IsNullOrWhiteSpace(this.SubjectType))
						throw new ArgumentException(null, nameof(this.SubjectType));

					this.BusinessLicenseInfo?.Validate();

					this.CertificateInfo?.Validate();

					this.FinanceInstitutionInfo?.Validate();

					if(this.IdentityInfo == null)
						throw new ArgumentException(null, nameof(this.IdentityInfo));

					this.IdentityInfo?.Validate();

					if(this.UboInfoList != null)
					{
						foreach(var item in this.UboInfoList)
						{
							if(item == null)
								throw new ArgumentException(null, nameof(this.UboInfoList));

							item.Validate();
						}
					}
				}
			}

			public sealed class BusinessLicense
			{
				[JsonPropertyName("license_copy")]
				public string LicenseCopy { get; set; }

				[JsonPropertyName("license_number")]
				public string LicenseNumber { get; set; }

				[JsonPropertyName("merchant_name")]
				public string MerchantName { get; set; }

				[JsonPropertyName("legal_person")]
				public string LegalPerson { get; set; }

				[JsonPropertyName("license_address")]
				public string LicenseAddress { get; set; }

				[JsonPropertyName("period_begin")]
				public string PeriodBegin { get; set; }

				[JsonPropertyName("period_end")]
				public string PeriodEnd { get; set; }

				internal void Validate()
				{
					if(string.IsNullOrWhiteSpace(this.LicenseCopy))
						throw new ArgumentException(null, nameof(this.LicenseCopy));

					if(string.IsNullOrWhiteSpace(this.LicenseNumber))
						throw new ArgumentException(null, nameof(this.LicenseNumber));

					if(string.IsNullOrWhiteSpace(this.MerchantName))
						throw new ArgumentException(null, nameof(this.MerchantName));

					if(string.IsNullOrWhiteSpace(this.LegalPerson))
						throw new ArgumentException(null, nameof(this.LegalPerson));
				}
			}

			public sealed class Certificate
			{
				[JsonPropertyName("cert_copy")]
				public string CertCopy { get; set; }

				[JsonPropertyName("cert_type")]
				public string CertType { get; set; }

				[JsonPropertyName("cert_number")]
				public string CertNumber { get; set; }

				[JsonPropertyName("merchant_name")]
				public string MerchantName { get; set; }

				[JsonPropertyName("company_address")]
				public string CompanyAddress { get; set; }

				[JsonPropertyName("legal_person")]
				public string LegalPerson { get; set; }

				[JsonPropertyName("period_begin")]
				public string PeriodBegin { get; set; }

				[JsonPropertyName("period_end")]
				public string PeriodEnd { get; set; }

				internal void Validate()
				{
					if(string.IsNullOrWhiteSpace(this.CertCopy))
						throw new ArgumentException(null, nameof(this.CertCopy));

					if(string.IsNullOrWhiteSpace(this.CertType))
						throw new ArgumentException(null, nameof(this.CertType));

					if(string.IsNullOrWhiteSpace(this.CertNumber))
						throw new ArgumentException(null, nameof(this.CertNumber));

					if(string.IsNullOrWhiteSpace(this.MerchantName))
						throw new ArgumentException(null, nameof(this.MerchantName));

					if(string.IsNullOrWhiteSpace(this.CompanyAddress))
						throw new ArgumentException(null, nameof(this.CompanyAddress));

					if(string.IsNullOrWhiteSpace(this.LegalPerson))
						throw new ArgumentException(null, nameof(this.LegalPerson));

					if(string.IsNullOrWhiteSpace(this.PeriodBegin))
						throw new ArgumentException(null, nameof(this.PeriodBegin));

					if(string.IsNullOrWhiteSpace(this.PeriodEnd))
						throw new ArgumentException(null, nameof(this.PeriodEnd));
				}
			}

			public sealed class FinanceInstitution
			{
				[JsonPropertyName("finance_type")]
				public string FinanceType { get; set; }

				[JsonPropertyName("finance_license_pics")]
				public string[] FinanceLicensePics { get; set; }

				internal void Validate()
				{
					if(string.IsNullOrWhiteSpace(this.FinanceType))
						throw new ArgumentException(null, nameof(this.FinanceType));

					if(this.FinanceLicensePics == null || this.FinanceLicensePics.Length == 0)
						throw new ArgumentException(null, nameof(this.FinanceLicensePics));

					if(this.FinanceLicensePics != null)
					{
						foreach(var item in this.FinanceLicensePics)
						{
							if(string.IsNullOrWhiteSpace(item))
								throw new ArgumentException(null, nameof(this.FinanceLicensePics));
						}
					}
				}
			}

			public sealed class Identity
			{
				[JsonPropertyName("id_holder_type")]
				public string IdHolderType { get; set; }

				[JsonPropertyName("id_doc_type")]
				public string IdDocType { get; set; }

				[JsonPropertyName("authorize_letter_copy")]
				public string AuthorizeLetterCopy { get; set; }

				[JsonPropertyName("id_card_info")]
				public IdCard IdCardInfo { get; set; }

				[JsonPropertyName("id_doc_info")]
				public IdDoc IdDocInfo { get; set; }

				[JsonPropertyName("owner")]
				public bool? Owner { get; set; }

				internal void Validate()
				{
					this.IdCardInfo?.Validate();

					this.IdDocInfo?.Validate();
				}
			}

			public sealed class IdCard
			{
				[JsonPropertyName("id_card_copy")]
				public string IdCardCopy { get; set; }

				[JsonPropertyName("id_card_national")]
				public string IdCardNational { get; set; }

				[JsonPropertyName("id_card_name")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string IdCardName { get; set; }

				[JsonPropertyName("id_card_number")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string IdCardNumber { get; set; }

				[JsonPropertyName("id_card_address")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string IdCardAddress { get; set; }

				[JsonPropertyName("card_period_begin")]
				public string CardPeriodBegin { get; set; }

				[JsonPropertyName("card_period_end")]
				public string CardPeriodEnd { get; set; }

				internal void Validate()
				{
					if(string.IsNullOrWhiteSpace(this.IdCardCopy))
						throw new ArgumentException(null, nameof(this.IdCardCopy));

					if(string.IsNullOrWhiteSpace(this.IdCardNational))
						throw new ArgumentException(null, nameof(this.IdCardNational));

					if(string.IsNullOrWhiteSpace(this.IdCardName))
						throw new ArgumentException(null, nameof(this.IdCardName));

					if(string.IsNullOrWhiteSpace(this.IdCardNumber))
						throw new ArgumentException(null, nameof(this.IdCardNumber));

					if(string.IsNullOrWhiteSpace(this.CardPeriodBegin))
						throw new ArgumentException(null, nameof(this.CardPeriodBegin));

					if(string.IsNullOrWhiteSpace(this.CardPeriodEnd))
						throw new ArgumentException(null, nameof(this.CardPeriodEnd));
				}
			}

			public sealed class IdDoc
			{
				[JsonPropertyName("id_doc_copy")]
				public string IdDocCopy { get; set; }

				[JsonPropertyName("id_doc_copy_back")]
				public string IdDocCopyBack { get; set; }

				[JsonPropertyName("id_doc_name")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string IdDocName { get; set; }

				[JsonPropertyName("id_doc_number")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string IdDocNumber { get; set; }

				[JsonPropertyName("id_doc_address")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string IdDocAddress { get; set; }

				[JsonPropertyName("doc_period_begin")]
				public string DocPeriodBegin { get; set; }

				[JsonPropertyName("doc_period_end")]
				public string DocPeriodEnd { get; set; }

				internal void Validate()
				{
					if(string.IsNullOrWhiteSpace(this.IdDocCopy))
						throw new ArgumentException(null, nameof(this.IdDocCopy));

					if(string.IsNullOrWhiteSpace(this.IdDocName))
						throw new ArgumentException(null, nameof(this.IdDocName));

					if(string.IsNullOrWhiteSpace(this.IdDocNumber))
						throw new ArgumentException(null, nameof(this.IdDocNumber));

					if(string.IsNullOrWhiteSpace(this.DocPeriodBegin))
						throw new ArgumentException(null, nameof(this.DocPeriodBegin));

					if(string.IsNullOrWhiteSpace(this.DocPeriodEnd))
						throw new ArgumentException(null, nameof(this.DocPeriodEnd));
				}
			}

			public sealed class UboInfo
			{
				[JsonPropertyName("ubo_id_doc_type")]
				public string UboIdDocType { get; set; }

				[JsonPropertyName("ubo_id_doc_copy")]
				public string UboIdDocCopy { get; set; }

				[JsonPropertyName("ubo_id_doc_copy_back")]
				public string UboIdDocCopyBack { get; set; }

				[JsonPropertyName("ubo_id_doc_name")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string UboIdDocName { get; set; }

				[JsonPropertyName("ubo_id_doc_number")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string UboIdDocNumber { get; set; }

				[JsonPropertyName("ubo_id_doc_address")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string UboIdDocAddress { get; set; }

				[JsonPropertyName("ubo_period_begin")]
				public string UboPeriodBegin { get; set; }

				[JsonPropertyName("ubo_period_end")]
				public string UboPeriodEnd { get; set; }

				internal void Validate()
				{
					if(string.IsNullOrWhiteSpace(this.UboIdDocType))
						throw new ArgumentException(null, nameof(this.UboIdDocType));

					if(string.IsNullOrWhiteSpace(this.UboIdDocCopy))
						throw new ArgumentException(null, nameof(this.UboIdDocCopy));

					if(string.IsNullOrWhiteSpace(this.UboIdDocName))
						throw new ArgumentException(null, nameof(this.UboIdDocName));

					if(string.IsNullOrWhiteSpace(this.UboIdDocNumber))
						throw new ArgumentException(null, nameof(this.UboIdDocNumber));

					if(string.IsNullOrWhiteSpace(this.UboIdDocAddress))
						throw new ArgumentException(null, nameof(this.UboIdDocAddress));

					if(string.IsNullOrWhiteSpace(this.UboPeriodBegin))
						throw new ArgumentException(null, nameof(this.UboPeriodBegin));

					if(string.IsNullOrWhiteSpace(this.UboPeriodEnd))
						throw new ArgumentException(null, nameof(this.UboPeriodEnd));
				}
			}
			#endregion
		}
	}
}
