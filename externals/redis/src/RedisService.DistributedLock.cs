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
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services.Distributing;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis;

partial class RedisService : IDistributedLockManager
{
	#region 常量定义
	private const string RELEASE_SCRIPT = @"if redis.call('get', KEYS[1])==ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";
	private const string RENEW_SCRIPT = @"if redis.call('get', KEYS[1])==ARGV[1] then return redis.call('pexpire', KEYS[1],ARGV[2]) else return 0 end";
	private const string FENCE_SUFFIX = ":FENCE";
	#endregion

	#region 公共属性
	public IDistributedLockTokenizer Tokenizer { get; set; }
	#endregion

	#region 公共方法
	async ValueTask<TimeSpan?> IDistributedLockManager.GetExpiryAsync(string key, CancellationToken cancellation)
	{
		if(string.IsNullOrEmpty(key))
			return null;

		cancellation.ThrowIfCancellationRequested();

		//确保连接成功
		await this.ConnectAsync(cancellation);

		return await _database.KeyTimeToLiveAsync(GetKey(key), CommandFlags.None).WaitAsync(cancellation);
	}

	public async ValueTask<IDistributedLock> AcquireAsync(string key, TimeSpan expiry, CancellationToken cancellation = default)
	{
		return await this.AcquireAsync(key, new DistributedLockOptions(expiry), cancellation);
	}

	public async ValueTask<IDistributedLock> AcquireAsync(string key, DistributedLockOptions options, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));
		ArgumentNullException.ThrowIfNull(options);
		if(options.RenewalInterval < TimeSpan.Zero || options.RenewalInterval >= options.Expiry)
			throw new ArgumentOutOfRangeException(nameof(options), "The renewal interval must be positive and shorter than the lock expiry.");

		cancellation.ThrowIfCancellationRequested();

		//确保连接成功
		await this.ConnectAsync(cancellation);

		var tokenizer = this.Tokenizer ??= DistributedLockTokenizer.Random;
		var token = tokenizer.Tokenize();
		using var activity = RedisDiagnostics.ActivitySource.StartActivity("redis.lock.acquire", System.Diagnostics.ActivityKind.Client);
		var fencingToken = await this.AcquireAsync(key, token, options.Expiry, cancellation);

		return new DistributedLock(this, key, token, options.Expiry, fencingToken, options.RenewalInterval);
	}

	internal async ValueTask<long> AcquireAsync(string key, byte[] token, TimeSpan expiry, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));
		if(token == null || token.Length == 0)
			throw new ArgumentNullException(nameof(token));
		if(expiry <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(expiry));

		cancellation.ThrowIfCancellationRequested();

		//确保连接成功
		await this.ConnectAsync(cancellation);

		var lockKey = GetKey(key);
		if(!await _database.StringSetAsync(lockKey, token, expiry, When.NotExists, CommandFlags.None).WaitAsync(cancellation))
			return 0;

		try
		{
			return await _database.StringIncrementAsync(lockKey + FENCE_SUFFIX).WaitAsync(cancellation);
		}
		catch
		{
			await this.ReleaseAsync(key, token);
			throw;
		}
	}

	internal async ValueTask<bool> RenewAsync(string key, byte[] token, TimeSpan expiry, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		var milliseconds = checked((long)Math.Ceiling(expiry.TotalMilliseconds));
		var result = await _database.ScriptEvaluateAsync(RENEW_SCRIPT, [(RedisKey)GetKey(key)], [token, milliseconds]).WaitAsync(cancellation);
		return (long)result != 0;
	}

	public async ValueTask<bool> ReleaseAsync(string key, byte[] token, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key) || token == null || token.Length == 0)
			return false;

		cancellation.ThrowIfCancellationRequested();

		//确保连接成功
		await this.ConnectAsync(cancellation);

		var result = await _database.ScriptEvaluateAsync(RELEASE_SCRIPT, [(RedisKey)GetKey(key)], [token]).WaitAsync(cancellation);
		return ((int)result) != 0;
	}
	#endregion

	#region 嵌套子类
	private sealed class DistributedLock : DistributedLockBase<RedisService>
	{
		private readonly TimeSpan? _renewalInterval;
		private CancellationTokenSource _renewalCancellation;
		private Task _renewalTask;

		public DistributedLock(RedisService service, string key, byte[] token, TimeSpan expiry, long fencingToken, TimeSpan? renewalInterval) : base(service, key, token, expiry, fencingToken > 0, fencingToken)
		{
			_renewalInterval = renewalInterval > TimeSpan.Zero ? renewalInterval : null;
			this.StartRenewal();
		}

		protected override void OnEntered() => this.StartRenewal();
		protected override async ValueTask<bool> OnEnterAsync(CancellationToken cancellation)
		{
			var token = this.Manager == null ? 0 : await this.Manager.AcquireAsync(this.Key, this.Token, this.Expiry, cancellation);
			this.FencingToken = token;
			return token > 0;
		}

		protected override ValueTask<bool> OnRenewAsync(CancellationToken cancellation) =>
			this.Manager?.RenewAsync(this.Key, this.Token, this.Expiry, cancellation) ?? ValueTask.FromResult(false);

		protected override void Dispose(bool disposing)
		{
			if(disposing)
				this.StopRenewalAsync().AsTask().GetAwaiter().GetResult();
			base.Dispose(disposing);
		}

		protected override async ValueTask DisposeAsync(bool disposing)
		{
			if(disposing)
				await this.StopRenewalAsync();
			await base.DisposeAsync(disposing);
		}

		private void StartRenewal()
		{
			if(_renewalTask?.IsCompleted == true)
			{
				Interlocked.Exchange(ref _renewalTask, null);
				Interlocked.Exchange(ref _renewalCancellation, null)?.Dispose();
			}

			if(!_renewalInterval.HasValue || !this.IsLocked || _renewalTask != null)
				return;

			_renewalCancellation = new CancellationTokenSource();
			_renewalTask = this.RenewalLoopAsync(_renewalCancellation.Token);
		}

		private async ValueTask StopRenewalAsync()
		{
			var source = Interlocked.Exchange(ref _renewalCancellation, null);
			source?.Cancel();
			var task = Interlocked.Exchange(ref _renewalTask, null);
			if(task != null)
			{
				try { await task; }
				catch(OperationCanceledException) when (source?.IsCancellationRequested == true) { }
			}
			source?.Dispose();
		}

		private async Task RenewalLoopAsync(CancellationToken cancellation)
		{
			try
			{
				while(true)
				{
					await Task.Delay(_renewalInterval.Value, cancellation);
					if(!await this.RenewAsync(cancellation))
						return;
				}
			}
			catch(OperationCanceledException) when (cancellation.IsCancellationRequested)
			{
			}
			catch(Exception exception)
			{
				this.Lose();
				RedisDiagnostics.LockRenewalFailures.Add(1);
				Zongsoft.Diagnostics.Logging.GetLogging(typeof(DistributedLock)).Error(exception);
			}
		}
	}
	#endregion
}
