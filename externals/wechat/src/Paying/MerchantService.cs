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
using System.Text.Json;
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
			#region 成员字段
			private HttpClient _client;
			#endregion

			#region 构造函数
			internal MerchantService(IAuthority authority)
			{
				this.Authority = authority ?? throw new ArgumentNullException(nameof(authority));
				this.Contracts = new ContractService(this);
			}
			#endregion

			#region 公共属性
			public IAuthority Authority { get; }
			public ContractService Contracts { get; }
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
							_client = HttpClientFactory.GetHttpClient(this.Authority.Certificate);
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
				var certificate = await this.Authority.GetCertificateAsync(cancellation);

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
					public string Period { get; set; }
				}

				public struct OrganizationLicenseInfo
				{
					[JsonPropertyName("organization_number")]
					public string LicenseCode { get; set; }
					[JsonPropertyName("organization_copy")]
					public string Photo { get; set; }
					[JsonPropertyName("organization_time")]
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

					[JsonPropertyName("id_card_valid_time")]
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
					public string Name { get; set; }
					[JsonPropertyName("id_doc_number")]
					public string Code { get; set; }
					[JsonPropertyName("doc_period_end")]
					[JsonConverter(typeof(Json.DateConverter))]
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

			#region 嵌套子类
			public class ContractService
			{
				#region 成员字段
				private readonly MerchantService _merchant;
				#endregion

				#region 构造函数
				internal ContractService(MerchantService merchant) => _merchant = merchant ?? throw new ArgumentNullException(nameof(merchant));
				#endregion

				#region 公共方法
				public async ValueTask<OperationResult<SignatoryInfo>> GetSignatoryAsync(string organizationId, string userId, CancellationToken cancellation = default)
				{
					if(string.IsNullOrEmpty(organizationId))
						throw new ArgumentNullException(nameof(organizationId));
					if(string.IsNullOrEmpty(userId))
						throw new ArgumentNullException(nameof(userId));

					var result = await _merchant.Client.GetAsync<SignatoryInfo>($"offlinefacemch/organizations/{organizationId}/users/out-user-id/{userId}", cancellation);
					return result;
				}

				public ValueTask<OperationResult<ContractInfo>> GetContractAsync(string contractId, string appId = null, CancellationToken cancellation = default)
				{
					if(string.IsNullOrEmpty(contractId))
						throw new ArgumentNullException(nameof(contractId));

					if(string.IsNullOrEmpty(appId))
						appId = _merchant.Authority.Accounts.Default.Code;

					return _merchant.Client.GetAsync<ContractInfo>($"offlineface/contracts/{contractId}?appid={appId}", cancellation);
				}

				public ValueTask<OperationResult<string>> AuthenticateAsync(string organizationId, string userId, CancellationToken cancellation = default) => this.AuthenticateAsync(organizationId, userId, null, cancellation);
				public async ValueTask<OperationResult<string>> AuthenticateAsync(string organizationId, string userId, string scene = null, CancellationToken cancellation = default)
				{
					if(string.IsNullOrEmpty(organizationId))
						throw new ArgumentNullException(nameof(organizationId));
					if(string.IsNullOrEmpty(userId))
						throw new ArgumentNullException(nameof(userId));

					if(string.IsNullOrWhiteSpace(scene))
						scene = "WEBSESSION";

					var result = await _merchant.Client.PostAsync<AuthenticateRequest, AuthenticateResult>("offlinefacemch/tokens", new AuthenticateRequest(organizationId, userId, scene), cancellation);
					return result.Succeed ? OperationResult.Success(result.Value.Token) : result.Failure;
				}

				public async ValueTask<OperationResult<string>> ApplyAsync(ApplyRequest request, CancellationToken cancellation = default)
				{
					if(request == null)
						throw new ArgumentNullException(nameof(request));

					var result = await _merchant.Client.PostAsync<ApplyRequest, ApplyResult>("offlineface/contracts/presign", request, cancellation);
					return result.Succeed ? OperationResult.Success(result.Value.Token) : result.Failure;
				}

				public async ValueTask<OperationResult> RevokeAsync(string organizationId, string userId, CancellationToken cancellation = default)
				{
					if(string.IsNullOrEmpty(organizationId))
						throw new ArgumentNullException(nameof(organizationId));
					if(string.IsNullOrEmpty(userId))
						throw new ArgumentNullException(nameof(userId));

					return await _merchant.Client.PostAsync<object>($"offlinefacemch/organizations/{organizationId}/users/user-id/{userId}/terminate-contract", null, cancellation);
				}
				#endregion

				#region 实体模型
				private struct AuthenticateRequest
				{
					public AuthenticateRequest(string organizationId, string userId, string scene)
					{
						this.Scene = scene;
						this.Data = new UserToken(organizationId, userId);
					}

					[JsonPropertyName("scene")]
					public string Scene { get; }
					[JsonPropertyName("web_init_data")]
					public UserToken Data { get; }

					public struct UserToken
					{
						public UserToken(string organizationId, string userId)
						{
							this.OrganizationId = organizationId;
							this.UserId = userId;
						}

						[JsonPropertyName("out_user_id")]
						public string UserId { get; }
						[JsonPropertyName("organization_id")]
						public string OrganizationId { get; }
					}
				}

				private struct AuthenticateResult
				{
					[JsonPropertyName("token")]
					public string Token { get; set; }
				}

				public class ApplyRequest
				{
					public ApplyRequest() { }
					public ApplyRequest(string business, PayerInfo payer)
					{
						this.BusinessName = business;
						this.Payer = payer;
					}

					[JsonPropertyName("business_name")]
					public string BusinessName { get; set; }
					[JsonPropertyName("contract_mode")]
					public ContractMode? ContractMode { get; set; }
					[JsonPropertyName("facepay_user")]
					public PayerInfo Payer { get; set; }
					[JsonPropertyName("limit_bank_card")]
					public BankCardInfo? BankCard { get; set; }

					public struct BankCardInfo
					{
						[JsonPropertyName("bank_card_number")]
						[JsonConverter(typeof(Json.CryptographyConverter))]
						public string Code { get; set; }

						[JsonPropertyName("identification_name")]
						[JsonConverter(typeof(Json.CryptographyConverter))]
						public string Name { get; set; }

						[JsonPropertyName("identification")]
						public Identity Identity { get; set; }

						[JsonPropertyName("bank_type")]
						public string BankType { get; set; }

						[JsonPropertyName("phone")]
						[JsonConverter(typeof(Json.CryptographyConverter))]
						public string PhoneNumber { get; set; }

						[JsonPropertyName("valid_thru")]
						public string Validity { get; set; }
					}

					public struct PayerInfo
					{
						[JsonPropertyName("out_user_id")]
						public string UserId { get; set; }
						[JsonPropertyName("identification_name")]
						[JsonConverter(typeof(Json.CryptographyConverter))]
						public string Name { get; set; }
						[JsonPropertyName("organization_id")]
						public string OrganizationId { get; set; }
						[JsonPropertyName("phone")]
						[JsonConverter(typeof(Json.CryptographyConverter))]
						public string PhoneNumber { get; set; }
						[JsonPropertyName("identification")]
						public Identity Identity { get; set; }
					}

					public struct Identity
					{
						[JsonPropertyName("identification_type")]
						public IdentityType Type { get; set; }
						[JsonPropertyName("identification_number")]
						[JsonConverter(typeof(Json.CryptographyConverter))]
						public string Code { get; set; }
					}

					[JsonConverter(typeof(IdentityTypeConverter))]
					public enum IdentityType
					{
						/// <summary>身份证</summary>
						IdentityId,
						/// <summary>护照</summary>
						Passport,
						/// <summary>港澳通行证</summary>
						Traffic,
					}

					private class IdentityTypeConverter : JsonConverter<IdentityType>
					{
						public override IdentityType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
						{
							if(reader.TokenType == JsonTokenType.String)
							{
								return reader.GetString() switch
								{
									"IDCARD" => IdentityType.IdentityId,
									"PASSPORT_NO" => IdentityType.Passport,
									"EEP_HK_MACAU" => IdentityType.Traffic,
									_ => IdentityType.IdentityId,
								};
							}

							return IdentityType.IdentityId;
						}

						public override void Write(Utf8JsonWriter writer, IdentityType value, JsonSerializerOptions options)
						{
							switch(value)
							{
								case IdentityType.IdentityId:
									writer.WriteStringValue("IDCARD");
									break;
								case IdentityType.Passport:
									writer.WriteStringValue("PASSPORT_NO");
									break;
								case IdentityType.Traffic:
									writer.WriteStringValue("EEP_HK_MACAU");
									break;
								default:
									writer.WriteStringValue("IDCARD");
									break;
							}
						}
					}
				}

				private struct ApplyResult
				{
					[JsonPropertyName("presign_token")]
					public string Token { get; set; }
				}

				public class ContractInfo
				{
					[JsonPropertyName("contract_id")]
					public string ContractId { get; set; }

					[JsonPropertyName("mchid")]
					public string MerchantId { get; set; }

					[JsonPropertyName("organization_id")]
					public string OrganizationId { get; set; }

					[JsonPropertyName("user_id")]
					public string UserCode { get; set; }

					[JsonPropertyName("appid")]
					public string AppId { get; set; }

					[JsonPropertyName("openid")]
					public string OpenId { get; set; }

					[JsonPropertyName("contract_state")]
					public ContractStatus Status { get; set; }

					[JsonPropertyName("contract_signed_time")]
					public DateTime SignedTime { get; set; }

					[JsonPropertyName("contract_terminated_time")]
					public DateTime TerminatedTime { get; set; }

					[JsonPropertyName("contract_mode")]
					public ContractMode ContractMode { get; set; }

					[JsonPropertyName("contract_bank_card_from")]
					public string BankCardFrom { get; set; }
				}

				public class SignatoryInfo
				{
					[JsonPropertyName("contract_id")]
					public string ContractId { get; set; }

					[JsonPropertyName("user_id")]
					public string UserCode { get; set; }

					[JsonPropertyName("out_user_id")]
					public string UserId { get; set; }

					[JsonPropertyName("user_name")]
					public string Name { get; set; }

					[JsonPropertyName("user_type")]
					public string Type { get; set; }

					[JsonPropertyName("status")]
					public SignatoryStatus Status { get; set; }

					[JsonPropertyName("contract_state")]
					public ContractStatus ContractStatus { get; set; }

					[JsonPropertyName("organization_id")]
					public string OrganizationId { get; set; }

					[JsonPropertyName("face_image_ok")]
					public bool HasFaceImage { get; set; }

					[JsonPropertyName("student_info")]
					public StudentInfo Student { get; set; }

					[JsonPropertyName("staff_info")]
					public StaffInfo Staff { get; set; }
				}

				[JsonConverter(typeof(SignatoryStatusConverter))]
				public enum SignatoryStatus
				{
					Normal,
					Disabled,
				}

				[JsonConverter(typeof(ContractStatusConverter))]
				public enum ContractStatus
				{
					None,
					Signed,
					Aborted,
					Unknown,
				}

				[JsonConverter(typeof(ContractModeConverter))]
				public enum ContractMode
				{
					None,
					Priority,
					Specially,
				}

				public struct StudentInfo
				{
					[JsonPropertyName("class_name")]
					public string ClassName { get; set; }
				}

				public struct StaffInfo
				{
					[JsonPropertyName("occupation")]
					public string Occupation { get; set; }
				}

				private class SignatoryStatusConverter : JsonConverter<SignatoryStatus>
				{
					public override SignatoryStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
					{
						if(reader.TokenType == JsonTokenType.String)
						{
							return reader.GetString() switch
							{
								"NORMAL" => SignatoryStatus.Normal,
								"DISABLED" => SignatoryStatus.Disabled,
								_ => SignatoryStatus.Normal,
							};
						}

						return SignatoryStatus.Normal;
					}

					public override void Write(Utf8JsonWriter writer, SignatoryStatus value, JsonSerializerOptions options)
					{
						switch(value)
						{
							case SignatoryStatus.Normal:
								writer.WriteStringValue("NORMAL");
								break;
							case SignatoryStatus.Disabled:
								writer.WriteStringValue("DISABLED");
								break;
						}
					}
				}

				private class ContractStatusConverter : JsonConverter<ContractStatus>
				{
					public override ContractStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
					{
						if(reader.TokenType == JsonTokenType.String)
						{
							return reader.GetString() switch
							{
								"NOT_CONTRACTED" => ContractStatus.None,
								"CONTRACTED" => ContractStatus.Signed,
								"TERMINATED" => ContractStatus.Aborted,
								_ => ContractStatus.None,
							};
						}

						return ContractStatus.None;
					}

					public override void Write(Utf8JsonWriter writer, ContractStatus value, JsonSerializerOptions options)
					{
						switch(value)
						{
							case ContractStatus.None:
								writer.WriteStringValue("NOT_CONTRACTED");
								break;
							case ContractStatus.Signed:
								writer.WriteStringValue("CONTRACTED");
								break;
							case ContractStatus.Aborted:
								writer.WriteStringValue("TERMINATED");
								break;
						}
					}
				}

				private class ContractModeConverter : JsonConverter<ContractMode>
				{
					public override ContractMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
					{
						if(reader.TokenType == JsonTokenType.String)
						{
							return reader.GetString() switch
							{
								"LIMIT_NONE" => ContractMode.None,
								"PRIORITY_BANK_CARD" => ContractMode.Priority,
								"LIMIT_BANK_CARD" => ContractMode.Specially,
								_ => ContractMode.None,
							};
						}

						return ContractMode.None;
					}

					public override void Write(Utf8JsonWriter writer, ContractMode value, JsonSerializerOptions options)
					{
						switch(value)
						{
							case ContractMode.None:
								writer.WriteStringValue("LIMIT_NONE");
								break;
							case ContractMode.Priority:
								writer.WriteStringValue("PRIORITY_BANK_CARD");
								break;
							case ContractMode.Specially:
								writer.WriteStringValue("LIMIT_BANK_CARD");
								break;
						}
					}
				}
				#endregion
			}
			#endregion
		}
	}
}
