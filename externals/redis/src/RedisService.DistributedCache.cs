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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Caching;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis;

partial class RedisService : IDistributedCache
{
	#region 事件定义
	event EventHandler<DistributedCacheEventArgs> IDistributedCache.Expired
	{
		add => throw new NotImplementedException();
		remove => throw new NotImplementedException();
	}
	event EventHandler<DistributedCacheEventArgs> IDistributedCache.Removed
	{
		add => throw new NotImplementedException();
		remove => throw new NotImplementedException();
	}
	event EventHandler<DistributedCacheEventArgs> IDistributedCache.Updated
	{
		add => throw new NotImplementedException();
		remove => throw new NotImplementedException();
	}
	#endregion

	#region 普通方法
	public long GetCount()
	{
		//确保连接成功
		this.Connect();

		return _connection.GetServer(_database.IdentifyEndpoint()).DatabaseSize(_database.Database);
	}

	public async ValueTask<long> GetCountAsync(CancellationToken cancellation = default)
	{
		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		return await _connection.GetServer(_database.IdentifyEndpoint()).DatabaseSizeAsync(_database.Database);
	}

	public bool Exists(string key)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		return _database.KeyExists(GetKey(key));
	}

	public async ValueTask<bool> ExistsAsync(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		return await _database.KeyExistsAsync(GetKey(key));
	}

	public IEnumerable<string> Find(string pattern)
	{
		//确保连接成功
		this.Connect();

		return _connection.GetServer(_database.IdentifyEndpoint())
			.Scan(_database.Database, pattern)
			.Select(key => (string)key);
	}

	public async IAsyncEnumerable<string> FindAsync(string pattern, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellation = default)
	{
		//确保连接成功
		await this.ConnectAsync(cancellation);

		await foreach(var key in _connection.GetServer(_database.IdentifyEndpoint()).ScanAsync(_database.Database, pattern))
			yield return key;
	}
	#endregion

	#region 过期方法
	public TimeSpan? GetExpiry(string key)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		return _database.KeyTimeToLive(GetKey(key));
	}

	public async ValueTask<TimeSpan?> GetExpiryAsync(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		return await _database.KeyTimeToLiveAsync(GetKey(key));
	}

	public bool SetExpiry(string key, TimeSpan expiry)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		return _database.KeyExpire(GetKey(key), expiry);
	}

	public async ValueTask<bool> SetExpiryAsync(string key, TimeSpan expiry, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		return await _database.KeyExpireAsync(GetKey(key), expiry);
	}
	#endregion

	#region 删除方法
	public void Clear()
	{
		const int BATCH_SIZE = 100;

		//确保连接成功
		this.Connect();

		RedisKey[] keys;

		do
		{
			keys = _connection
				.GetServer(_database.IdentifyEndpoint())
				.Keys(_database.Database, GetKey("*"), BATCH_SIZE).ToArray();
		} while(keys.Length > 0 && _database.KeyDelete(keys) > 0);
	}

	public async ValueTask ClearAsync(CancellationToken cancellation = default)
	{
		const int BATCH_SIZE = 100;

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);

		RedisKey[] keys;

		do
		{
			keys = _connection
				.GetServer(_database.IdentifyEndpoint())
				.Keys(_database.Database, GetKey("*"), BATCH_SIZE).ToArray();
		} while(keys.Length > 0 && (await _database.KeyDeleteAsync(keys)) > 0);
	}

	public bool Remove(string key)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		return _database.KeyDelete(GetKey(key));
	}

	public bool Remove(string key, out object value)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		var result = _database.StringGetDelete(GetKey(key));
		value = result.HasValue ? result.GetValue<object>() : default;
		return result.HasValue;
	}

	public int Remove(IEnumerable<string> keys)
	{
		if(keys == null)
			return 0;

		//确保连接成功
		this.Connect();

		return (int)_database.KeyDelete(keys.Select(key => (RedisKey)GetKey(key)).ToArray());
	}

	public async ValueTask<bool> RemoveAsync(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		return await _database.KeyDeleteAsync(GetKey(key));
	}

	public async ValueTask<int> RemoveAsync(IEnumerable<string> keys, CancellationToken cancellation = default)
	{
		if(keys == null)
			return 0;

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		return (int)await _database.KeyDeleteAsync(keys.Select(key => (RedisKey)GetKey(key)).ToArray());
	}

	public bool Rename(string oldKey, string newKey)
	{
		if(string.IsNullOrEmpty(oldKey))
			throw new ArgumentNullException(nameof(oldKey));

		if(string.IsNullOrEmpty(newKey))
			throw new ArgumentNullException(nameof(newKey));

		//确保连接成功
		this.Connect();

		return _database.KeyRename(GetKey(oldKey), GetKey(newKey), When.Exists);
	}

	public async ValueTask<bool> RenameAsync(string oldKey, string newKey, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(oldKey))
			throw new ArgumentNullException(nameof(oldKey));

		if(string.IsNullOrEmpty(newKey))
			throw new ArgumentNullException(nameof(newKey));

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		return await _database.KeyRenameAsync(GetKey(oldKey), GetKey(newKey), When.Exists);
	}
	#endregion

	#region 读取方法
	public object GetValue(string key) => this.GetValue<object>(key);
	public T GetValue<T>(string key)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		if(typeof(T) == typeof(ISet<string>))
			return (T)(ISet<string>)new RedisHashset(_database, GetKey(key));
		if(typeof(T) == typeof(IDictionary<string, string>))
			return (T)(IDictionary<string, string>)new RedisDictionary(_database, GetKey(key));

		return _database.StringGet(GetKey(key)).GetValue<T>();
	}

	public object GetValue(string key, out TimeSpan? expiry) => GetValue<object>(key, out expiry);
	public T GetValue<T>(string key, out TimeSpan? expiry)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		if(typeof(T) == typeof(ISet<string>))
		{
			expiry = _database.KeyTimeToLive(GetKey(key));
			return (T)(ISet<string>)new RedisHashset(_database, GetKey(key));
		}

		if(typeof(T) == typeof(IDictionary<string, string>))
		{
			expiry = _database.KeyTimeToLive(GetKey(key));
			return (T)(IDictionary<string, string>)new RedisDictionary(_database, GetKey(key));
		}

		var result = _database.StringGetWithExpiry(GetKey(key));
		expiry = result.Expiry;
		return result.Value.GetValue<T>();
	}

	public ValueTask<object> GetValueAsync(string key, CancellationToken cancellation = default) => this.GetValueAsync<object>(key, cancellation);
	public async ValueTask<T> GetValueAsync<T>(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);

		if(typeof(T) == typeof(ISet<string>))
			return (T)(ISet<string>)new RedisHashset(_database, GetKey(key));
		if(typeof(T) == typeof(IDictionary<string, string>))
			return (T)(IDictionary<string, string>)new RedisDictionary(_database, GetKey(key));

		return (await _database.StringGetAsync(GetKey(key))).GetValue<T>();
	}

	public ValueTask<(object Value, TimeSpan? Expiry)> GetValueExpiryAsync(string key, CancellationToken cancellation = default) => this.GetValueExpiryAsync<object>(key, cancellation);
	public async ValueTask<(T Value, TimeSpan? Expiry)> GetValueExpiryAsync<T>(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);

		if(typeof(T) == typeof(ISet<string>))
			return ((T)(ISet<string>)new RedisHashset(_database, GetKey(key)), await _database.KeyTimeToLiveAsync(GetKey(key)));

		if(typeof(T) == typeof(IDictionary<string, string>))
			return ((T)(IDictionary<string, string>)new RedisDictionary(_database, GetKey(key)), await _database.KeyTimeToLiveAsync(GetKey(key)));

		var result = await _database.StringGetWithExpiryAsync(GetKey(key));
		return (result.Value.GetValue<T>(), result.Expiry);
	}

	public bool TryGetValue<T>(string key, out T value)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		if(typeof(T) == typeof(ISet<string>))
		{
			value = (T)(ISet<string>)new RedisHashset(_database, GetKey(key));
			return true;
		}

		if(typeof(T) == typeof(IDictionary<string, string>))
		{
			value = (T)(IDictionary<string, string>)new RedisDictionary(_database, GetKey(key));
			return true;
		}

		var result = _database.StringGet(GetKey(key));
		value = result.HasValue ? result.GetValue<T>() : default;
		return result.HasValue;
	}

	public bool TryGetValue<T>(string key, out T value, out TimeSpan? expiry)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		if(typeof(T) == typeof(ISet<string>))
		{
			value = (T)(ISet<string>)new RedisHashset(_database, GetKey(key));
			expiry = _database.KeyTimeToLive(GetKey(key));
			return true;
		}

		if(typeof(T) == typeof(IDictionary<string, string>))
		{
			value = (T)(IDictionary<string, string>)new RedisDictionary(_database, GetKey(key));
			expiry = _database.KeyTimeToLive(GetKey(key));
			return true;
		}

		var result = _database.StringGetWithExpiry(GetKey(key));
		value = result.Expiry.HasValue ? result.Value.GetValue<T>() : default;
		expiry = result.Expiry;
		return result.Value.HasValue;
	}

	public ValueTask<(bool result, object value)> TryGetValueAsync(string key, CancellationToken cancellation = default) => this.TryGetValueAsync<object>(key, cancellation);
	public async ValueTask<(bool result, T value)> TryGetValueAsync<T>(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);

		if(typeof(T) == typeof(ISet<string>))
		{
			var hashset = (T)(ISet<string>)new RedisHashset(_database, GetKey(key));
			return (true, hashset);
		}

		if(typeof(T) == typeof(IDictionary<string, string>))
		{
			var hashset = (T)(IDictionary<string, string>)new RedisDictionary(_database, GetKey(key));
			return (true, hashset);
		}

		var result = await _database.StringGetAsync(GetKey(key));
		return (result.HasValue, result.HasValue ? result.GetValue<T>() : default);
	}
	#endregion

	#region 设置方法
	public bool SetValue(string key, object value, CacheRequisite requisite = CacheRequisite.Always)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		return this.SetEntry(key, value, TimeSpan.Zero, requisite);
	}

	public bool SetValue(string key, object value, TimeSpan expiry, CacheRequisite requisite = CacheRequisite.Always)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		return this.SetEntry(key, value, expiry, requisite);
	}

	public async ValueTask<bool> SetValueAsync(string key, object value, CacheRequisite requisite = CacheRequisite.Always, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		return await this.SetEntryAsync(key, value, TimeSpan.Zero, requisite, cancellation);
	}

	public async ValueTask<bool> SetValueAsync(string key, object value, TimeSpan expiry, CacheRequisite requisite = CacheRequisite.Always, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		return await this.SetEntryAsync(key, value, expiry, requisite, cancellation);
	}
	#endregion
}
