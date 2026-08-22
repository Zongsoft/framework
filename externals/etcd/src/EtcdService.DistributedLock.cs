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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Etcd library.
 *
 * The Zongsoft.Externals.Etcd is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Etcd is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Etcd library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;

using Zongsoft.Services;
using Zongsoft.Services.Distributing;

namespace Zongsoft.Externals.Etcd;

[Service<IDistributedLock>(Tags = "etcd")]
partial class EtcdService : IDistributedLockManager
{
	public IDistributedLockTokenizer Tokenizer { get; set; }

	async ValueTask<TimeSpan?> IDistributedLockManager.GetExpiryAsync(string key, CancellationToken cancellation)
	{
		if(string.IsNullOrEmpty(key))
			return null;

		cancellation.ThrowIfCancellationRequested();
		var client = await this.ConnectAsync(cancellation);
		var response = await client.GetAsync(GetKey(key), null, null, cancellation);
		if(response.Kvs.Count == 0 || response.Kvs[0].Lease == 0)
			return null;

		var lease = await client.LeaseTimeToLiveAsync(new Etcdserverpb.LeaseTimeToLiveRequest
		{
			ID = response.Kvs[0].Lease,
			Keys = false,
		}, null, null, cancellation);
		return lease.TTL > 0 ? TimeSpan.FromSeconds(lease.TTL) : null;
	}

	public ValueTask<IDistributedLock> AcquireAsync(string key, TimeSpan expiry, CancellationToken cancellation = default) =>
		this.AcquireAsync(key, new DistributedLockOptions(expiry), cancellation);

