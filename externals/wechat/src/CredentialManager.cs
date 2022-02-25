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

using Zongsoft.Common;
using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Distributing;

namespace Zongsoft.Externals.Wechat
{
	public static class CredentialManager
	{
		#region 成员字段
		private static readonly HttpClient _http;
		private static readonly Dictionary<string, Token> _localCache;

		private static ICache _cache;
		private static IDistributedLockManager _locker;
		#endregion

		#region 静态函数
		static CredentialManager()
		{
			_http = new HttpClient();
			_http.BaseAddress = new Uri("https://api.weixin.qq.com");
			_http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			_http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Zongsoft.Externals.Wechat", "1.0"));

			_localCache = new Dictionary<string, Token>();
		}
		#endregion

		#region 公共属性
		public static ICache Cache
		{
			get => _cache ??= ApplicationContext.Current.Services.Resolve<IServiceProvider<ICache>>()?.GetService(GetCacheName());
			set => _cache = value;
		}

		public static IDistributedLockManager Locker
		{
			get => _locker ??= ApplicationContext.Current.Services.Resolve<IServiceProvider<IDistributedLockManager>>()?.GetService(GetCacheName());
			set => _locker = value;
		}
		#endregion

		#region 内部属性
		internal static HttpClient Http { get => _http; }
		#endregion

		#region 公共方法
		public static async ValueTask<string> GetCredentialAsync(this Account account, bool refresh, CancellationToken cancellation = default)
		{
			if(account.IsEmpty)
				throw new ArgumentNullException(nameof(account));

			var key = GetCredentalKey(account.Code);

			//首先从本地内存缓存中获取凭证标记，如果获取成功并且凭证未过期则返回该凭证号
			if(!refresh && _localCache.TryGetValue(key, out var token) && !token.IsExpired)
				return token.Key;

			var cache = Cache;

			if(cache == null)
			{
				Zongsoft.Diagnostics.Logger.Error("Missing the required distributed cache in the credential manager.");
				return null;
			}

			//从外部缓存中获取凭证标记
			if(!refresh)
			{
				(var credentialId, var expiry) = await cache.GetValueExpiryAsync<string>(key, cancellation);

				if(!string.IsNullOrEmpty(credentialId) && expiry > TimeSpan.Zero)
				{
					_localCache[key] = new CredentialToken(credentialId, expiry.Value);
					return credentialId;
				}
			}

			//获取分布式锁
			using var locker = await Locker.AcquireAsync(key + ":LOCKER", TimeSpan.FromSeconds(5), cancellation);

			if(locker != null)
			{
				var result = await AcquireCredentialAsync(account.Code, account.Secret);

				if(result.Succeed)
				{
					token = result.Value;

					if(cache.SetValue(key, token.Key, token.Expiry.GetPeriod()))
						_localCache[key] = token;

					return token.Key;
				}

				Zongsoft.Diagnostics.Logger.Error(result.Failure.Message, result.Failure);
			}
			else
			{
				//尝试再次从外部缓存获取凭证
				for(int i = 0; i < 3; i++)
				{
					await Task.Delay(Math.Min(3000, 500 * (i + 1)), cancellation);
					var (credentialId, expiry) = await cache.GetValueExpiryAsync<string>(key, cancellation);

					if(!string.IsNullOrEmpty(credentialId) && expiry > TimeSpan.Zero)
					{
						_localCache.TryAdd(key, new CredentialToken(credentialId, expiry.Value));
						return credentialId;
					}
				}

				Zongsoft.Diagnostics.Logger.Error("Attempts to acquires credential of the Wechat failed.");
			}

			return null;
		}

