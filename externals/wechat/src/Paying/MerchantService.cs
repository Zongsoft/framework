/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat.Paying
{
	public partial class PaymentManager
	{
		public class MerchantService
		{
			#region 私有变量
			private readonly IAuthority _authority;
			#endregion

			#region 成员字段
			private HttpClient _client;
			#endregion

			#region 构造函数
			public MerchantService(IAuthority authority)
			{
				_authority = authority ?? throw new ArgumentNullException(nameof(authority));
			}
			#endregion

			#region 保护属性
			protected HttpClient Client
			{
				get
				{
					if(_client != null)
						return _client;

					lock(this)
					{
						if(_client == null)
							_client = HttpClientFactory.GetHttpClient(_authority.Certificate);
					}

					return _client;
				}
			}
			#endregion

			#region 公共方法
			public async ValueTask<OperationResult<string>> RegisterAsync(Registration request, CancellationToken cancellation = default)
			{
				if(request == null)
					throw new ArgumentNullException(nameof(request));

				//获取微信支付平台的数字证书
				var certificate = await _authority.GetCertificateAsync(cancellation);

				var result = await this.Client.PostAsync<Registration, RegistrationResult>("ecommerce/applyments/", request, certificate, cancellation);
				return result.Succeed ? OperationResult.Success(result.Value.ApplymentId.ToString()) : result.Failure;
			}

			public ValueTask<OperationResult<Applyment>> GetAsync(string key, CancellationToken cancellation = default)
			{
				if(string.IsNullOrEmpty(key))
					throw new ArgumentNullException(nameof(key));

				return this.Client.GetAsync<Applyment>($"ecommerce/applyments/out-request-no/{key}", cancellation);
			}
			#endregion

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

				[JsonPropertyName("organization_cert_info")]
				public OrganizationLicenseInfo? OrganizationLicense { get; set; }

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

				[JsonPropertyName("need_account_info")]
				public bool HasBankAccount { get => this.BankAccount.HasValue; }

				[JsonPropertyName("account_info")]
				public BankAccountInfo? BankAccount { get; set; }

				[JsonPropertyName("qualifications")]
				public string[] Qualifications { get; set; }

				[JsonPropertyName("business_addition_pics")]
				public string[] Attachments { get; set; }

				[JsonPropertyName("business_addition_desc")]
				public string Remark { get; set; }
				#endregion

				#region 嵌套结构
				public struct BusinessLicenseInfo
				{
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
					public DateTime? Expiration { get; set; }
				}

				public struct OrganizationLicenseInfo
				{
					[JsonPropertyName("organization_number")]
					public string LicenseCode { get; set; }
					[JsonPropertyName("organization_copy")]
					public string Photo { get; set; }
					[JsonPropertyName("organization_time")]
					public DateTime? Expiration { get; set; }
				}

				public struct IdentityCardInfo
				{
					[JsonPropertyName("id_card_name")]
					[JsonConverter(typeof(Json.CryptographyConverter))]
					public string Name { get; set; }

					[JsonPropertyName("id_card_number")]
					[JsonConverter(typeof(Json.CryptographyConverter))]
					public string Code { get; set; }

					[JsonPropertyName("id_card_valid_time")]
					public DateTime? Expration { get; set; }

					[JsonPropertyName("id_card_copy")]
					public string PhotoA { get; set; }

					[JsonPropertyName("id_card_national")]
					public string PhotoB { get; set; }
				}

				public struct IdentityLicenseInfo
				{
					[JsonPropertyName("id_doc_name")]
					public string Name { get; set; }
					[JsonPropertyName("id_doc_number")]
					public string Code { get; set; }
					[JsonPropertyName("doc_period_end")]
					public DateTime? Expration { get; set; }
					[JsonPropertyName("id_doc_copy")]
					public string Photo { get; set; }
				}

				public struct StoreInfo
				{
					[JsonPropertyName("store_name")]
					public string Name { get; set; }
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

					[JsonPropertyName("contact_email")]
					[JsonConverter(typeof(Json.CryptographyConverter))]
					public string Email { get; set; }
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

					[JsonPropertyName("bank_address_code")]
					[JsonNumberHandling(JsonNumberHandling.WriteAsString)]
					public int AddressId { get; set; }

					[JsonPropertyName("account_bank")]
					public string BankAbbr { get; set; }

					[JsonPropertyName("bank_name")]
					public string BankName { get; set; }

					[JsonPropertyName("bank_branch_id")]
					public string BankCode { get; set; }
				}
				#endregion
			}

			private struct RegistrationResult
			{
				[JsonPropertyName("applyment_id")]
				public long ApplymentId { get; set; }
			}

			public class Applyment
			{
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
			#endregion
		}
	}
}