	public async ValueTask<IDistributedLock> AcquireAsync(string key, DistributedLockOptions options, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));
		ArgumentNullException.ThrowIfNull(options);
		if(options.RenewalInterval < TimeSpan.Zero || options.RenewalInterval >= options.Expiry)
			throw new ArgumentOutOfRangeException(nameof(options));

		var tokenizer = this.Tokenizer ??= DistributedLockTokenizer.Random;
		var token = tokenizer.Tokenize();
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
		var client = await this.ConnectAsync(cancellation);
		var lease = await client.LeaseGrantAsync(new Etcdserverpb.LeaseGrantRequest { TTL = GetLeaseSeconds(expiry) }, null, null, cancellation);
		var lockKey = ByteString.CopyFromUtf8(GetKey(key));
		try
		{
			var response = await client.TransactionAsync(new Etcdserverpb.TxnRequest
			{
				Compare =
				{
					new Etcdserverpb.Compare
					{
						Key = lockKey,
						Target = Etcdserverpb.Compare.Types.CompareTarget.Create,
						Result = Etcdserverpb.Compare.Types.CompareResult.Equal,
						CreateRevision = 0,
					},
				},
				Success =
				{
					new Etcdserverpb.RequestOp
					{
						RequestPut = new Etcdserverpb.PutRequest
						{
							Key = lockKey,
							Value = ByteString.CopyFrom(token),
							Lease = lease.ID,
						},
					},
				},
			}, null, null, cancellation);

			if(response.Succeeded)
				return response.Header.Revision;
		}
		catch
		{
			await RevokeLeaseAsync(client, lease.ID);
			throw;
		}

		await RevokeLeaseAsync(client, lease.ID);
		return 0;
	}

	internal async ValueTask<bool> RenewAsync(string key, byte[] token, TimeSpan expiry, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		var client = await this.ConnectAsync(cancellation);
		var physicalKey = GetKey(key);
		var current = await client.GetAsync(physicalKey, null, null, cancellation);
		if(current.Kvs.Count == 0 || !current.Kvs[0].Value.Span.SequenceEqual(token))
			return false;

		var entry = current.Kvs[0];
		var lease = await client.LeaseGrantAsync(new Etcdserverpb.LeaseGrantRequest { TTL = GetLeaseSeconds(expiry) }, null, null, cancellation);
		var lockKey = ByteString.CopyFromUtf8(physicalKey);
		try
		{
			var response = await client.TransactionAsync(new Etcdserverpb.TxnRequest
			{
				Compare =
				{
					new Etcdserverpb.Compare
					{
						Key = lockKey,
						Target = Etcdserverpb.Compare.Types.CompareTarget.Mod,
						Result = Etcdserverpb.Compare.Types.CompareResult.Equal,
						ModRevision = entry.ModRevision,
					},
					new Etcdserverpb.Compare
					{
						Key = lockKey,
						Target = Etcdserverpb.Compare.Types.CompareTarget.Value,
						Result = Etcdserverpb.Compare.Types.CompareResult.Equal,
						Value = ByteString.CopyFrom(token),
					},
				},
				Success =
				{
					new Etcdserverpb.RequestOp
					{
						RequestPut = new Etcdserverpb.PutRequest { Key = lockKey, Value = ByteString.CopyFrom(token), Lease = lease.ID },
					},
				},
			}, null, null, cancellation);

			if(!response.Succeeded)
			{
				await RevokeLeaseAsync(client, lease.ID);
				return false;
			}
		}
		catch
		{
			await RevokeLeaseAsync(client, lease.ID);
			throw;
		}

		if(entry.Lease != 0)
			await RevokeLeaseAsync(client, entry.Lease);
		return true;
	}

	public async ValueTask<bool> ReleaseAsync(string key, byte[] token, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key) || token == null || token.Length == 0)
			return false;

		cancellation.ThrowIfCancellationRequested();
		var client = await this.ConnectAsync(cancellation);
		var physicalKey = GetKey(key);
		var current = await client.GetAsync(physicalKey, null, null, cancellation);
		if(current.Kvs.Count == 0 || !current.Kvs[0].Value.Span.SequenceEqual(token))
			return false;

		var entry = current.Kvs[0];
		var lockKey = ByteString.CopyFromUtf8(physicalKey);
		var response = await client.TransactionAsync(new Etcdserverpb.TxnRequest
		{
			Compare =
			{
				new Etcdserverpb.Compare
				{
					Key = lockKey,
					Target = Etcdserverpb.Compare.Types.CompareTarget.Value,
					Result = Etcdserverpb.Compare.Types.CompareResult.Equal,
					Value = ByteString.CopyFrom(token),
				},
			},
			Success =
			{
				new Etcdserverpb.RequestOp
				{
					RequestDeleteRange = new Etcdserverpb.DeleteRangeRequest { Key = lockKey },
				},
			},
		}, null, null, cancellation);

		if(!response.Succeeded)
			return false;
		if(entry.Lease != 0)
			await RevokeLeaseAsync(client, entry.Lease);
		return true;
	}

	private static long GetLeaseSeconds(TimeSpan expiry) => Math.Max(1L, checked((long)Math.Ceiling(expiry.TotalSeconds)));
	private static async ValueTask RevokeLeaseAsync(dotnet_etcd.EtcdClient client, long leaseId)
	{
		try { await client.LeaseRevokeAsync(new Etcdserverpb.LeaseRevokeRequest { ID = leaseId }); }
		catch { }
	}

	private sealed class DistributedLock : DistributedLockBase<EtcdService>
	{
		private readonly TimeSpan? _renewalInterval;
		private CancellationTokenSource _renewalCancellation;
		private Task _renewalTask;

		public DistributedLock(EtcdService service, string key, byte[] token, TimeSpan expiry, long fencingToken, TimeSpan? renewalInterval) : base(service, key, token, expiry, fencingToken > 0, fencingToken)
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
				catch(OperationCanceledException) when(source?.IsCancellationRequested == true) { }
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
			catch(OperationCanceledException) when(cancellation.IsCancellationRequested) { }
			catch(Exception exception)
			{
				this.Lose();
				Zongsoft.Diagnostics.Logging.GetLogging(typeof(DistributedLock)).Error(exception);
			}
		}
	}
}
