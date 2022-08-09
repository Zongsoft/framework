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
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat
{
	public class Applet
	{
		#region 成员字段
		private volatile UserProvider _users;
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

		public UserProvider Users
		{
			get
			{
				if(_users == null)
					Interlocked.CompareExchange(ref _users, new UserProvider(this.Account), null);

				return _users;
			}
		}
		#endregion

		#region 获取凭证
		public ValueTask<string> GetCredentialAsync(bool refresh, CancellationToken cancellation = default) => CredentialManager.GetCredentialAsync(this.Account, refresh, cancellation);
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

		#region 手机号码
		public async ValueTask<OperationResult<string>> GetPhoneNumberAsync(string token, CancellationToken cancellation = default)
		{
			var credential = await CredentialManager.GetCredentialAsync(this.Account, false, cancellation);
			//var response = await CredentialManager.Http.PostAsJsonAsync($"/wxa/business/getuserphonenumber?access_token={credential}", new { code = token }, cancellation);

			/* 注意：此API调用微信不兼容 HttpClient.PostAsJsonAsync(...) 方法，必须改为 StringContent 模式（具体原因不详）。 */
			var content = new StringContent(Zongsoft.Serialization.Serializer.Json.Serialize(new { code = token }), Encoding.UTF8, "application/json");
			var response = await CredentialManager.Http.PostAsync($"/wxa/business/getuserphonenumber?access_token={credential}", content, cancellation);
			var result = await response.GetResultAsync<PhoneInfoWrapper>(cancellation);

			return result.Succeed ?
				OperationResult.Success(result.Value.Phone.PhoneNumber) :
				(OperationResult)result.Failure;
		}
		#endregion

		#region 嵌套结构
		public readonly struct LoginResult
		{
			public LoginResult(string openId, string secret, string unionId)
			{
				this.OpenId = openId;
				this.Secret = secret;
				this.UnionId = unionId;
			}

			public string Secret { get; }
			public string OpenId { get; }
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

		private struct PhoneInfoWrapper
		{
			[JsonPropertyName("phone_info")]
			[Serialization.SerializationMember("phone_info")]
			public PhoneInfo Phone { get; set; }

			public struct PhoneInfo
			{
				[JsonPropertyName("phoneNumber")]
				[Serialization.SerializationMember("phoneNumber")]
				public string PhoneNumber { get; set; }

				[JsonPropertyName("countryCode")]
				[Serialization.SerializationMember("countryCode")]
				public string CountryCode { get; set; }
			}
		}
		#endregion
	}
}
