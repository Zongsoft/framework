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
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat
{
	public class ChannelAuthentication
	{
		#region 构造函数
		public ChannelAuthentication(Account account)
		{
			if(account.IsEmpty)
				throw new ArgumentNullException(nameof(account));

			this.Account = account;
		}
		#endregion

		#region 公共属性
		public Account Account { get; }
		#endregion

		#region 公共方法
		public async ValueTask<OperationResult<Credential>> AuthenticateAsync(string token, CancellationToken cancellation = default)
		{
			var response = await CredentialManager.Http.GetAsync($"/sns/oauth2/access_token?appid={this.Account.Code}&secret={this.Account.Secret}&code={token}&grant_type=authorization_code", cancellation);
			var result = await response.GetResultAsync<AuthenticationResult>(cancellation);

			if(result.Failed)
				return (OperationResult)result.Failure;

			var info = await GetUserInfo(result.Value.AccessToken, result.Value.OpenId, cancellation);
			return OperationResult.Success(result.Value.ToCredential(info.Succeed ? info.Value : default));
		}

		public static async ValueTask<OperationResult<UserInfo>> GetUserInfo(string token, string openId, CancellationToken cancellation = default)
		{
			var response = await CredentialManager.Http.GetAsync($"/sns/userinfo?access_token={token}&openid={openId}", cancellation);
			var result = await response.GetResultAsync<UserInfoResult>(cancellation);
			return result.Succeed ? OperationResult.Success(result.Value.ToInfo(openId)) : (OperationResult)result.Failure;
		}
		#endregion

		#region 嵌套结构
		public readonly struct Credential
		{
			public Credential(string credentialId, string accessToken, string openId, string unionId, TimeSpan period, string permission, in UserInfo user)
			{
				this.CredentialId = string.IsNullOrEmpty(credentialId) ? Randomizer.GenerateString(16) : credentialId;
				this.AccessToken = accessToken;
				this.OpenId = openId;
				this.UnionId = unionId;
				this.Period = period;
				this.Permission = permission;
				this.User = user;
			}

			public readonly string CredentialId { get; }
			public readonly string AccessToken { get; }
			public readonly string OpenId { get; }
			public readonly string UnionId { get; }
			public readonly TimeSpan Period { get; }
			public readonly string Permission { get; }
			public readonly UserInfo User { get; }
		}

		private struct AuthenticationResult
		{
			#region 公共属性
			[JsonPropertyName("access_token")]
			[Serialization.SerializationMember("access_token")]
			public string AccessToken { get; set; }

			[JsonPropertyName("refresh_token")]
			[Serialization.SerializationMember("refresh_token")]
			public string RenewalToken { get; set; }

			[JsonPropertyName("expires_in")]
			[Serialization.SerializationMember("expires_in")]
			public int Period { get; set; }

			[JsonPropertyName("openid")]
			[Serialization.SerializationMember("openid")]
			public string OpenId { get; set; }

			[JsonPropertyName("unionid")]
			[Serialization.SerializationMember("unionid")]
			public string UnionId { get; set; }

			[JsonPropertyName("scope")]
			[Serialization.SerializationMember("scope")]
			public string Permission { get; set; }
			#endregion

			#region 公共方法
			public Credential ToCredential(UserInfo user) => new
			(
				Randomizer.GenerateString(16),
				this.AccessToken,
				this.OpenId,
				this.UnionId,
				TimeSpan.FromSeconds(this.Period),
				this.Permission,
				user
			);
			#endregion
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
}
