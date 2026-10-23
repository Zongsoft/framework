using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	public partial class MerchantService
	{
		#region 嵌套子类
		public partial class ContractService
		{
			#region 成员字段
			private readonly MerchantService _merchant;
			#endregion

			#region 构造函数
			internal ContractService(MerchantService merchant) => _merchant = merchant ?? throw new ArgumentNullException(nameof(merchant));
			#endregion

			#region 公共方法
			public async ValueTask<SignatoryInfo> GetSignatoryAsync(string organizationId, string userId, CancellationToken cancellation = default)
			{
				ValidateIdentifier(organizationId, 50, nameof(organizationId), true);
				ValidateIdentifier(userId, 50, nameof(userId), true);

				var result = await _merchant.Client.GetAsync<SignatoryInfo>($"offlinefacemch/organizations/{Uri.EscapeDataString(organizationId)}/users/out-user-id/{Uri.EscapeDataString(userId)}", cancellation);
				if(result == null || result.OrganizationId != organizationId || result.UserId != userId || string.IsNullOrWhiteSpace(result.UserCode) ||
					result.ContractStatus is ContractStatus.Signed or ContractStatus.Aborted && string.IsNullOrWhiteSpace(result.ContractId))
					throw new FormatException();

				return result;
			}

			public async ValueTask<ContractInfo> GetContractAsync(string contractId, string appId = null, CancellationToken cancellation = default)
			{
				ValidateIdentifier(contractId, 64, nameof(contractId));
				appId = string.IsNullOrEmpty(appId) ? _merchant.Authority.Accounts.Default.Code : appId;
				if(!string.IsNullOrEmpty(appId) && _merchant.Authority.Accounts.Default.Code != appId && !_merchant.Authority.Accounts.Any(account => account.Code == appId))
					throw new ArgumentException(null, nameof(appId));

				var query = string.IsNullOrEmpty(appId) ? string.Empty : $"?appid={Uri.EscapeDataString(appId)}";
				var result = await _merchant.Client.GetAsync<ContractInfo>($"offlineface/contracts/{Uri.EscapeDataString(contractId)}{query}", cancellation);
				if(result == null || result.ContractId != contractId || result.MerchantId != _merchant.Authority.Code ||
					string.IsNullOrWhiteSpace(result.OrganizationId) || string.IsNullOrWhiteSpace(result.UserCode) ||
					!string.IsNullOrEmpty(result.AppId) && result.AppId != appId)
					throw new FormatException();

				return result;
			}

			public ValueTask<string> AuthenticateAsync(string organizationId, string userId, CancellationToken cancellation = default) => this.AuthenticateAsync(organizationId, userId, null, cancellation);
			public async ValueTask<string> AuthenticateAsync(string organizationId, string userId, string scene = null, CancellationToken cancellation = default)
			{
				ValidateIdentifier(organizationId, 50, nameof(organizationId), true);
				ValidateIdentifier(userId, 50, nameof(userId), true);

				if(!string.IsNullOrEmpty(scene) && scene != "WEBSESSION")
					throw new ArgumentException(null, nameof(scene));

				var result = await _merchant.Client.PostAsync<AuthenticateRequest, AuthenticateResult>("offlinefacemch/tokens", new(organizationId, userId, "WEBSESSION"), cancellation);
				if(string.IsNullOrWhiteSpace(result.Token))
					throw new FormatException();

				return result.Token;
			}

			public async ValueTask<string> ApplyAsync(ApplyRequest request, CancellationToken cancellation = default)
			{
				ArgumentNullException.ThrowIfNull(request);
				ValidateIdentifier(request.Payer.OrganizationId, 50, nameof(request), true);
				ValidateIdentifier(request.Payer.UserId, 50, nameof(request), true);

				if(request.BusinessName is not ("K12" or "GROUP_DINING") || request.ContractMode.HasValue && !Enum.IsDefined(request.ContractMode.Value) ||
					request.ContractMode == ContractMode.Specially && !request.BankCard.HasValue)
					throw new ArgumentException(null, nameof(request));

				if(request.BankCard.HasValue && (string.IsNullOrWhiteSpace(request.BankCard.Value.Code) || string.IsNullOrWhiteSpace(request.BankCard.Value.Name) ||
					request.BankCard.Value.Identity.Type != ApplyRequest.IdentityType.IdentityId || string.IsNullOrWhiteSpace(request.BankCard.Value.Identity.Code)))
					throw new ArgumentException(null, nameof(request));
				if(request.Payer.Identity.HasValue && (!Enum.IsDefined(request.Payer.Identity.Value.Type) || string.IsNullOrWhiteSpace(request.Payer.Identity.Value.Code)))
					throw new ArgumentException(null, nameof(request));

				var certificate = await _merchant.Authority.GetCertificateAsync(cancellation);
				var result = await _merchant.Client.PostAsync<ApplyRequest, ApplyResult>("offlineface/contracts/presign", request, certificate, cancellation);
				if(string.IsNullOrWhiteSpace(result.Token))
					throw new FormatException();

				return result.Token;
			}

			public ValueTask RevokeAsync(string organizationId, string userId, CancellationToken cancellation = default)
			{
				ValidateIdentifier(organizationId, 50, nameof(organizationId), true);
				ValidateIdentifier(userId, 64, nameof(userId));
				return _merchant.Client.PostAsync($"offlinefacemch/organizations/{Uri.EscapeDataString(organizationId)}/users/user-id/{Uri.EscapeDataString(userId)}/terminate-contract", cancellation);
			}
			#endregion

			#region 私有方法
			private static void ValidateIdentifier(string value, int length, string parameter, bool alphanumeric = false)
			{
				if(string.IsNullOrWhiteSpace(value) || Encoding.UTF8.GetByteCount(value) > length || value.Any(character => char.IsControl(character) || alphanumeric && !char.IsAsciiLetterOrDigit(character)))
					throw new ArgumentException(null, parameter);
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
					public Identity? Identity { get; set; }
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
				public ContractStatus Status { get; set; } = ContractStatus.Unknown;

				[JsonPropertyName("contract_signed_time")]
				[JsonConverter(typeof(Json.DateConverter))]
				public DateTime? SignedTime { get; set; }

				[JsonPropertyName("contract_terminated_time")]
				[JsonConverter(typeof(Json.DateConverter))]
				public DateTime? TerminatedTime { get; set; }

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
				public SignatoryStatus Status { get; set; } = SignatoryStatus.Unknown;

				[JsonPropertyName("contract_state")]
				public ContractStatus ContractStatus { get; set; } = ContractStatus.Unknown;

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
				Unknown = -1,
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
							_ => SignatoryStatus.Unknown,
						};
					}

					return SignatoryStatus.Unknown;
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
						default:
							writer.WriteStringValue("UNKNOWN");
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
							_ => ContractStatus.Unknown,
						};
					}

					return ContractStatus.Unknown;
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
						default:
							writer.WriteStringValue("UNKNOWN");
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
