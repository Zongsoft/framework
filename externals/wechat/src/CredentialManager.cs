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
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Distributing;

namespace Zongsoft.Externals.Wechat
{
	public class CredentialManager
	{
		#region 成员字段
		private HttpClient _http;
		private readonly Dictionary<string, Token> _localCache;
		#endregion

		#region 构造函数
		public CredentialManager()
		{
			_http = new HttpClient();
			_http.BaseAddress = new Uri("https://api.weixin.qq.com");
			_http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			_http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Zongsoft.Externals.Wechat", "1.0"));

			_localCache = new Dictionary<string, Token>();
		}
		#endregion

		#region 公共属性
		public HttpClient Http { get => _http; }
		public ICache Cache { get; set; }
		public IDistributedLockManager Locker { get; set; }
		#endregion

		#region 公共方法
		public async ValueTask<string> GetCredentialAsync(Applet applet, CancellationToken cancellation = default)
		{
			if(applet.IsEmpty)
				throw new ArgumentNullException(nameof(applet));

			var key = GetCredentalKey(applet.Code);

			//首先从本地内存缓存中获取凭证标记，如果获取成功并且凭证未过期则返回该凭证号
			if(_localCache.TryGetValue(key, out var token) && !token.IsExpired)
				return token.Key;

			//从外部缓存中获取凭证标记
			(var credentialId, var expiry) = await this.Cache.GetValueExpiryAsync<string>(key, cancellation);

			if(!string.IsNullOrEmpty(credentialId) && expiry > TimeSpan.Zero)
			{
				_localCache[key] = new CredentialToken(credentialId, expiry.Value);
				return credentialId;
			}

			//获取分布式锁
			using var locker = await this.Locker.AcquireAsync(key, TimeSpan.FromSeconds(5), cancellation);

			if(locker != null)
			{
				token = await this.AcquireCredentialAsync(applet.Code, applet.Secret);

				if(this.Cache.SetValue(key, token.Key, token.Expiry.GetPeriod()))
					_localCache[key] = token;

				return token.Key;
			}
			else
			{
				//尝试再次从外部缓存获取凭证
				for(int i = 0; i < 3; i++)
				{
					await Task.Delay(Math.Min(3000, 500 * (i + 1)), cancellation);
					(credentialId, expiry) = await this.Cache.GetValueExpiryAsync<string>(key, cancellation);

					if(!string.IsNullOrEmpty(credentialId) && expiry > TimeSpan.Zero)
					{
						_localCache[key] = new CredentialToken(credentialId, expiry.Value);
						return credentialId;
					}
				}
			}

			return null;
		}

		public async ValueTask<(string ticket, TimeSpan period)> GetTicketAsync(Applet applet, string type = "jsapi", CancellationToken cancellation = default)
		{
			if(applet.IsEmpty)
				throw new ArgumentNullException(nameof(applet));

			var key = GetTicketKey(applet.Code);

			//首先从本地内存缓存中获取票据标记，如果获取成功并且票据未过期则返回该票据号
			if(_localCache.TryGetValue(key, out var token) && !token.IsExpired)
				return (token.Key, token.Expiry.GetPeriod());

			//从外部缓存中获取票据标记
			(var ticketId, var expiry) = await this.Cache.GetValueExpiryAsync<string>(key, cancellation);

			if(!string.IsNullOrEmpty(ticketId) && expiry > TimeSpan.Zero)
			{
				_localCache[key] = new TicketToken(ticketId, expiry.Value);
				return (ticketId, expiry.Value);
			}

			//获取分布式锁
			using var locker = await this.Locker.AcquireAsync(key, TimeSpan.FromSeconds(5), cancellation);

			if(locker != null)
			{
				token = await this.AcquireTicketAsync(await this.GetCredentialAsync(applet, cancellation), type);

				if(this.Cache.SetValue(key, token.Key, token.Expiry.GetPeriod()))
					_localCache[key] = token;

				return (token.Key, token.Expiry.GetPeriod());
			}
			else
			{
				//尝试再次从外部缓存获取凭证
				for(int i = 0; i < 3; i++)
				{
					await Task.Delay(Math.Min(3000, 500 * (i + 1)), cancellation);
					(ticketId, expiry) = await this.Cache.GetValueExpiryAsync<string>(key, cancellation);

					if(!string.IsNullOrEmpty(ticketId) && expiry > TimeSpan.Zero)
					{
						_localCache[key] = new TicketToken(ticketId, expiry.Value);
						return (ticketId, expiry.Value);
					}
				}
			}

			return (null, TimeSpan.Zero);
		}
		#endregion

