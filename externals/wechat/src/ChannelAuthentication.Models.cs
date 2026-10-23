using System;
using System.Text.Json.Serialization;

namespace Zongsoft.Externals.Wechat;

public partial class ChannelAuthentication
{
	#region 凭据结构
	public readonly struct Credential
	{
		public Credential(string appId, string accessToken, string renewalToken, string openId, string unionId, DateTimeOffset expiration, string permission, UserInfo user, string componentId = null)
		{
			this.AppId = appId;
			this.ComponentId = componentId;
			this.AccessToken = accessToken;
			this.RenewalToken = renewalToken;
			this.OpenId = openId;
			this.UnionId = unionId;
			this.Expiration = expiration;
			this.Permission = permission;
			this.User = user;
		}

		public string AppId { get; }
		public string ComponentId { get; }
		[JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public string AccessToken { get; }
		[JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public string RenewalToken { get; }
		public string OpenId { get; }
		public string UnionId { get; }
		public DateTimeOffset Expiration { get; }
		public TimeSpan Period
		{
			get
			{
				var remaining = this.Expiration - DateTimeOffset.UtcNow;
				return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
			}
		}
		public string Permission { get; }
		public UserInfo User { get; }
	}
	#endregion

	#region 协议结构
	private struct AuthenticationResult
	{
		[JsonPropertyName("access_token")]
		public string AccessToken { get; set; }
		[JsonPropertyName("refresh_token")]
		public string RenewalToken { get; set; }
		[JsonPropertyName("expires_in")]
		public int Period { get; set; }
		[JsonPropertyName("openid")]
		public string OpenId { get; set; }
		[JsonPropertyName("unionid")]
		public string UnionId { get; set; }
		[JsonPropertyName("scope")]
		public string Permission { get; set; }
	}

	private struct UserInfoResult
	{
		#region 公共属性
		[JsonPropertyName("openid")]
		[Serialization.SerializationMember("openid")]
		public string OpenId { get; set; }

		[JsonPropertyName("unionid")]
		[Serialization.SerializationMember("unionid")]
		public string UnionId { get; set; }

		[JsonPropertyName("nickname")]
		[Serialization.SerializationMember("nickname")]
		public string Nickname { get; set; }

		[JsonPropertyName("language")]
		[Serialization.SerializationMember("language")]
		public string Language { get; set; }

		[JsonPropertyName("country")]
		[Serialization.SerializationMember("country")]
		public string Country { get; set; }

		[JsonPropertyName("province")]
		[Serialization.SerializationMember("province")]
		public string Province { get; set; }

		[JsonPropertyName("city")]
		[Serialization.SerializationMember("city")]
		public string City { get; set; }

		[JsonPropertyName("headimgurl")]
		[Serialization.SerializationMember("headimgurl")]
		public string Avatar { get; set; }

		[JsonPropertyName("subscribe_time")]
		[Serialization.SerializationMember("subscribe_time")]
		public long SubscribedTime { get; set; }

		[JsonPropertyName("remark")]
		[Serialization.SerializationMember("remark")]
		public string Description { get; set; }

		[JsonPropertyName("privilege")]
		[Serialization.SerializationMember("privilege")]
		public string[] Privileges { get; set; }
		#endregion

		#region 公共方法
		public UserInfo ToInfo(string openId, string unionId = null)
		{
			return new UserInfo()
			{
				OpenId = string.IsNullOrEmpty(openId) ? this.OpenId : openId,
				UnionId = string.IsNullOrEmpty(unionId) ? this.UnionId : unionId,
				Nickname = this.Nickname,
				Language = this.Language,
				Country = this.Country,
				Province = this.Province,
				City = this.City,
				Avatar = this.Avatar,
				SubscribedTime = this.SubscribedTime,
				Description = this.Description,
				Privileges = this.Privileges,
			};
		}
		#endregion
	}

	public struct UserInfo
	{
		public string OpenId { get; init; }
		public string UnionId { get; init; }
		public string Nickname { get; init; }
		public string Language { get; init; }
		public string Avatar { get; init; }
		public string Country { get; init; }
		public string Province { get; init; }
		public string City { get; init; }
		public string Description { get; init; }
		public long SubscribedTime { get; init; }
		public string[] Privileges { get; init; }
	}
	#endregion
}
