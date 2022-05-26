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
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Caching;
using Zongsoft.Configuration;
using Zongsoft.Services;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis
{
	public partial class RedisService : ISequence
	{
		#region 常量定义
		private const string INCREMENT_SCRIPT = @"if redis.call('exists', KEYS[1])==0 then redis.call('set', KEYS[1], ARGV[2], 'NX') end return redis.call('incrby', KEYS[1], ARGV[1])";
		private const string DECREMENT_SCRIPT = @"if redis.call('exists', KEYS[1])==0 then redis.call('set', KEYS[1], ARGV[2], 'NX') end return redis.call('decrby', KEYS[1], ARGV[1])";

		private const string INCREMENT_EXPIRY_SCRIPT = @"if redis.call('exists', KEYS[1])==0 then redis.call('setex', KEYS[1], ARGV[3], ARGV[2]) end return redis.call('incrby', KEYS[1], ARGV[1])";
		private const string DECREMENT_EXPIRY_SCRIPT = @"if redis.call('exists', KEYS[1])==0 then redis.call('setex', KEYS[1], ARGV[3], ARGV[2]) end return redis.call('decrby', KEYS[1], ARGV[1])";
		#endregion

		#region 公共方法
		public long Decrease(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			//确保连接成功
			this.Connect();

			if(interval == 0)
				return (long)_database.StringGet(GetKey(key));

			if(expiry.HasValue && expiry.Value > TimeSpan.Zero)
				return (long)_database.ScriptEvaluate(DECREMENT_EXPIRY_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed, unchecked((int)expiry.Value.TotalSeconds) });

			if(seed == 0)
				return _database.StringDecrement(GetKey(key), interval);

			return (long)_database.ScriptEvaluate(DECREMENT_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed });
		}

		public double Decrease(string key, double interval, double seed = 0, TimeSpan? expiry = null)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			//确保连接成功
			this.Connect();

			if(interval == 0)
				return (double)_database.StringGet(GetKey(key));

			if(expiry.HasValue && expiry.Value > TimeSpan.Zero)
				return (double)_database.ScriptEvaluate(DECREMENT_EXPIRY_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed, unchecked((int)expiry.Value.TotalSeconds) });

			if(seed == 0)
				return _database.StringDecrement(GetKey(key), interval);

			return (double)_database.ScriptEvaluate(DECREMENT_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed });
		}

		public async Task<long> DecreaseAsync(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);

			if(interval == 0)
				return (long)_database.StringGet(GetKey(key));

			if(expiry.HasValue && expiry.Value > TimeSpan.Zero)
				return (long)await _database.ScriptEvaluateAsync(DECREMENT_EXPIRY_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed, unchecked((int)expiry.Value.TotalSeconds) });

			if(seed == 0)
				return await _database.StringDecrementAsync(GetKey(key), interval);

			return (long)await _database.ScriptEvaluateAsync(DECREMENT_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed });
		}

		public async Task<double> DecreaseAsync(string key, double interval, double seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);

			if(interval == 0)
				return (double)_database.StringGet(GetKey(key));

			if(expiry.HasValue && expiry.Value > TimeSpan.Zero)
				return (double)await _database.ScriptEvaluateAsync(DECREMENT_EXPIRY_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed, unchecked((int)expiry.Value.TotalSeconds) });

			if(seed == 0)
				return await _database.StringDecrementAsync(GetKey(key), interval);

			return (double)await _database.ScriptEvaluateAsync(DECREMENT_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed });
		}

		public long Increase(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			//确保连接成功
			this.Connect();

			if(interval == 0)
				return (long)_database.StringGet(GetKey(key));

			if(expiry.HasValue && expiry.Value > TimeSpan.Zero)
				return (long)_database.ScriptEvaluate(INCREMENT_EXPIRY_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed, unchecked((int)expiry.Value.TotalSeconds) });

			if(seed == 0)
				return _database.StringIncrement(GetKey(key), interval);

			return (long)_database.ScriptEvaluate(INCREMENT_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed });
		}

		public double Increase(string key, double interval, double seed = 0, TimeSpan? expiry = null)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			//确保连接成功
			this.Connect();

			if(interval == 0)
				return (double)_database.StringGet(GetKey(key));

			if(expiry.HasValue && expiry.Value > TimeSpan.Zero)
				return (double)_database.ScriptEvaluate(INCREMENT_EXPIRY_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed, unchecked((int)expiry.Value.TotalSeconds) });

			if(seed == 0)
				return _database.StringIncrement(GetKey(key), interval);

			return (double)_database.ScriptEvaluate(INCREMENT_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed });
		}

		public async Task<long> IncreaseAsync(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);

			if(interval == 0)
				return (long)_database.StringGet(GetKey(key));

			if(expiry.HasValue && expiry.Value > TimeSpan.Zero)
				return (long)await _database.ScriptEvaluateAsync(INCREMENT_EXPIRY_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed, unchecked((int)expiry.Value.TotalSeconds) });

			if(seed == 0)
				return await _database.StringIncrementAsync(GetKey(key), interval);

			return (long)await _database.ScriptEvaluateAsync(INCREMENT_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed });
		}

		public async Task<double> IncreaseAsync(string key, double interval, double seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);

			if(interval == 0)
				return (double)_database.StringGet(GetKey(key));

			if(expiry.HasValue && expiry.Value > TimeSpan.Zero)
				return (double)await _database.ScriptEvaluateAsync(INCREMENT_EXPIRY_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed, unchecked((int)expiry.Value.TotalSeconds) });

			if(seed == 0)
				return await _database.StringIncrementAsync(GetKey(key), interval);

			return (double)await _database.ScriptEvaluateAsync(INCREMENT_SCRIPT, new[] { (RedisKey)GetKey(key) }, new RedisValue[] { interval, seed });
		}

		void ISequence.Reset(string key, int value, TimeSpan? expiry)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			//确保连接成功
			this.Connect();

			_database.StringSet(GetKey(key), value, expiry, When.Exists, CommandFlags.None);
		}

		void ISequence.Reset(string key, double value, TimeSpan? expiry)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			//确保连接成功
			this.Connect();

			_database.StringSet(GetKey(key), value, expiry, When.Exists, CommandFlags.None);
		}

		async Task ISequence.ResetAsync(string key, int value, TimeSpan? expiry, CancellationToken cancellation)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);
			await _database.StringSetAsync(GetKey(key), value, expiry, When.Exists, CommandFlags.None);
		}

		async Task ISequence.ResetAsync(string key, double value, TimeSpan? expiry, CancellationToken cancellation)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);
			await _database.StringSetAsync(GetKey(key), value, expiry, When.Exists, CommandFlags.None);
		}
		#endregion

	}
}