		#region 私有方法
		private async ValueTask<Token> AcquireCredentialAsync(string appId, string secret, int retries = 3)
		{
			var response = await _http.GetAsync($"/cgi-bin/token?grant_type=client_credential&appid={appId}&secret={secret}");
			var result = await response.GetResultAsync<CredentialToken>();

			if(result.Succeed)
				return result.Value;

			if(result.Failure.Code == ErrorCodes.Busy && retries > 0)
			{
				await Task.Delay(Math.Max(500, Zongsoft.Common.Randomizer.GenerateInt32() % 2500));
				return await this.AcquireCredentialAsync(appId, secret, retries - 1);
			}

			throw new WechatException(result.Failure.Code, result.Failure.Message);
		}

		private async ValueTask<Token> AcquireTicketAsync(string credentialId, string type, int retries = 3)
		{
			var response = await _http.GetAsync($"/cgi-bin/ticket/getticket?access_token={credentialId}&type={type}");
			var result = await response.GetResultAsync<TicketToken>();

			if(result.Succeed)
				return result.Value;

			if(result.Failure.Code == ErrorCodes.Busy && retries > 0)
			{
				await Task.Delay(Math.Max(500, Zongsoft.Common.Randomizer.GenerateInt32() % 2500));
				return await this.AcquireTicketAsync(credentialId, type, retries - 1);
			}

			throw new WechatException(result.Failure.Code, result.Failure.Message);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetCredentalKey(string appId) => $"Zongsoft.Wechat.Credential:{appId}";

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetTicketKey(string appId) => $"Zongsoft.Wechat.Ticket:{appId}";
		#endregion

		#region 嵌套子类
		public abstract class Token
		{
			#region 私有变量
			private TimeSpan _period;
			private DateTime _expiry;
			#endregion

			#region 构造函数
			protected Token() { }
			#endregion

			#region 公共属性
			[Zongsoft.Serialization.SerializationMember("expires_in")]
			[JsonPropertyName("expires_in")]
			[JsonConverter(typeof(TimeSpanConverter))]
			public TimeSpan Period
			{
				get => _period;
				set
				{
					//如果有效期超过1分钟，则对其9折处理（以确保该凭证不会被过期使用）
					_period = value.TotalSeconds < 60 ? value : new TimeSpan((long)(value.Ticks * 0.9));
					_expiry = DateTime.UtcNow.Add(_period);
				}
			}

			public DateTime Expiry { get => _expiry; }

			[JsonIgnore]
			[Zongsoft.Serialization.SerializationMember(Ignored = true)]
			public bool IsValid
			{
				get => this.Key != null && this.Key.Length > 0;
			}

			[JsonIgnore]
			[Zongsoft.Serialization.SerializationMember(Ignored = true)]
			public bool IsExpired
			{
				get => DateTime.UtcNow > _expiry;
			}
			#endregion

			#region 抽象属性
			[Zongsoft.Serialization.SerializationMember(Ignored = true)]
			[JsonIgnore]
			internal protected abstract string Key { get; }
			#endregion

			#region 重写方法
			public override string ToString() => $"{this.Key}@{_expiry}";
			#endregion

			#region 类型转换
			private class TimeSpanConverter : JsonConverter<TimeSpan>
			{
				public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				{
					var value = reader.GetInt32();
					return TimeSpan.FromSeconds(value);
				}

				public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
				{
					writer.WriteStringValue(value.TotalSeconds.ToString());
				}
			}
			#endregion
		}

		public class CredentialToken : Token
		{
			#region 构造函数
			public CredentialToken(string credentialId, TimeSpan period)
			{
				this.CredentialId = credentialId;
				this.Period = period;
			}
			#endregion

			#region 公共属性
			[Zongsoft.Serialization.SerializationMember("access_token")]
			[JsonPropertyName("access_token")]
			public string CredentialId { get; set; }
			#endregion

			#region 重写属性
			internal protected override string Key { get => this.CredentialId; }
			#endregion
		}

		public class TicketToken : Token
		{
			#region 构造函数
			public TicketToken(string ticketId, TimeSpan period)
			{
				this.TicketId = ticketId;
				this.Period = period;
			}
			#endregion

			#region 公共属性
			[Zongsoft.Serialization.SerializationMember("ticket")]
			[JsonPropertyName("ticket")]
			public string TicketId { get; set; }
			#endregion

			#region 重写属性
			internal protected override string Key { get => this.TicketId; }
			#endregion
		}
		#endregion
	}
}
