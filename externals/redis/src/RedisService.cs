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
	public partial class RedisService : ICache, IDisposable
	{
		#region 事件定义
		event EventHandler<CacheChangedEventArgs> ICache.Changed
		{
			add
			{
				throw new NotSupportedException();
			}
			remove
			{
				throw new NotSupportedException();
			}
		}
		#endregion

		#region 成员字段
		private readonly string _name;
		private string _namespace;
		private RedisServiceSettings _settings;

		private IDatabase _database;
		private volatile ConnectionMultiplexer _connection;
		private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
		#endregion

		#region 构造函数
		public RedisService(string name)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
		}

		public RedisService(string name, RedisServiceSettings settings)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get => _name;
		}

		public string Namespace
		{
			get => _namespace;
			set => _namespace = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}

		public int DatabaseId
		{
			get => _database?.Database ?? -1;
		}

		public RedisServiceSettings Settings
		{
			get
			{
				if(_settings == null)
				{
					var connectionSettings = ApplicationContext.Current?.Configuration.GetOption<ConnectionSettingCollection>("/Externals/Redis/ConnectionSettings");

					if(connectionSettings != null && connectionSettings.Contains(_name))
						_settings = RedisServiceSettings.Parse(connectionSettings[_name].Value);
				}

				return _settings;
			}
		}
		#endregion

		#region 公共方法
		public void Use(int databaseId)
		{
			if(databaseId < 0)
				throw new ArgumentOutOfRangeException(nameof(databaseId));

			if(_connection == null)
				this.Connect(databaseId);

			if(_database.Database != databaseId)
				_database = _connection.GetDatabase(databaseId);
		}

		public async Task UseAsync(int databaseId, CancellationToken cancellation = default)
		{
			if(databaseId < 0)
				throw new ArgumentOutOfRangeException(nameof(databaseId));

			cancellation.ThrowIfCancellationRequested();

			if(_connection == null)
				await this.ConnectAsync(databaseId, cancellation);

			if(_database.Database != databaseId)
				_database = _connection.GetDatabase(databaseId);
		}

		public IEnumerable<string> Find(string pattern, int count = 100)
		{
			//确保连接成功
			this.Connect();

			return _connection.GetServer(_database.IdentifyEndpoint())
				.Keys(_database.Database, pattern, count)
				.Select(key => (string)key);
		}

		public async Task<IEnumerable<string>> FindAsync(string pattern, int count = 100, CancellationToken cancellation = default)
		{
			//确保连接成功
			await this.ConnectAsync(cancellation);

			return _connection.GetServer(_database.IdentifyEndpoint())
				.Keys(_database.Database, pattern, count)
				.Select(key => (string)key);
		}

		public RedisServiceInfo GetInfo()
		{
			this.Connect();
			return this.GetInfoCore();
		}

		public async Task<RedisServiceInfo> GetInfoAsync(CancellationToken cancellation = default)
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

		public async Task<RedisEntryType> GetEntryTypeAsync(string key, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();

			await this.ConnectAsync(cancellation);

			return await _database.KeyTypeAsync(GetKey(key)) switch
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

		public bool SetEntry(string key, object value, TimeSpan expiry, CacheRequisite requisite = CacheRequisite.Always)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			this.Connect();

			if(value == null)
				return _database.KeyDelete(key);

			key = GetKey(key);

			if(value is MemoryStream memory)
				return _database.StringSet(key, RedisValue.CreateFrom(memory), expiry > TimeSpan.Zero ? expiry : (TimeSpan?)null, GetWhen(requisite), CommandFlags.None);

			if(value is byte[] buffer)
			{
				using(var memoryStream = new MemoryStream(buffer))
				return _database.StringSet(key, RedisValue.CreateFrom(memoryStream), expiry > TimeSpan.Zero ? expiry : (TimeSpan?)null, GetWhen(requisite), CommandFlags.None);
			}

			if(value.GetType().IsDictionary(out var fields))
			{
				var transaction = _database.CreateTransaction();

				if(TryGetCondition(key, requisite, out var condition))
					transaction.AddCondition(condition);

				transaction.HashSetAsync(key, fields.Select(p => new HashEntry(RedisValue.Unbox(p.Key), RedisValue.Unbox(p.Value))).ToArray());

				if(expiry > TimeSpan.Zero)
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

				transaction.SetAddAsync(key, values.ToArray());

				if(expiry > TimeSpan.Zero)
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

				transaction.ListRightPushAsync(key, values.ToArray());

				if(expiry > TimeSpan.Zero)
					transaction.KeyExpireAsync(key, expiry);

				return transaction.Execute();
			}

			return _database.StringSet(key, RedisValue.Unbox(value), expiry > TimeSpan.Zero ? expiry : (TimeSpan?)null, GetWhen(requisite), CommandFlags.None);
		}

		public async Task<bool> SetEntryAsync(string key, object value, TimeSpan expiry, CacheRequisite requisite = CacheRequisite.Always, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);

			if(value == null)
				return await _database.KeyDeleteAsync(key);

			key = GetKey(key);

			if(value is MemoryStream memory)
				return await _database.StringSetAsync(key, RedisValue.CreateFrom(memory), expiry > TimeSpan.Zero ? expiry : (TimeSpan?)null, GetWhen(requisite), CommandFlags.None);

			if(value is byte[] buffer)
			{
				using(var memoryStream = new MemoryStream(buffer))
					return await _database.StringSetAsync(key, RedisValue.CreateFrom(memoryStream), expiry > TimeSpan.Zero ? expiry : (TimeSpan?)null, GetWhen(requisite), CommandFlags.None);
			}

			if(value.GetType().IsDictionary(out var fields))
			{
				var transaction = _database.CreateTransaction();

				if(TryGetCondition(key, requisite, out var condition))
					transaction.AddCondition(condition);

				await transaction.HashSetAsync(key, fields.Select(p => new HashEntry(RedisValue.Unbox(p.Key), RedisValue.Unbox(p.Value))).ToArray());

				if(expiry > TimeSpan.Zero)
					await transaction.KeyExpireAsync(key, expiry);

				return await transaction.ExecuteAsync();
			}

			if(value.GetType().IsHashset())
			{
				var transaction = _database.CreateTransaction();

				if(TryGetCondition(key, requisite, out var condition))
					transaction.AddCondition(condition);

				var values = new List<RedisValue>();

				foreach(var item in (IEnumerable)value)
					values.Add(RedisValue.Unbox(item));

				await transaction.SetAddAsync(key, values.ToArray());

				if(expiry > TimeSpan.Zero)
					await transaction.KeyExpireAsync(key, expiry);

				return await transaction.ExecuteAsync();
			}

			if(value.GetType().IsList())
			{
				var transaction = _database.CreateTransaction();

				if(TryGetCondition(key, requisite, out var condition))
					transaction.AddCondition(condition);

				var values = new List<RedisValue>();

				foreach(var item in (IEnumerable)value)
					values.Add(RedisValue.Unbox(item));

				await transaction.ListRightPushAsync(key, values.ToArray());

				if(expiry > TimeSpan.Zero)
					await transaction.KeyExpireAsync(key, expiry);

				return await transaction.ExecuteAsync();
			}

			return await _database.StringSetAsync(key, RedisValue.Unbox(value), expiry > TimeSpan.Zero ? expiry : (TimeSpan?)null, GetWhen(requisite), CommandFlags.None);
		}
		#endregion

		#region 缓存实现
		public long GetCount()
		{
			//确保连接成功
			this.Connect();

			return _connection.GetServer(_database.IdentifyEndpoint()).DatabaseSize(_database.Database);
		}

		public async Task<long> GetCountAsync(CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);
			return await _connection.GetServer(_database.IdentifyEndpoint()).DatabaseSizeAsync(_database.Database);
		}

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

		public async Task ClearAsync(CancellationToken cancellation = default)
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

		public bool Exists(string key)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			//确保连接成功
			this.Connect();

			return _database.KeyExists(GetKey(key));
		}

		public async Task<bool> ExistsAsync(string key, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);
			return await _database.KeyExistsAsync(GetKey(key));
		}

		public TimeSpan? GetExpiry(string key)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			//确保连接成功
			this.Connect();

			return _database.KeyTimeToLive(GetKey(key));
		}

		public async Task<TimeSpan?> GetExpiryAsync(string key, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);
			return await _database.KeyTimeToLiveAsync(GetKey(key));
		}

		public object GetValue(string key)
		{
			return this.GetValue<object>(key);
		}

		public T GetValue<T>(string key)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			//确保连接成功
			this.Connect();

			return _database.StringGet(GetKey(key)).GetValue<T>();
		}

		public object GetValue(string key, out TimeSpan? expiry)
		{
			return GetValue<object>(key, out expiry);
		}

		public T GetValue<T>(string key, out TimeSpan? expiry)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			//确保连接成功
			this.Connect();

			var result = _database.StringGetWithExpiry(GetKey(key));
			expiry = result.Expiry;
			return result.Value.GetValue<T>();
		}

		public Task<object> GetValueAsync(string key, CancellationToken cancellation = default)
		{
			return this.GetValueAsync<object>(key, cancellation);
		}

		public async Task<T> GetValueAsync<T>(string key, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);
			return (await _database.StringGetAsync(GetKey(key))).GetValue<T>();
		}

		public Task<(object Value, TimeSpan? Expiry)> GetValueExpiryAsync(string key, CancellationToken cancellation = default)
		{
			return this.GetValueExpiryAsync<object>(key, cancellation);
		}

		public async Task<(T Value, TimeSpan? Expiry)> GetValueExpiryAsync<T>(string key, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();

			await this.ConnectAsync(cancellation);
			var result = await _database.StringGetWithExpiryAsync(GetKey(key));

			return (result.Value.GetValue<T>(), result.Expiry);
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

		public async Task<bool> RemoveAsync(string key, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);
			return await _database.KeyDeleteAsync(GetKey(key));
		}

		public async Task<int> RemoveAsync(IEnumerable<string> keys, CancellationToken cancellation = default)
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

		public async Task<bool> RenameAsync(string oldKey, string newKey, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(oldKey))
				throw new ArgumentNullException(nameof(oldKey));

			if(string.IsNullOrEmpty(newKey))
				throw new ArgumentNullException(nameof(newKey));

			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);
			return await _database.KeyRenameAsync(GetKey(oldKey), GetKey(newKey), When.Exists);
		}

		public bool SetExpiry(string key, TimeSpan expiry)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			//确保连接成功
			this.Connect();

			return _database.KeyExpire(GetKey(key), expiry);
		}

		public async Task<bool> SetExpiryAsync(string key, TimeSpan expiry, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);
			return await _database.KeyExpireAsync(GetKey(key), expiry);
		}

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

		public async Task<bool> SetValueAsync(string key, object value, CacheRequisite requisite = CacheRequisite.Always, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);
			return await this.SetEntryAsync(key, value, TimeSpan.Zero, requisite, cancellation);
		}

		public async Task<bool> SetValueAsync(string key, object value, TimeSpan expiry, CacheRequisite requisite = CacheRequisite.Always, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			await this.ConnectAsync(cancellation);
			return await this.SetEntryAsync(key, value, expiry, requisite, cancellation);
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			if(_connection != null)
				_connection.Close();
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private string GetKey(string key)
		{
			return string.IsNullOrEmpty(_namespace) ? key : _namespace + ":" + key;
		}

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
			if(_database != null)
				return;

			var settings = this.Settings ?? throw new InvalidOperationException($"The connection string for the redis named '{_name}' is not configured.");

			_connectionLock.Wait();

			try
			{
				if(_database == null)
				{
					_connection = ConnectionMultiplexer.Connect(settings.RedisOptions);
					_database = _connection.GetDatabase(databaseId);
				}
			}
			finally
			{
				_connectionLock.Release();
			}
		}

		private Task ConnectAsync(CancellationToken cancellation = default)
		{
			return ConnectAsync(-1, cancellation);
		}

		private async Task ConnectAsync(int databaseId = -1, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();

			if(_database != null)
				return;

			var settings = this.Settings ?? throw new InvalidOperationException($"The connection string for the redis named '{_name}' is not configured.");

			await _connectionLock.WaitAsync(cancellation);

			try
			{
				if(_database == null)
				{
					_connection = await ConnectionMultiplexer.ConnectAsync(settings.RedisOptions);
					_database = _connection.GetDatabase(databaseId);
				}
			}
			finally
			{
				_connectionLock.Release();
			}
		}
		#endregion
	}
}
