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
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat
{
	public class Applet
	{
		#region 静态变量
		private static readonly System.Security.Cryptography.SHA1 SHA1 = System.Security.Cryptography.SHA1.Create();
		#endregion

		#region 构造函数
		public Applet(Account account)
		{
			if(account.IsEmpty)
				throw new ArgumentNullException(nameof(account));

			this.Account = account;
		}
		#endregion

		#region 公共属性
		public Account Account { get; }
		#endregion

		#region 获取凭证
		public ValueTask<string> GetCredentialAsync(CancellationToken cancellation = default) => CredentialManager.GetCredentialAsync(this.Account, cancellation);
		#endregion

		#region 登录方法
		public async ValueTask<OperationResult<LoginResult>> LoginAsync(string token, CancellationToken cancellation = default)
		{
			var response = await CredentialManager.Http.GetAsync($"/sns/jscode2session?appid={this.Account.Code}&secret={this.Account.Secret}&js_code={token}&grant_type=authorization_code", cancellation);
			var result = await response.GetResultAsync<LoginResultWrapper>(cancellation);

			return result.Succeed ?
				OperationResult.Success(new LoginResult(result.Value.OpenId, result.Value.Secret, result.Value.UnionId)) :
				(OperationResult)result.Failure;
		}
		#endregion

		#region 嵌套结构
		public readonly struct LoginResult
		{
			public LoginResult(string identifier, string secret, string unionId)
			{
				this.Identifier = identifier;
				this.Secret = secret;
				this.UnionId = unionId;
			}

			public string Secret { get; }
			public string Identifier { get; }
			public string UnionId { get; }
		}

		private struct LoginResultWrapper
		{
			[JsonPropertyName("session_key")]
			[Serialization.SerializationMember("session_key")]
			public string Secret { get; set; }

			[JsonPropertyName("openid")]
			[Serialization.SerializationMember("openid")]
			public string OpenId { get; set; }

			[JsonPropertyName("unionid")]
			[Serialization.SerializationMember("unionid")]
			public string UnionId { get; set; }
		}
		#endregion
	}
}
