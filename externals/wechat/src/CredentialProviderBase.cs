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
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Caching;
using Zongsoft.Services;

namespace Zongsoft.Externals.Wechat
{
	public abstract class CredentialProviderBase : ICredentialProvider
	{
		#region 成员字段
		private HttpClient _http;
		private readonly Dictionary<string, Token> _localCache;
		#endregion

		#region 构造函数
		public CredentialProviderBase()
		{
			_http = new HttpClient();
			_localCache = new Dictionary<string, Token>();
		}
		#endregion

		#region 公共属性
		public ICache Cache { get; set; }
		#endregion

		#region 公共方法
		public async Task<string> GetCredentialAsync(string appId)
		{
			if(string.IsNullOrEmpty(appId))
				throw new ArgumentNullException(nameof(appId));

			var key = GetCredentalKey(appId);

			//首先从本地内存缓存中获取凭证标记，如果获取成功并且凭证未过期则返回该凭证号
			if(_localCache.TryGetValue(key, out var token) && !token.IsExpired)
				return token.Key;

			var credentialId = this.Cache.GetValue<string>(key);

			if(string.IsNullOrEmpty(credentialId))
			{
				token = await this.GetRemoteCredentialAsync(appId, this.GetSecret(appId));

				if(this.Cache.SetValue(key, token.Key, token.Expiry.GetDuration()))
					_localCache[key] = token;

				return token.Key;
			}
			else
			{
				var expiry = this.Cache.GetExpiry(key);

				if(expiry.HasValue)
					_localCache[key] = new CredentialToken(credentialId, DateTime.UtcNow.Add(expiry.Value));

				return credentialId;
			}
		}

		public async Task<string> GetTicketAsync(string appId)
		{
			if(string.IsNullOrEmpty(appId))
				throw new ArgumentNullException(nameof(appId));

			var key = GetTicketKey(appId);

			//首先从本地内存缓存中获取票据标记，如果获取成功并且票据未过期则返回该票据号
			if(_localCache.TryGetValue(key, out var token) && !token.IsExpired)
				return token.Key;

			var ticketId = this.Cache.GetValue<string>(key);

			if(string.IsNullOrEmpty(ticketId))
			{
				token = await this.GetRemoteTicketAsync(await this.GetCredentialAsync(appId));

				if(this.Cache.SetValue(key, token.Key, token.Expiry.GetDuration()))
					_localCache[key] = token;

				return token.Key;
			}
			else
			{
				var expiry = this.Cache.GetExpiry(key);

				if(expiry.HasValue)
					_localCache[key] = new TicketToken(ticketId, DateTime.UtcNow.Add(expiry.Value));

				return ticketId;
			}
		}
		#endregion

		#region 抽象方法
		protected abstract string GetSecret(string appId);
		#endregion

		#region 私有方法
		private async Task<Token> GetRemoteCredentialAsync(string appId, string secret, int retries = 3)
		{
			var response = await _http.GetAsync(Urls.GetAccessToken("client_credential", appId, secret));
			var result = await response.GetResultAsync<CredentialToken>();

			if(result.Succeed)
				return result.Value;

			if(result.Failure.Code == ErrorCodes.Busy && retries > 0)
			{
				await Task.Delay(Math.Max(500, Zongsoft.Common.Randomizer.GenerateInt32() % 2500));
				return await this.GetRemoteCredentialAsync(appId, secret, retries - 1);
			}

			throw new WechatException(result.Failure.Code, result.Failure.Message);
		}

		private async Task<Token> GetRemoteTicketAsync(string credentialId, int retries = 3)
		{
			var response = await _http.GetAsync(Urls.GetTicketUrl(credentialId, "jsapi"));
			var result = await response.GetResultAsync<TicketToken>();

			if(result.Succeed)
				return result.Value;

			if(result.Failure.Code == ErrorCodes.Busy && retries > 0)
			{
				await Task.Delay(Math.Max(500, Zongsoft.Common.Randomizer.GenerateInt32() % 2500));
				return await this.GetRemoteTicketAsync(credentialId, retries - 1);
			}

			throw new WechatException(result.Failure.Code, result.Failure.Message);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private string GetCredentalKey(string appId)
		{
			return "Zongsoft.Wechat.Credential:" + appId.ToLowerInvariant();
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private string GetTicketKey(string appId)
		{
			return "Zongsoft.Wechat.Ticket:" + appId.ToLowerInvariant();
		}
		#endregion

		#region 嵌套子类
		public abstract class Token
		{
			#region 构造函数
			protected Token()
			{
			}
			#endregion

			#region 公共属性
			[Zongsoft.Serialization.SerializationMember("expires_in")]
			[System.Text.Json.Serialization.JsonPropertyName("expires_in")]
			[System.ComponentModel.TypeConverter(typeof(TimestampConverter))]
			public DateTime Expiry { get; set; }

			[Zongsoft.Serialization.SerializationMember(Ignored = true)]
			[System.Text.Json.Serialization.JsonIgnore]
			public bool IsValid
			{
				get => this.Key != null && this.Key.Length > 0;
			}

			[Zongsoft.Serialization.SerializationMember(Ignored = true)]
			[System.Text.Json.Serialization.JsonIgnore]
			public bool IsExpired
			{
				get => DateTime.UtcNow > this.Expiry;
			}
			#endregion

			#region 抽象属性
			[Zongsoft.Serialization.SerializationMember(Ignored = true)]
			[System.Text.Json.Serialization.JsonIgnore]
			internal protected abstract string Key { get; }
			#endregion

			#region 重写方法
			public override string ToString()
			{
				return this.Key + "@" + this.Expiry.ToLocalTime().ToString();
			}
			#endregion
		}

		public class CredentialToken : Token
		{
			#region 构造函数
			public CredentialToken(string credentialId, DateTime expiry)
			{
				this.CredentialId = credentialId;
				this.Expiry = expiry;
			}
			#endregion

			#region 公共属性
			[Zongsoft.Serialization.SerializationMember("access_token")]
			[System.Text.Json.Serialization.JsonPropertyName("access_token")]
			public string CredentialId { get; set; }
			#endregion

			#region 重写属性
			internal protected override string Key { get => this.CredentialId; }
			#endregion
		}

		public class TicketToken : Token
		{
			#region 构造函数
			public TicketToken(string ticketId, DateTime expiry)
			{
				this.TicketId = ticketId;
				this.Expiry = expiry;
			}
			#endregion

			#region 公共属性
			[Zongsoft.Serialization.SerializationMember("ticket")]
			[System.Text.Json.Serialization.JsonPropertyName("ticket")]
			public string TicketId { get; set; }
			#endregion

			#region 重写属性
			internal protected override string Key { get => this.TicketId; }
			#endregion
		}
		#endregion
	}
}
