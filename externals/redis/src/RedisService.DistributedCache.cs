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
using Zongsoft.Components;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis;

partial class RedisService : IDistributedCache
{
	#region 订阅方法
	public async ValueTask<IDistributedCacheSubscription> SubscribeAsync(IHandler<DistributedCacheNotification> handler, DistributedCacheSubscriptionOptions options = null, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(handler);
		DistributedCacheSubscription subscription = null;
		var snapshot = (options ?? DistributedCacheSubscriptionOptions.Default).Snapshot();

		await _gate.WaitAsync(cancellation);

		try
		{
			this.ThrowIfDisposed();
			await this.ConnectCoreAsync(-1, cancellation);

			var @namespace = string.IsNullOrEmpty(_namespace) ? string.Empty : _namespace + ":";
			while(true)
			{
				var hub = await DistributedCacheNotificationHub.GetAsync(_connection, _database.Database, @namespace, cancellation);
				subscription = new DistributedCacheSubscription(this, hub, handler, snapshot);
				if(await subscription.SubscribeAsync(cancellation))
					break;

				await subscription.DisposeAsync();
				subscription = null;
			}

			if(!_subscriptions.TryAdd(subscription, 0))
				throw new InvalidOperationException(Properties.Resources.CacheNotificationSubscriptionRegistrationFailed_Message);

			return subscription;
		}
		catch
		{
			if(subscription != null)
				await subscription.DisposeAsync();

			throw;
		}
		finally
		{
			_gate.Release();
		}
	}
	#endregion

	#region 普通方法
	public long GetCount()
	{
		//确保连接成功
		this.Connect();

		if(!string.IsNullOrEmpty(_namespace))
		{
			long count = 0;

			foreach(var key in this.ScanKeys(GetKeyPattern("*")))
				count++;

			return count;
		}

		long result = 0;

		foreach(var server in this.GetServers())
			result += server.DatabaseSize(_database.Database);

		return result;
	}

	public async ValueTask<long> GetCountAsync(CancellationToken cancellation = default)
	{
		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		if(!string.IsNullOrEmpty(_namespace))
		{
			long count = 0;

			await foreach(var key in this.ScanKeysAsync(GetKeyPattern("*"), cancellation))
				count++;

			return count;
		}

		long result = 0;

		foreach(var server in this.GetServers())
			result += await server.DatabaseSizeAsync(_database.Database).WaitAsync(cancellation);

		return result;
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
		return await _database.KeyExistsAsync(GetKey(key)).WaitAsync(cancellation);
	}

	public IEnumerable<string> Find(string pattern)
	{
		//确保连接成功
		this.Connect();

		return FindCore(string.IsNullOrEmpty(pattern) ? "*" : pattern);

		IEnumerable<string> FindCore(string pattern)
		{
			foreach(var key in this.ScanKeys(GetKeyPattern(pattern)))
				yield return this.GetLogicalKey(key);
		}
	}

	public async IAsyncEnumerable<string> FindAsync(string pattern, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellation = default)
	{
		//确保连接成功
		await this.ConnectAsync(cancellation);

		await foreach(var key in this.ScanKeysAsync(GetKeyPattern(string.IsNullOrEmpty(pattern) ? "*" : pattern), cancellation))
			yield return this.GetLogicalKey(key);
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
		return await _database.KeyTimeToLiveAsync(GetKey(key)).WaitAsync(cancellation);
	}

