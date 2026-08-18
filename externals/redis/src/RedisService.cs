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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Common;
using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Configuration;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis;

public partial class RedisService : IDisposable, IAsyncDisposable
{
	#region 成员字段
	private readonly string _name;
	private string _namespace;
	private IConnectionSettings _settings;
	private ConfigurationOptions _options;

	private IDatabase _database;
	private volatile ConnectionMultiplexer _connection;
	private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
	private readonly SemaphoreSlim _subscriptionLock = new SemaphoreSlim(1, 1);
	private readonly ConcurrentDictionary<RedisCacheSubscription, byte> _subscriptions = new();
	private readonly object _disposeLock = new();
	private Task _disposeTask;
	private int _disposed;
	#endregion

	#region 构造函数
	public RedisService(string name)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		_name = name.Trim();
	}

	public RedisService(string name, IConnectionSettings settings)
	{
		if(string.IsNullOrWhiteSpace(name))
		{
			if(settings == null || string.IsNullOrEmpty(settings.Name))
				throw new ArgumentNullException(nameof(name));

			name = settings.Name;
		}

		_name = name.Trim();
		_settings = settings;
	}

	public RedisService(string name, string connectionString)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));
		if(string.IsNullOrWhiteSpace(connectionString))
			throw new ArgumentNullException(nameof(connectionString));

		_name = name.Trim();
		_settings = Configuration.RedisConnectionSettingsDriver.Instance.GetSettings(connectionString);
	}
	#endregion

	#region 公共属性
	public string Name => _name;
	public string Namespace
	{
		get => _namespace;
		set
		{
			var @namespace = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

			_subscriptionLock.Wait();
			try
			{
				this.ThrowIfDisposed();

				if(string.Equals(_namespace, @namespace, StringComparison.Ordinal))
					return;

				if(!_subscriptions.IsEmpty)
					throw new InvalidOperationException("The Redis cache namespace cannot be changed while notification subscriptions are active.");

				_namespace = @namespace;
			}
			finally
			{
				_subscriptionLock.Release();
			}
		}
	}

	public int DatabaseId => _database?.Database ?? -1;
	public IConnectionSettings Settings => _settings ??= ApplicationContext.Current?.Configuration.GetConnectionSettings("/Externals/Redis/ConnectionSettings", _name, "redis");
	public ConfigurationOptions Options => _options ??= this.Settings?.GetOptions<ConfigurationOptions>();
	#endregion

	#region 内部属性
	internal IServer Server
	{
		get
		{
			//确保连接成功
			this.Connect();
			return _connection.GetServer(_database.IdentifyEndpoint());
		}
	}

	internal IDatabase Database
	{
		get
		{
			if(_database == null)
				this.Connect();

			return _database;
		}
	}
	#endregion

	#region 公共方法
	public void Use(int databaseId)
	{
		if(databaseId < 0)
			throw new ArgumentOutOfRangeException(nameof(databaseId));

		_subscriptionLock.Wait();
		try
		{
			this.ThrowIfDisposed();

			if(_database?.Database == databaseId)
				return;

			if(!_subscriptions.IsEmpty)
				throw new InvalidOperationException("The Redis cache database cannot be changed while notification subscriptions are active.");

			if(_connection == null)
				this.Connect(databaseId);
			else
				_database = _connection.GetDatabase(databaseId);
		}
		finally
		{
			_subscriptionLock.Release();
		}
	}

	public async ValueTask UseAsync(int databaseId, CancellationToken cancellation = default)
	{
		if(databaseId < 0)
			throw new ArgumentOutOfRangeException(nameof(databaseId));

		await _subscriptionLock.WaitAsync(cancellation);
		try
		{
			this.ThrowIfDisposed();

			if(_database?.Database == databaseId)
				return;

			if(!_subscriptions.IsEmpty)
				throw new InvalidOperationException("The Redis cache database cannot be changed while notification subscriptions are active.");

			if(_connection == null)
				await this.ConnectAsync(databaseId, cancellation);
			else
				_database = _connection.GetDatabase(databaseId);
		}
		finally
		{
			_subscriptionLock.Release();
		}
	}

	public RedisServiceInfo GetInfo()
	{
		this.Connect();
		return this.GetInfoCore();
	}

	public async ValueTask<RedisServiceInfo> GetInfoAsync(CancellationToken cancellation = default)
	{
		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		return this.GetInfoCore();
	}

	public RedisEntryType GetEntryType(string key)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		this.Connect();

		return _database.KeyType(GetKey(key)) switch
		{
			RedisType.String => RedisEntryType.String,
			RedisType.Hash => RedisEntryType.Dictionary,
			RedisType.List => RedisEntryType.List,
			RedisType.Set => RedisEntryType.Set,
			RedisType.SortedSet => RedisEntryType.SortedSet,
			RedisType.Stream => RedisEntryType.Stream,
			_ => RedisEntryType.None,
		};
	}

	public async ValueTask<RedisEntryType> GetEntryTypeAsync(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		cancellation.ThrowIfCancellationRequested();

		await this.ConnectAsync(cancellation);

		return await _database.KeyTypeAsync(GetKey(key)).WaitAsync(cancellation) switch
		{
			RedisType.String => RedisEntryType.String,
			RedisType.Hash => RedisEntryType.Dictionary,
			RedisType.List => RedisEntryType.List,
			RedisType.Set => RedisEntryType.Set,
			RedisType.SortedSet => RedisEntryType.SortedSet,
			RedisType.Stream => RedisEntryType.Stream,
			_ => RedisEntryType.None,
		};
	}

	public object GetEntry(string key) => this.GetEntry(key, out RedisEntryType _);
	public object GetEntry(string key, out RedisEntryType entryType)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		var entryKey = this.GetKey(key);
		entryType = _database.KeyType(entryKey) switch
		{
			RedisType.String => RedisEntryType.String,
			RedisType.Hash => RedisEntryType.Dictionary,
			RedisType.List => RedisEntryType.List,
			RedisType.Set => RedisEntryType.Set,
			RedisType.SortedSet => RedisEntryType.SortedSet,
			RedisType.Stream => RedisEntryType.Stream,
			_ => RedisEntryType.None,
		};

		return entryType switch
		{
			RedisEntryType.String => _database.StringGet(entryKey).GetValue<object>(),
			RedisEntryType.Dictionary => new RedisDictionary(_database, entryKey),
			RedisEntryType.List => GetStringValues(_database.ListRange(entryKey)),
			RedisEntryType.Set => new RedisHashset(_database, entryKey, this.GetKeyPrefix()),
			RedisEntryType.SortedSet => GetStringValues(_database.SortedSetRangeByRank(entryKey)),
			RedisEntryType.Stream => new Messaging.RedisQueue(entryKey, _database),
			_ => null,
		};
	}

	public object GetEntry(string key, out TimeSpan? expiry) => this.GetEntry(key, out _, out expiry);
	public object GetEntry(string key, out RedisEntryType entryType, out TimeSpan? expiry)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		this.Connect();

		var entryKey = this.GetKey(key);
		expiry = _database.KeyTimeToLive(entryKey);
		entryType = _database.KeyType(entryKey) switch
		{
			RedisType.String => RedisEntryType.String,
			RedisType.Hash => RedisEntryType.Dictionary,
			RedisType.List => RedisEntryType.List,
			RedisType.Set => RedisEntryType.Set,
			RedisType.SortedSet => RedisEntryType.SortedSet,
			RedisType.Stream => RedisEntryType.Stream,
			_ => RedisEntryType.None,
		};

		return entryType switch
		{
			RedisEntryType.String => _database.StringGet(entryKey).GetValue<object>(),
			RedisEntryType.Dictionary => new RedisDictionary(_database, entryKey),
			RedisEntryType.List => GetStringValues(_database.ListRange(entryKey)),
			RedisEntryType.Set => new RedisHashset(_database, entryKey, this.GetKeyPrefix()),
			RedisEntryType.SortedSet => GetStringValues(_database.SortedSetRangeByRank(entryKey)),
			RedisEntryType.Stream => new Messaging.RedisQueue(entryKey, _database),
			_ => null,
		};
	}

	public async Task<(object value, RedisEntryType entryType, TimeSpan? expiry)> GetEntryAsync(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		//确保连接成功
		await this.ConnectAsync(cancellation);

		var entryKey = this.GetKey(key);
		var expiryTask = _database.KeyTimeToLiveAsync(entryKey);
		var typeTask = _database.KeyTypeAsync(entryKey);
		await Task.WhenAll(expiryTask, typeTask).WaitAsync(cancellation);

		var expiry = await expiryTask;
		var entryType = await typeTask switch
		{
			RedisType.String => RedisEntryType.String,
			RedisType.Hash => RedisEntryType.Dictionary,
			RedisType.List => RedisEntryType.List,
			RedisType.Set => RedisEntryType.Set,
			RedisType.SortedSet => RedisEntryType.SortedSet,
			RedisType.Stream => RedisEntryType.Stream,
			_ => RedisEntryType.None,
		};

		object value = entryType switch
		{
			RedisEntryType.String => (await _database.StringGetAsync(entryKey).WaitAsync(cancellation)).GetValue<object>(),
			RedisEntryType.Dictionary => new RedisDictionary(_database, entryKey),
			RedisEntryType.List => GetStringValues(await _database.ListRangeAsync(entryKey).WaitAsync(cancellation)),
			RedisEntryType.Set => new RedisHashset(_database, entryKey, this.GetKeyPrefix()),
			RedisEntryType.SortedSet => GetStringValues(await _database.SortedSetRangeByRankAsync(entryKey).WaitAsync(cancellation)),
			RedisEntryType.Stream => new Messaging.RedisQueue(entryKey, _database),
			_ => null,
		};

		return (value, entryType, expiry);
	}

	public bool SetEntry(string key, object value, TimeSpan expiry, CacheRequisite requisite = CacheRequisite.Always)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));
		if(expiry < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(expiry));

		this.Connect();
		key = this.GetKey(key);

		if(value == null)
			return _database.KeyDelete(key);

		if(value is MemoryStream memory)
			return _database.StringSet(key, RedisValue.CreateFrom(memory), expiry > TimeSpan.Zero ? expiry : (TimeSpan?)null, GetWhen(requisite), CommandFlags.None);

		if(value is byte[] buffer)
			return _database.StringSet(key, buffer, expiry > TimeSpan.Zero ? expiry : (TimeSpan?)null, GetWhen(requisite), CommandFlags.None);

		if(TryGetDictionaryEntries(value, out var entries))
		{
			var transaction = _database.CreateTransaction();

			if(TryGetCondition(key, requisite, out var condition))
				transaction.AddCondition(condition);

			transaction.KeyDeleteAsync(key);

			if(entries.Length > 0)
				transaction.HashSetAsync(key, entries);

			if(entries.Length > 0 && expiry > TimeSpan.Zero)
				transaction.KeyExpireAsync(key, expiry);

			return transaction.Execute();
		}

		if(value.GetType().IsHashset())
		{
			var transaction = _database.CreateTransaction();

			if(TryGetCondition(key, requisite, out var condition))
				transaction.AddCondition(condition);

			var values = new List<RedisValue>();

			foreach(var item in (IEnumerable)value)
				values.Add(RedisValue.Unbox(item));

			transaction.KeyDeleteAsync(key);

			if(values.Count > 0)
				transaction.SetAddAsync(key, values.ToArray());

			if(values.Count > 0 && expiry > TimeSpan.Zero)
				transaction.KeyExpireAsync(key, expiry);

			return transaction.Execute();
		}

		if(value.GetType().IsList())
		{
			var transaction = _database.CreateTransaction();

			if(TryGetCondition(key, requisite, out var condition))
				transaction.AddCondition(condition);

			var values = new List<RedisValue>();

			foreach(var item in (IEnumerable)value)
				values.Add(RedisValue.Unbox(item));

			transaction.KeyDeleteAsync(key);

			if(values.Count > 0)
				transaction.ListRightPushAsync(key, values.ToArray());

			if(values.Count > 0 && expiry > TimeSpan.Zero)
				transaction.KeyExpireAsync(key, expiry);

			return transaction.Execute();
		}

		return _database.StringSet(key, RedisValue.Unbox(value), expiry > TimeSpan.Zero ? expiry : (TimeSpan?)null, GetWhen(requisite), CommandFlags.None);
	}

	public async ValueTask<bool> SetEntryAsync(string key, object value, TimeSpan expiry, CacheRequisite requisite = CacheRequisite.Always, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));
		if(expiry < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(expiry));

		cancellation.ThrowIfCancellationRequested();
		await this.ConnectAsync(cancellation);
		key = this.GetKey(key);

		if(value == null)
			return await _database.KeyDeleteAsync(key).WaitAsync(cancellation);

		if(value is MemoryStream memory)
			return await _database.StringSetAsync(key, RedisValue.CreateFrom(memory), expiry > TimeSpan.Zero ? expiry : (TimeSpan?)null, GetWhen(requisite), CommandFlags.None).WaitAsync(cancellation);

		if(value is byte[] buffer)
			return await _database.StringSetAsync(key, buffer, expiry > TimeSpan.Zero ? expiry : (TimeSpan?)null, GetWhen(requisite), CommandFlags.None).WaitAsync(cancellation);

		if(TryGetDictionaryEntries(value, out var entries))
		{
			var transaction = _database.CreateTransaction();

			if(TryGetCondition(key, requisite, out var condition))
				transaction.AddCondition(condition);

			_ = transaction.KeyDeleteAsync(key);

			if(entries.Length > 0)
				_ = transaction.HashSetAsync(key, entries);

			if(entries.Length > 0 && expiry > TimeSpan.Zero)
				_ = transaction.KeyExpireAsync(key, expiry);

			return await transaction.ExecuteAsync().WaitAsync(cancellation);
		}

		if(value.GetType().IsHashset())
		{
			var transaction = _database.CreateTransaction();

			if(TryGetCondition(key, requisite, out var condition))
				transaction.AddCondition(condition);

			var values = new List<RedisValue>();

			foreach(var item in (IEnumerable)value)
				values.Add(RedisValue.Unbox(item));

			_ = transaction.KeyDeleteAsync(key);

			if(values.Count > 0)
				_ = transaction.SetAddAsync(key, values.ToArray());

			if(values.Count > 0 && expiry > TimeSpan.Zero)
				_ = transaction.KeyExpireAsync(key, expiry);

			return await transaction.ExecuteAsync().WaitAsync(cancellation);
		}

		if(value.GetType().IsList())
		{
			var transaction = _database.CreateTransaction();

			if(TryGetCondition(key, requisite, out var condition))
				transaction.AddCondition(condition);

			var values = new List<RedisValue>();

			foreach(var item in (IEnumerable)value)
				values.Add(RedisValue.Unbox(item));

			_ = transaction.KeyDeleteAsync(key);

			if(values.Count > 0)
				_ = transaction.ListRightPushAsync(key, values.ToArray());

			if(values.Count > 0 && expiry > TimeSpan.Zero)
				_ = transaction.KeyExpireAsync(key, expiry);

			return await transaction.ExecuteAsync().WaitAsync(cancellation);
		}

		return await _database.StringSetAsync(key, RedisValue.Unbox(value), expiry > TimeSpan.Zero ? expiry : (TimeSpan?)null, GetWhen(requisite), CommandFlags.None).WaitAsync(cancellation);
	}

	public IDictionary<string, string> CreateDictionary(string name)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Connect();

		name = this.GetKey(name);

		if(_database.KeyExists(name))
			throw new InvalidOperationException($"The specified '{name}' key already exists.");

		return new RedisDictionary(_database, name);
	}

	public ISet<string> CreateHashset(string name)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Connect();

		name = this.GetKey(name);

		if(_database.KeyExists(name))
			throw new InvalidOperationException($"The specified '{name}' key already exists.");

		return new RedisHashset(_database, name, this.GetKeyPrefix());
	}
	#endregion

	#region 处置方法
	public void Dispose() => this.DisposeAsync().AsTask().GetAwaiter().GetResult();
	public ValueTask DisposeAsync()
	{
		lock(_disposeLock)
			return new ValueTask(_disposeTask ??= this.DisposeAsyncCore());
	}

	private async Task DisposeAsyncCore()
	{
		Interlocked.Exchange(ref _disposed, 1);

		await _subscriptionLock.WaitAsync();
		try
		{
			foreach(var subscription in _subscriptions.Keys)
			{
				try
				{
					await subscription.DisposeAsync();
				}
				catch(Exception exception)
				{
					Zongsoft.Diagnostics.Logging.GetLogging(typeof(RedisService)).Error(exception);
				}
			}

			_subscriptions.Clear();

			await _connectionLock.WaitAsync();
			try
			{
				_database = null;

				var connection = Interlocked.Exchange(ref _connection, null);
				if(connection != null)
					await connection.DisposeAsync();
			}
			finally
			{
				_connectionLock.Release();
			}
		}
		finally
		{
			_subscriptionLock.Release();
		}

		GC.SuppressFinalize(this);
	}
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private string GetKey(string key) => string.IsNullOrEmpty(_namespace) ? key : $"{_namespace}:{key}";

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private string GetKeyPrefix() => string.IsNullOrEmpty(_namespace) ? string.Empty : _namespace + ":";

	private string GetKeyPattern(string pattern)
	{
		if(string.IsNullOrEmpty(_namespace))
			return pattern;

		StringBuilder builder = null;

		for(int i = 0; i < _namespace.Length; i++)
		{
			var character = _namespace[i];

			if(character is '*' or '?' or '[' or ']' or '\\')
			{
				builder ??= new StringBuilder(_namespace.Length + 8).Append(_namespace, 0, i);
				builder.Append('\\');
			}

			builder?.Append(character);
		}

		return $"{builder?.ToString() ?? _namespace}:{pattern}";
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private void ThrowIfDisposed()
	{
		if(Volatile.Read(ref _disposed) != 0)
			throw new ObjectDisposedException(this.GetType().Name);
	}

	internal void Unregister(RedisCacheSubscription subscription) => _subscriptions.TryRemove(subscription, out _);

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static When GetWhen(CacheRequisite requisite)
	{
		return requisite switch
		{
			CacheRequisite.Exists => When.Exists,
			CacheRequisite.NotExists => When.NotExists,
			_ => When.Always,
		};
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static bool TryGetCondition(string key, CacheRequisite requisite, out Condition condition)
	{
		switch(requisite)
		{
			case CacheRequisite.Exists:
				condition = Condition.KeyExists(key);
				return true;
			case CacheRequisite.NotExists:
				condition = Condition.KeyNotExists(key);
				return true;
			default:
				condition = null;
				return false;
		}
	}

	private static bool TryGetDictionaryEntries(object value, out HashEntry[] entries)
	{
		if(value is IDictionary dictionary)
		{
			entries = new HashEntry[dictionary.Count];
			var index = 0;

			foreach(DictionaryEntry entry in dictionary)
				entries[index++] = new HashEntry(RedisValue.Unbox(entry.Key), RedisValue.Unbox(entry.Value));

			return true;
		}

		if(value == null || !value.GetType().IsDictionary() || value is not IEnumerable enumerable)
		{
			entries = null;
			return false;
		}

		var result = new List<HashEntry>();

		foreach(var item in enumerable)
		{
			if(item == null)
				continue;

			var type = item.GetType();
			var key = type.GetProperty("Key")?.GetValue(item);
			var data = type.GetProperty("Value")?.GetValue(item);
			result.Add(new HashEntry(RedisValue.Unbox(key), RedisValue.Unbox(data)));
		}

		entries = result.ToArray();
		return true;
	}

	private static string[] GetStringValues(RedisValue[] values)
	{
		if(values == null || values.Length == 0)
			return Array.Empty<string>();

		var result = new string[values.Length];

		for(int i = 0; i < values.Length; i++)
			result[i] = values[i];

		return result;
	}

	private RedisServiceInfo GetInfoCore()
	{
		var info = new RedisServiceInfo(_name, _namespace, this.DatabaseId, this.Settings);
		var endpoints = _connection.GetEndPoints();

		info.Servers = new RedisServerDescriptor[endpoints.Length];

		for(int i = 0; i < endpoints.Length; i++)
		{
			info.Servers[i] = new RedisServerDescriptor(_connection.GetServer(endpoints[i]));
		}

		return info;
	}

	private void Connect(int databaseId = -1)
	{
		this.ThrowIfDisposed();

		if(_database != null)
			return;

		var options = this.Options ?? throw new InvalidOperationException($"The connection string for the redis named '{_name}' is not configured.");

		_connectionLock.Wait();

		try
		{
			this.ThrowIfDisposed();

			if(_database == null)
			{
				var connection = ConnectionMultiplexer.Connect(options);

				if(Volatile.Read(ref _disposed) != 0)
				{
					connection.Dispose();
					this.ThrowIfDisposed();
				}

				_connection = connection;
				_database = connection.GetDatabase(databaseId);
			}
		}
		finally
		{
			_connectionLock.Release();
		}
	}

	private ValueTask ConnectAsync(CancellationToken cancellation = default) => this.ConnectAsync(-1, cancellation);
	private async ValueTask ConnectAsync(int databaseId, CancellationToken cancellation = default)
	{
		cancellation.ThrowIfCancellationRequested();
		this.ThrowIfDisposed();

		if(_database != null)
			return;

		var options = this.Options ?? throw new InvalidOperationException($"The connection string for the redis named '{_name}' is not configured.");

		await _connectionLock.WaitAsync(cancellation);

		try
		{
			this.ThrowIfDisposed();

			if(_database == null)
			{
				var task = ConnectionMultiplexer.ConnectAsync(options);
				ConnectionMultiplexer connection;

				try
				{
					connection = await task.WaitAsync(cancellation);
				}
				catch(OperationCanceledException)
				{
					_ = DisposeConnectionAsync(task);
					throw;
				}

				if(Volatile.Read(ref _disposed) != 0)
				{
					await connection.DisposeAsync();
					this.ThrowIfDisposed();
				}

				_connection = connection;
				_database = connection.GetDatabase(databaseId);
			}
		}
		finally
		{
			_connectionLock.Release();
		}
	}

	private static async Task DisposeConnectionAsync(Task<ConnectionMultiplexer> task)
	{
		try
		{
			var connection = await task;
			await connection.DisposeAsync();
		}
		catch
		{
		}
	}
	#endregion
}