		public static async ValueTask<(string ticket, TimeSpan period)> GetTicketAsync(this Account account, string type, bool refresh, CancellationToken cancellation = default)
		{
			if(account.IsEmpty)
				throw new ArgumentNullException(nameof(account));

			var key = GetTicketKey(account.Code);

			//首先从本地内存缓存中获取票据标记，如果获取成功并且票据未过期则返回该票据号
			if(!refresh && _localCache.TryGetValue(key, out var token) && !token.IsExpired)
				return (token.Key, token.Expiry.GetPeriod());

			var cache = Cache;

			if(cache == null)
			{
				Zongsoft.Diagnostics.Logger.Error("Missing the required distributed cache in the credential manager.");
				return default;
			}

			if(!refresh)
			{
				//从外部缓存中获取票据标记
				(var ticketId, var expiry) = await cache.GetValueExpiryAsync<string>(key, cancellation);

				if(!string.IsNullOrEmpty(ticketId) && expiry > TimeSpan.Zero)
				{
					_localCache[key] = new TicketToken(ticketId, expiry.Value);
					return (ticketId, expiry.Value);
				}
			}

			//获取分布式锁
			using var locker = await Locker.AcquireAsync(key + ":LOCKER", TimeSpan.FromSeconds(5), cancellation);

			if(locker != null)
			{
				var credential = await GetCredentialAsync(account, refresh, cancellation);

				if(string.IsNullOrEmpty(credential))
					return default;

				var result = await AcquireTicketAsync(credential, type);

				if(result.Succeed)
				{
					token = result.Value;

					if(cache.SetValue(key, token.Key, token.Expiry.GetPeriod()))
						_localCache[key] = token;

					return (token.Key, token.Expiry.GetPeriod());
				}

				Zongsoft.Diagnostics.Logger.Error(result.Failure.Message, result.Failure);
			}
			else
			{
				//尝试再次从外部缓存票据标记
				for(int i = 0; i < 3; i++)
				{
					await Task.Delay(Math.Min(3000, 500 * (i + 1)), cancellation);
					var (ticketId, expiry) = await cache.GetValueExpiryAsync<string>(key, cancellation);

					if(!string.IsNullOrEmpty(ticketId) && expiry > TimeSpan.Zero)
					{
						_localCache[key] = new TicketToken(ticketId, expiry.Value);
						return (ticketId, expiry.Value);
					}
				}

				Zongsoft.Diagnostics.Logger.Error("Attempts to acquires ticket of the Wechat failed.");
			}

			return (null, TimeSpan.Zero);
		}
		#endregion

		#region 私有方法
		private static async ValueTask<OperationResult<Token>> AcquireCredentialAsync(string appId, string secret, int retries = 3)
		{
			var response = await _http.GetAsync($"/cgi-bin/token?grant_type=client_credential&appid={appId}&secret={secret}");
			var result = await response.GetResultAsync<CredentialToken>();

			if(result.Succeed)
				return OperationResult.Success((Token)result.Value);

			if(result.Failure.Code == ErrorCodes.Busy && retries > 0)
			{
				await Task.Delay(Math.Max(500, Zongsoft.Common.Randomizer.GenerateInt32() % 2500));
				return await AcquireCredentialAsync(appId, secret, retries - 1);
			}

			return (OperationResult)result.Failure;
		}

		private static async ValueTask<OperationResult<Token>> AcquireTicketAsync(string credentialId, string type, int retries = 3)
		{
			if(string.IsNullOrEmpty(credentialId))
				return OperationResult.Fail("MissingCredential", $"Missing the access token of the Wechat.");

			var response = await _http.GetAsync($"/cgi-bin/ticket/getticket?access_token={credentialId}&type={type}");
			var result = await response.GetResultAsync<TicketToken>();

			if(result.Succeed)
				return OperationResult.Success((Token)result.Value);

			if(result.Failure.Code == ErrorCodes.Busy && retries > 0)
			{
				await Task.Delay(Math.Max(500, Zongsoft.Common.Randomizer.GenerateInt32() % 2500));
				return await AcquireTicketAsync(credentialId, type, retries - 1);
			}

			return (OperationResult)result.Failure;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetCredentalKey(string appId) => $"Zongsoft.Wechat.Credential:{appId}";

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetTicketKey(string appId) => $"Zongsoft.Wechat.Ticket:{appId}";

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetCacheName() => Utility.GetOptions<Options.CachingOptions>("/Externals/Wechat/Caching")?.Name;
		#endregion

		#region 嵌套子类
		private abstract class Token
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

		private class CredentialToken : Token
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

		private class TicketToken : Token
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
