/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

using Zongsoft.Common;
using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Services.Distributing;

namespace Zongsoft.Externals.Wechat;

public static partial class CredentialManager
{
	#region 成员字段
	private static readonly HttpClient _http = new(new SocketsHttpHandler { AllowAutoRedirect = false, PooledConnectionLifetime = TimeSpan.FromMinutes(5) })
	{
		BaseAddress = new Uri("https://api.weixin.qq.com/"),
		Timeout = TimeSpan.FromSeconds(30),
	};
	private static readonly Dictionary<string, Gate> _gates = new(StringComparer.Ordinal);
	private static IDistributedCache _cache;
	private static IDistributedLockManager _locker;
	#endregion

	#region 公共属性
	public static IDistributedCache Cache
	{
		get => _cache ??= ApplicationContext.Current?.Services.Resolve<IServiceProvider<IDistributedCache>>()?.GetService(GetCacheName());
		set => _cache = value;
	}

	public static IDistributedLockManager Locker
	{
		get => _locker ??= ApplicationContext.Current?.Services.Resolve<IServiceProvider<IDistributedLockManager>>()?.GetService(GetCacheName());
		set => _locker = value;
	}
	#endregion

	#region 公共方法
	public static ValueTask<string> GetCredentialAsync(this Account account, bool refresh, CancellationToken cancellation = default) => GetCredentialAsync(account, 0, refresh, cancellation);

	internal static async ValueTask<string> GetCredentialAsync(this Account account, int permission, bool refresh, CancellationToken cancellation = default) =>
		account.Component == null ? (await GetAsync(account, null, refresh, cancellation)).Value : await GetAuthorizerCredentialAsync(account, permission, refresh, cancellation);

	public static async ValueTask<(string ticket, TimeSpan period)> GetTicketAsync(this Account account, string type, bool refresh, CancellationToken cancellation = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(type);
		if(type is not ("jsapi" or "wx_card") || account.Type != AccountType.Channel)
			throw new ArgumentException(null, nameof(type));

		var token = account.Component == null ? await GetAsync(account, type, refresh, cancellation) : await GetAuthorizerTicketAsync(account, type, refresh, cancellation);
		return (token.Value, token.Remaining);
	}
	#endregion

	#region 私有方法
	private static async ValueTask<Token> GetAsync(Account account, string type, bool refresh, CancellationToken cancellation)
	{
		if(account.IsEmpty || account.Type is not (AccountType.Applet or AccountType.Channel))
			throw new ArgumentException(null, nameof(account));

		var key = $"Zongsoft.Wechat.Self:{account.Type}:{Uri.EscapeDataString(account.Code)}:{(type == null ? "stable-token" : "ticket:" + type)}";
		return await GetAsync(key, refresh, token => AcquireAsync(account, type, refresh, token), cancellation);
	}

	private static async ValueTask<Token> GetAsync(string key, bool refresh, Func<CancellationToken, ValueTask<Token>> acquire, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		var cache = Cache ?? throw new InvalidOperationException();
		var previous = await ReadAsync(cache, key, cancellation);

		if(!refresh && previous != null)
			return previous;

		var gate = EnterGate(key);
		var entered = false;

		try
		{
			await gate.Semaphore.WaitAsync(cancellation);
			entered = true;
			var manager = Locker ?? throw new InvalidOperationException();

			for(var attempt = 0; attempt < 60; attempt++)
			{
				var current = await ReadAsync(cache, key, cancellation);
				if(current != null && (!refresh || current.Revision != previous?.Revision))
					return current;

				await using(var lease = await manager.AcquireAsync(key + ":lock", new DistributedLockOptions(TimeSpan.FromSeconds(60)) { RenewalInterval = TimeSpan.FromSeconds(20) }, cancellation))
				{
					if(lease?.IsLocked == true)
					{
						current = await ReadAsync(cache, key, cancellation);
						if(current != null && (!refresh || current.Revision != previous?.Revision))
							return current;

						var token = await acquire(cancellation);
						if(!lease.IsLocked)
							throw new OperationException("CREDENTIAL_LOCK_LOST", null);

						var period = token.Remaining;
						if(period <= TimeSpan.Zero || !await cache.SetValueAsync(key, JsonSerializer.Serialize(token, Json.Options), period, CacheRequisite.Always, cancellation))
							throw new OperationException("CREDENTIAL_STORE_FAILED", null);

						return token;
					}
				}

				await Task.Delay(TimeSpan.FromMilliseconds(500), cancellation);
			}

			throw new TimeoutException();
		}
		finally
		{
			if(entered)
				gate.Semaphore.Release();

			ExitGate(key, gate);
		}
	}

	private static async ValueTask<Token> ReadAsync(IDistributedCache cache, string key, CancellationToken cancellation)
	{
		var started = DateTime.UtcNow;
		var (value, expiry) = await cache.GetValueExpiryAsync<string>(key, cancellation);
		if(string.IsNullOrEmpty(value) || !expiry.HasValue || expiry.Value <= TimeSpan.Zero)
			return null;

		var token = JsonSerializer.Deserialize<Token>(value, Json.Options);
		if(token == null || string.IsNullOrEmpty(token.Value) || string.IsNullOrEmpty(token.Revision))
			throw new FormatException();

		token.Expiration = started.Add(expiry.Value);
		return token.Remaining > TimeSpan.Zero ? token : null;
	}

	private static Gate EnterGate(string key)
	{
		lock(_gates)
		{
			if(!_gates.TryGetValue(key, out var gate))
				_gates.Add(key, gate = new Gate());

			gate.Users++;
			return gate;
		}
	}

	private static void ExitGate(string key, Gate gate)
	{
		lock(_gates)
		{
			if(--gate.Users == 0)
			{
				_gates.Remove(key);
				gate.Semaphore.Dispose();
			}
		}
	}

	private static string GetCacheName() => Utility.GetOptions<Options.CachingOptions>("/Externals/Wechat/Caching")?.Name;
	#endregion

	#region 嵌套类型
	private sealed class Gate
	{
		public readonly SemaphoreSlim Semaphore = new(1, 1);
		public int Users;
	}

	private sealed class Token
	{
		public string Value { get; set; }
		public string Revision { get; set; }
		[JsonIgnore]
		public DateTime Expiration { get; set; }
		[JsonIgnore]
		public TimeSpan Remaining => this.Expiration - DateTime.UtcNow;
	}
	#endregion
}
