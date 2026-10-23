using System;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public partial class PartnerService
	{
		public partial class Registration
		{
			#region 公共属性
			[JsonPropertyName("business_code")]
			public string BusinessCode { get; set; }

			[JsonPropertyName("contact_info")]
			public Contact ContactInfo { get; set; }

			[JsonPropertyName("subject_info")]
			public Subject SubjectInfo { get; set; }

			[JsonPropertyName("business_info")]
			public Business BusinessInfo { get; set; }

			[JsonPropertyName("settlement_info")]
			public Settlement SettlementInfo { get; set; }

			[JsonPropertyName("bank_account_info")]
			public BankAccount BankAccountInfo { get; set; }

			[JsonPropertyName("addition_info")]
			public Addition AdditionInfo { get; set; }
			#endregion

			#region 内部方法
			internal void Validate()
			{
				if(string.IsNullOrWhiteSpace(this.BusinessCode))
					throw new ArgumentException(null, nameof(this.BusinessCode));

				if(this.ContactInfo == null)
					throw new ArgumentException(null, nameof(this.ContactInfo));

				this.ContactInfo?.Validate();

				if(this.SubjectInfo == null)
					throw new ArgumentException(null, nameof(this.SubjectInfo));

				this.SubjectInfo?.Validate();

				if(this.BusinessInfo == null)
					throw new ArgumentException(null, nameof(this.BusinessInfo));

				this.BusinessInfo?.Validate();

				if(this.SettlementInfo == null)
					throw new ArgumentException(null, nameof(this.SettlementInfo));

				this.SettlementInfo?.Validate();

				if(this.BankAccountInfo == null)
					throw new ArgumentException(null, nameof(this.BankAccountInfo));

				this.BankAccountInfo?.Validate();

				this.AdditionInfo?.Validate();
			}
			#endregion
		}
	}
}
