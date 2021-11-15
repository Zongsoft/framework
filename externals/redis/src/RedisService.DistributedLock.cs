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

using Zongsoft.Distributing;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis
{
   public partial class RedisService : IDistributedLockManager
	{
		#region 常量定义
		private const string RELEASE_SCRIPT = @"if redis.call('get', KEYS[1])==ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";
		#endregion

		#region 公共属性
		public IDistributedLockNormalizer Normalizer { get; set; }
		#endregion

		#region 公共方法
		public async ValueTask<IDistributedLock> AcquireAsync(string key, TimeSpan duration, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();

			//确保连接成功
			this.Connect();

			var normalizer = this.Normalizer ??= DistributedLockNormalizer.Randon;
			var token = normalizer.Normalize();

			return await _database.StringSetAsync(key, token, duration, When.NotExists) ?
				new DistributedLock(this, key, token.ToArray(), DateTime.UtcNow.Add(duration)) : null;
		}

		public async ValueTask<bool> ReleaseAsync(string key, ReadOnlyMemory<byte> token, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				return false;

			cancellation.ThrowIfCancellationRequested();

			//确保连接成功
			this.Connect();

			var result = await _database.ScriptEvaluateAsync(RELEASE_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { token });
			return ((int)result) != 0;
		}
		#endregion
	}
}
