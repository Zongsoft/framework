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
			public sealed class Settlement
			{
				[JsonPropertyName("settlement_id")]
				public string SettlementId { get; set; }

				[JsonPropertyName("qualification_type")]
				public string QualificationType { get; set; }

				[JsonPropertyName("qualifications")]
				public string[] Qualifications { get; set; }

				[JsonPropertyName("activities_id")]
				public string ActivitiesId { get; set; }

				[JsonPropertyName("activities_rate")]
				public string ActivitiesRate { get; set; }

				[JsonPropertyName("activities_additions")]
				public string[] ActivitiesAdditions { get; set; }

				[JsonPropertyName("debit_activities_rate")]
				public string DebitActivitiesRate { get; set; }

				[JsonPropertyName("credit_activities_rate")]
				public string CreditActivitiesRate { get; set; }

				internal void Validate()
				{
					if(string.IsNullOrWhiteSpace(this.SettlementId))
						throw new ArgumentException(null, nameof(this.SettlementId));

					if(string.IsNullOrWhiteSpace(this.QualificationType))
						throw new ArgumentException(null, nameof(this.QualificationType));

					if(this.Qualifications != null)
					{
						foreach(var item in this.Qualifications)
						{
							if(string.IsNullOrWhiteSpace(item))
								throw new ArgumentException(null, nameof(this.Qualifications));
						}
					}

					if(this.ActivitiesAdditions != null)
					{
						foreach(var item in this.ActivitiesAdditions)
						{
							if(string.IsNullOrWhiteSpace(item))
								throw new ArgumentException(null, nameof(this.ActivitiesAdditions));
						}
					}
				}
			}

			public sealed class BankAccount
			{
				[JsonPropertyName("bank_account_type")]
				public string BankAccountType { get; set; }

				[JsonPropertyName("account_name")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string AccountName { get; set; }

				[JsonPropertyName("account_bank")]
				public string AccountBank { get; set; }

				[JsonPropertyName("bank_address_code")]
				public string BankAddressCode { get; set; }

				[JsonPropertyName("bank_branch_id")]
				public string BankBranchId { get; set; }

				[JsonPropertyName("bank_name")]
				public string BankName { get; set; }

				[JsonPropertyName("account_number")]
				[JsonConverter(typeof(Json.CryptographyConverter))]
				public string AccountNumber { get; set; }

				internal void Validate()
				{
					if(string.IsNullOrWhiteSpace(this.BankAccountType))
						throw new ArgumentException(null, nameof(this.BankAccountType));

					if(string.IsNullOrWhiteSpace(this.AccountName))
						throw new ArgumentException(null, nameof(this.AccountName));

					if(string.IsNullOrWhiteSpace(this.AccountBank))
						throw new ArgumentException(null, nameof(this.AccountBank));

					if(string.IsNullOrWhiteSpace(this.AccountNumber))
						throw new ArgumentException(null, nameof(this.AccountNumber));
				}
			}

			public sealed class Addition
			{
				[JsonPropertyName("legal_person_commitment")]
				public string LegalPersonCommitment { get; set; }

				[JsonPropertyName("legal_person_video")]
				public string LegalPersonVideo { get; set; }

				[JsonPropertyName("business_addition_pics")]
				public string[] BusinessAdditionPics { get; set; }

				[JsonPropertyName("business_addition_msg")]
				public string BusinessAdditionMsg { get; set; }

				internal void Validate()
				{
					if(this.BusinessAdditionPics != null)
					{
						foreach(var item in this.BusinessAdditionPics)
						{
							if(string.IsNullOrWhiteSpace(item))
								throw new ArgumentException(null, nameof(this.BusinessAdditionPics));
						}
					}
				}
			}
			#endregion
		}
	}
}
