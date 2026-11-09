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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;

using Zongsoft.Common;

namespace Zongsoft.Externals.Etcd;

partial class EtcdService : ISequence
{
	#region 公共方法
	public long Decrease(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null) => this.Increase(key, -interval, seed, expiry);
	public double Decrease(string key, double interval, double seed = 0, TimeSpan? expiry = null) => this.Increase(key, -interval, seed, expiry);
	public ValueTask<long> DecreaseAsync(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) => this.IncreaseAsync(key, -interval, seed, expiry, cancellation);
	public ValueTask<double> DecreaseAsync(string key, double interval, double seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) => this.IncreaseAsync(key, -interval, seed, expiry, cancellation);

	public long Increase(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null) => throw new NotSupportedException(Properties.Resources.Operation_AsyncOnly_Message);

	public double Increase(string key, double interval, double seed = 0, TimeSpan? expiry = null) => throw new NotSupportedException(Properties.Resources.Operation_AsyncOnly_Message);

	public ValueTask<long> IncreaseAsync(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) =>
		this.ChangeAsync(key, interval, seed, expiry, static value => long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture), static value => value.ToString(CultureInfo.InvariantCulture), cancellation);

	public ValueTask<double> IncreaseAsync(string key, double interval, double seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) =>
		this.ChangeAsync(key, interval, seed, expiry, static value => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture), static value => value.ToString("R", CultureInfo.InvariantCulture), cancellation);

	public void Reset(string key, int value = 0, TimeSpan? expiry = null) => throw new NotSupportedException(Properties.Resources.Operation_AsyncOnly_Message);
	public void Reset(string key, double value, TimeSpan? expiry = null) => throw new NotSupportedException(Properties.Resources.Operation_AsyncOnly_Message);
	public ValueTask ResetAsync(string key, int value = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) =>
		this.ResetAsync(key, value.ToString(CultureInfo.InvariantCulture), expiry, cancellation);
	public ValueTask ResetAsync(string key, double value, TimeSpan? expiry = null, CancellationToken cancellation = default) =>
		this.ResetAsync(key, value.ToString("R", CultureInfo.InvariantCulture), expiry, cancellation);
	#endregion

	#region 私有方法
	private async ValueTask<T> ChangeAsync<T>(string key, T interval, T seed, TimeSpan? expiry, Func<string, T> parser, Func<T, string> formatter, CancellationToken cancellation) where T : System.Numerics.INumber<T>
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		cancellation.ThrowIfCancellationRequested();
		var client = await this.ConnectAsync(cancellation);
		var physicalKey = this.GetKey(key);
		var keyBytes = ByteString.CopyFromUtf8(physicalKey);

		while(true)
		{
			cancellation.ThrowIfCancellationRequested();
			var current = await client.GetAsync(physicalKey, null, null, cancellation);
			var exists = current.Kvs.Count > 0;
			var value = exists ? parser(current.Kvs[0].Value.ToStringUtf8()) : seed;

			if(interval == T.Zero)
				return value;

			var result = checked(value + interval);
			long leaseId = 0;

			try
			{
				if(!exists && expiry > TimeSpan.Zero)
				{
					var lease = await client.LeaseGrantAsync(new Etcdserverpb.LeaseGrantRequest { TTL = GetLeaseSeconds(expiry.Value) }, null, null, cancellation);
					leaseId = lease.ID;
				}

				var compare = new Etcdserverpb.Compare
				{
					Key = keyBytes,
					Target = exists ? Etcdserverpb.Compare.Types.CompareTarget.Mod : Etcdserverpb.Compare.Types.CompareTarget.Create,
					Result = Etcdserverpb.Compare.Types.CompareResult.Equal,
				};
				if(exists)
					compare.ModRevision = current.Kvs[0].ModRevision;
				else
					compare.CreateRevision = 0;

				var response = await client.TransactionAsync(new Etcdserverpb.TxnRequest
				{
					Compare = { compare },
					Success =
					{
						new Etcdserverpb.RequestOp
						{
							RequestPut = new Etcdserverpb.PutRequest
							{
								Key = keyBytes,
								Value = ByteString.CopyFromUtf8(formatter(result)),
								Lease = exists ? current.Kvs[0].Lease : leaseId,
							},
						},
					},
				}, null, null, cancellation);

				if(response.Succeeded)
					return result;
			}
			catch
			{
				if(leaseId != 0)
					await RevokeLeaseAsync(client, leaseId);
				throw;
			}

			if(leaseId != 0)
				await RevokeLeaseAsync(client, leaseId);
		}
	}

	private async ValueTask ResetAsync(string key, string value, TimeSpan? expiry, CancellationToken cancellation)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		await this.SetValueAsync(key, value, expiry, cancellation);
	}
	#endregion
}
