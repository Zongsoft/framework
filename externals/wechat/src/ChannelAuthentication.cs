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

			var credentialId = Randomizer.GenerateString(16);
			return OperationResult.Success(new Credential(credentialId, result.Value.Identifier, TimeSpan.FromSeconds(result.Value.Period), result.Value.Permission));
		}
		#endregion

		#region 嵌套结构
		public readonly struct Credential
		{
			public Credential(string credentialId, string identifier, TimeSpan period, string permission)
			{
				this.CredentialId = string.IsNullOrEmpty(credentialId) ? Randomizer.GenerateString(16) : credentialId;
				this.Identifier = identifier;
				this.Period = period;
				this.Permission = permission;
			}

			public readonly string CredentialId { get; }
			public readonly string Identifier { get; }
			public readonly TimeSpan Period { get; }
			public readonly string Permission { get; }
		}

		private struct AuthenticationResult
		{
			[JsonPropertyName("access_token")]
			[Serialization.SerializationMember("access_token")]
			public string Token;

			[JsonPropertyName("refresh_token")]
			[Serialization.SerializationMember("refresh_token")]
			public string RenewalToken;

			[JsonPropertyName("expires_in")]
			[Serialization.SerializationMember("expires_in")]
			public int Period;

			[JsonPropertyName("openid")]
			[Serialization.SerializationMember("openid")]
			public string Identifier;

			[JsonPropertyName("scope")]
			[Serialization.SerializationMember("scope")]
			public string Permission;
		}
		#endregion
	}
}
