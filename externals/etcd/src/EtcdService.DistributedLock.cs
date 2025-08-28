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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using dotnet_etcd;
using dotnet_etcd.interfaces;
using Google.Protobuf;

using Zongsoft.Services;
using Zongsoft.Services.Distributing;

namespace Zongsoft.Externals.Etcd;

[Service<IDistributedLock>(Tags = "etcd")]
partial class EtcdService : IDistributedLockManager
{
	#region 公共属性
	public IDistributedLockTokenizer Tokenizer { get; set; }
	#endregion

	#region 公共方法
	public async ValueTask<IDistributedLock> AcquireAsync(string key, TimeSpan expiry, CancellationToken cancellation = default)
	{
		var request = new Etcdserverpb.LeaseGrantRequest()
		{
			TTL = (long)expiry.TotalSeconds
		};

		var response = await _client.LeaseGrantAsync(request, null, null, cancellation);
		var req = new V3Lockpb.LockRequest()
		{
			Name = ByteString.CopyFromUtf8(key),
			Lease = response.ID
		};

		try
		{
			var res = await _client.LockAsync(req, null, null, cancellation);
			res.Key.ToStringUtf8();
			return new DistributedLock(this, key, res.ToByteArray(), expiry, true);
		}
		catch(Grpc.Core.RpcException ex) when(ex.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded)
		{
			throw;
		}
	}

	public async ValueTask<TimeSpan?> GetExpiryAsync(string key, CancellationToken cancellation = default)
	{
		var request = new Etcdserverpb.LeaseTimeToLiveRequest()
		{
			Keys = false,
		};

		var response = await _client.LeaseTimeToLiveAsync(new Etcdserverpb.LeaseTimeToLiveRequest(), null, null, cancellation);
		return response.TTL > 0 ? TimeSpan.FromSeconds(response.TTL) : null;
	}

	public async ValueTask<bool> ReleaseAsync(string key, byte[] token, CancellationToken cancellation = default)
	{
		var response = await _client.UnlockAsync(key, null, null, cancellation);
		return response != null;
	}
	#endregion

	#region 内部方法
	internal async ValueTask<bool> AcquireAsync(string key, byte[] token, TimeSpan expiry, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		cancellation.ThrowIfCancellationRequested();

		return true;
	}
	#endregion

	#region 嵌套子类
	private sealed class DistributedLock(EtcdService service, string key, byte[] token, TimeSpan expiry, bool isHeld) : DistributedLockBase<EtcdService>(service, key, token, expiry, isHeld)
	{
		protected override ValueTask<bool> OnEnterAsync(CancellationToken cancellation) => this.Manager?.AcquireAsync(this.Key, this.Token, this.Expiry, cancellation) ?? ValueTask.FromResult(false);
	}
	#endregion
}