	public bool SetExpiry(string key, TimeSpan expiry)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));
		if(expiry < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(expiry));

		//确保连接成功
		this.Connect();

		return expiry == TimeSpan.Zero ? _database.KeyPersist(GetKey(key)) : _database.KeyExpire(GetKey(key), expiry);
	}

	public async ValueTask<bool> SetExpiryAsync(string key, TimeSpan expiry, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));
		if(expiry < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(expiry));

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		return expiry == TimeSpan.Zero ?
			await _database.KeyPersistAsync(GetKey(key)).WaitAsync(cancellation) :
			await _database.KeyExpireAsync(GetKey(key), expiry).WaitAsync(cancellation);
	}
	#endregion

	#region 删除方法
	public void Clear()
	{
		const int BATCH_SIZE = 100;

		//确保连接成功
		this.Connect();

		var keys = new List<RedisKey>(BATCH_SIZE);

		foreach(var key in this.ScanKeys(GetKeyPattern("*")))
		{
			keys.Add(key);

			if(keys.Count < BATCH_SIZE)
				continue;

			foreach(var item in keys)
				_database.KeyDelete(item);

			keys.Clear();
		}

		foreach(var key in keys)
			_database.KeyDelete(key);
	}

	public async ValueTask ClearAsync(CancellationToken cancellation = default)
	{
		const int BATCH_SIZE = 100;

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);

		var tasks = new List<Task<bool>>(BATCH_SIZE);

		await foreach(var key in this.ScanKeysAsync(GetKeyPattern("*"), cancellation))
		{
			tasks.Add(_database.KeyDeleteAsync(key));

			if(tasks.Count < BATCH_SIZE)
				continue;

			await Task.WhenAll(tasks).WaitAsync(cancellation);
			tasks.Clear();
		}

		if(tasks.Count > 0)
			await Task.WhenAll(tasks).WaitAsync(cancellation);
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

		var entries = keys.Where(key => !string.IsNullOrEmpty(key)).Select(key => (RedisKey)GetKey(key)).ToArray();
		var count = 0;

		foreach(var entry in entries)
		{
			if(_database.KeyDelete(entry))
				count++;
		}

		return count;
	}

	public async ValueTask<bool> RemoveAsync(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		return await _database.KeyDeleteAsync(GetKey(key)).WaitAsync(cancellation);
	}

	public async ValueTask<int> RemoveAsync(IEnumerable<string> keys, CancellationToken cancellation = default)
	{
		if(keys == null)
			return 0;

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);

		var entries = keys.Where(key => !string.IsNullOrEmpty(key)).Select(key => (RedisKey)GetKey(key)).ToArray();

		if(entries.Length == 0)
			return 0;

		var tasks = new Task<bool>[entries.Length];

		for(int i = 0; i < entries.Length; i++)
			tasks[i] = _database.KeyDeleteAsync(entries[i]);

		var results = await Task.WhenAll(tasks).WaitAsync(cancellation);
		var count = 0;

		for(int i = 0; i < results.Length; i++)
		{
			if(results[i])
				count++;
		}

		return count;
	}

	public bool Rename(string oldKey, string newKey)
	{
		if(string.IsNullOrEmpty(oldKey))
			throw new ArgumentNullException(nameof(oldKey));

		if(string.IsNullOrEmpty(newKey))
			throw new ArgumentNullException(nameof(newKey));

		//确保连接成功
		this.Connect();

		return _database.KeyRename(GetKey(oldKey), GetKey(newKey), When.Always);
	}

	public async ValueTask<bool> RenameAsync(string oldKey, string newKey, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(oldKey))
			throw new ArgumentNullException(nameof(oldKey));

		if(string.IsNullOrEmpty(newKey))
			throw new ArgumentNullException(nameof(newKey));

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		return await _database.KeyRenameAsync(GetKey(oldKey), GetKey(newKey), When.Always).WaitAsync(cancellation);
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

		var entryKey = GetKey(key);

		if(typeof(T) == typeof(object))
			return (T)this.GetEntry(key);
		if(IsStringList(typeof(T)))
			return _database.KeyExists(entryKey) ? GetStringList<T>(_database.ListRange(entryKey)) : default;

		if(typeof(T) == typeof(ISet<string>))
			return _database.KeyExists(entryKey) ? (T)(ISet<string>)new RedisHashset(_database, entryKey, this.GetKeyPrefix()) : default;
		if(typeof(T) == typeof(IDictionary<string, string>))
			return _database.KeyExists(entryKey) ? (T)(IDictionary<string, string>)new RedisDictionary(_database, entryKey) : default;

		return _database.StringGet(entryKey).GetValue<T>();
	}

	public object GetValue(string key, out TimeSpan? expiry) => GetValue<object>(key, out expiry);
	public T GetValue<T>(string key, out TimeSpan? expiry)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		var entryKey = GetKey(key);

		if(typeof(T) == typeof(object))
			return (T)this.GetEntry(key, out _, out expiry);
		if(IsStringList(typeof(T)))
		{
			if(!_database.KeyExists(entryKey))
			{
				expiry = null;
				return default;
			}

			expiry = _database.KeyTimeToLive(entryKey);
			return GetStringList<T>(_database.ListRange(entryKey));
		}

		if(typeof(T) == typeof(ISet<string>))
		{
			if(!_database.KeyExists(entryKey))
			{
				expiry = null;
				return default;
			}

			expiry = _database.KeyTimeToLive(entryKey);
			return (T)(ISet<string>)new RedisHashset(_database, entryKey, this.GetKeyPrefix());
		}

		if(typeof(T) == typeof(IDictionary<string, string>))
		{
			if(!_database.KeyExists(entryKey))
			{
				expiry = null;
				return default;
			}

			expiry = _database.KeyTimeToLive(entryKey);
			return (T)(IDictionary<string, string>)new RedisDictionary(_database, entryKey);
		}

		var result = _database.StringGetWithExpiry(entryKey);
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

		var entryKey = GetKey(key);

		if(typeof(T) == typeof(object))
		{
			var result = await this.GetEntryAsync(key, cancellation);
			return (T)result.value;
		}
		if(IsStringList(typeof(T)))
			return await _database.KeyExistsAsync(entryKey).WaitAsync(cancellation) ? GetStringList<T>(await _database.ListRangeAsync(entryKey).WaitAsync(cancellation)) : default;

		if(typeof(T) == typeof(ISet<string>))
			return await _database.KeyExistsAsync(entryKey).WaitAsync(cancellation) ? (T)(ISet<string>)new RedisHashset(_database, entryKey, this.GetKeyPrefix()) : default;
		if(typeof(T) == typeof(IDictionary<string, string>))
			return await _database.KeyExistsAsync(entryKey).WaitAsync(cancellation) ? (T)(IDictionary<string, string>)new RedisDictionary(_database, entryKey) : default;

		return (await _database.StringGetAsync(entryKey).WaitAsync(cancellation)).GetValue<T>();
	}

	public ValueTask<(object Value, TimeSpan? Expiry)> GetValueExpiryAsync(string key, CancellationToken cancellation = default) => this.GetValueExpiryAsync<object>(key, cancellation);
	public async ValueTask<(T Value, TimeSpan? Expiry)> GetValueExpiryAsync<T>(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);

		var entryKey = GetKey(key);

		if(typeof(T) == typeof(object))
		{
			var objectResult = await this.GetEntryAsync(key, cancellation);
			return ((T)objectResult.value, objectResult.expiry);
		}
		if(IsStringList(typeof(T)))
		{
			if(!await _database.KeyExistsAsync(entryKey).WaitAsync(cancellation))
				return (default, null);

			return (GetStringList<T>(await _database.ListRangeAsync(entryKey).WaitAsync(cancellation)), await _database.KeyTimeToLiveAsync(entryKey).WaitAsync(cancellation));
		}

		if(typeof(T) == typeof(ISet<string>))
		{
			if(!await _database.KeyExistsAsync(entryKey).WaitAsync(cancellation))
				return (default, null);

			return ((T)(ISet<string>)new RedisHashset(_database, entryKey, this.GetKeyPrefix()), await _database.KeyTimeToLiveAsync(entryKey).WaitAsync(cancellation));
		}

		if(typeof(T) == typeof(IDictionary<string, string>))
		{
			if(!await _database.KeyExistsAsync(entryKey).WaitAsync(cancellation))
				return (default, null);

			return ((T)(IDictionary<string, string>)new RedisDictionary(_database, entryKey), await _database.KeyTimeToLiveAsync(entryKey).WaitAsync(cancellation));
		}

		var result = await _database.StringGetWithExpiryAsync(entryKey).WaitAsync(cancellation);
		return (result.Value.GetValue<T>(), result.Expiry);
	}

	public bool TryGetValue<T>(string key, out T value)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		var entryKey = GetKey(key);

		if(typeof(T) == typeof(object))
		{
			var objectResult = this.GetEntry(key);
			value = (T)objectResult;
			return objectResult != null;
		}
		if(IsStringList(typeof(T)))
		{
			var exists = _database.KeyExists(entryKey);
			value = exists ? GetStringList<T>(_database.ListRange(entryKey)) : default;
			return exists;
		}

		if(typeof(T) == typeof(ISet<string>))
		{
			var exists = _database.KeyExists(entryKey);
			value = exists ? (T)(ISet<string>)new RedisHashset(_database, entryKey, this.GetKeyPrefix()) : default;
			return exists;
		}

		if(typeof(T) == typeof(IDictionary<string, string>))
		{
			var exists = _database.KeyExists(entryKey);
			value = exists ? (T)(IDictionary<string, string>)new RedisDictionary(_database, entryKey) : default;
			return exists;
		}

		var result = _database.StringGet(entryKey);
		value = result.HasValue ? result.GetValue<T>() : default;
		return result.HasValue;
	}

	public bool TryGetValue<T>(string key, out T value, out TimeSpan? expiry)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		var entryKey = GetKey(key);

		if(typeof(T) == typeof(object))
		{
			var objectResult = this.GetEntry(key, out _, out expiry);
			value = (T)objectResult;
			return objectResult != null;
		}
		if(IsStringList(typeof(T)))
		{
			var exists = _database.KeyExists(entryKey);
			value = exists ? GetStringList<T>(_database.ListRange(entryKey)) : default;
			expiry = exists ? _database.KeyTimeToLive(entryKey) : null;
			return exists;
		}

		if(typeof(T) == typeof(ISet<string>))
		{
			var exists = _database.KeyExists(entryKey);
			value = exists ? (T)(ISet<string>)new RedisHashset(_database, entryKey, this.GetKeyPrefix()) : default;
			expiry = exists ? _database.KeyTimeToLive(entryKey) : null;
			return exists;
		}

		if(typeof(T) == typeof(IDictionary<string, string>))
		{
			var exists = _database.KeyExists(entryKey);
			value = exists ? (T)(IDictionary<string, string>)new RedisDictionary(_database, entryKey) : default;
			expiry = exists ? _database.KeyTimeToLive(entryKey) : null;
			return exists;
		}

		var result = _database.StringGetWithExpiry(entryKey);
		value = result.Value.HasValue ? result.Value.GetValue<T>() : default;
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

		var entryKey = GetKey(key);

		if(typeof(T) == typeof(object))
		{
			var objectResult = await this.GetEntryAsync(key, cancellation);
			return (objectResult.value != null, (T)objectResult.value);
		}
		if(IsStringList(typeof(T)))
		{
			var exists = await _database.KeyExistsAsync(entryKey).WaitAsync(cancellation);
			return (exists, exists ? GetStringList<T>(await _database.ListRangeAsync(entryKey).WaitAsync(cancellation)) : default);
		}

		if(typeof(T) == typeof(ISet<string>))
		{
			var exists = await _database.KeyExistsAsync(entryKey).WaitAsync(cancellation);
			return (exists, exists ? (T)(ISet<string>)new RedisHashset(_database, entryKey, this.GetKeyPrefix()) : default);
		}

		if(typeof(T) == typeof(IDictionary<string, string>))
		{
			var exists = await _database.KeyExistsAsync(entryKey).WaitAsync(cancellation);
			return (exists, exists ? (T)(IDictionary<string, string>)new RedisDictionary(_database, entryKey) : default);
		}

		var result = await _database.StringGetAsync(entryKey).WaitAsync(cancellation);
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

	#region 私有方法
	private IEnumerable<IServer> GetServers()
	{
		foreach(var endpoint in _connection.GetEndPoints())
		{
			var server = _connection.GetServer(endpoint);

			if(server.IsConnected && !server.IsReplica)
				yield return server;
		}
	}

	private IEnumerable<RedisKey> ScanKeys(RedisValue pattern)
	{
		foreach(var server in this.GetServers())
		{
			foreach(var key in server.Keys(_database.Database, pattern))
				yield return key;
		}
	}

	private async IAsyncEnumerable<RedisKey> ScanKeysAsync(RedisValue pattern, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellation)
	{
		foreach(var server in this.GetServers())
		{
			await foreach(var key in server.KeysAsync(_database.Database, pattern).WithCancellation(cancellation))
				yield return key;
		}
	}

	private string GetLogicalKey(RedisKey key)
	{
		var text = (string)key;
		var prefix = this.GetKeyPrefix();

		return string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(text) || !text.StartsWith(prefix, StringComparison.Ordinal) ? text : text[prefix.Length..];
	}

	private static bool IsStringList(Type type) =>
		type == typeof(string[]) ||
		type == typeof(List<string>) ||
		type == typeof(IList<string>) ||
		type == typeof(ICollection<string>) ||
		type == typeof(IEnumerable<string>) ||
		type == typeof(IReadOnlyList<string>) ||
		type == typeof(IReadOnlyCollection<string>);

	private static T GetStringList<T>(RedisValue[] values)
	{
		var array = GetStringValues(values);
		return typeof(T) == typeof(string[]) ? (T)(object)array : (T)(object)new List<string>(array);
	}
	#endregion
}
