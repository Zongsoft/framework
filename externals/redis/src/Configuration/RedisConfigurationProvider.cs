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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Zongsoft.Caching;
using Zongsoft.Components;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis.Configuration;

public class RedisConfigurationProvider : ConfigurationProvider, IDisposable, IAsyncDisposable
{
	#region 私有字段
	private readonly RedisConfigurationSource _source;
	private readonly CancellationTokenSource _lifetime = new();
	private readonly object _sync = new();
	private RedisService _redis;
	private IDistributedCacheSubscription _subscription;
	private Task _subscriptionTask;
	private Task _reloadTask;
	private int _disposed;
	#endregion

	#region 构造函数
	public RedisConfigurationProvider(RedisConfigurationSource source) : this(source, null) { }

	internal RedisConfigurationProvider(RedisConfigurationSource source, RedisService redis)
	{
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_redis = redis;
	}
	#endregion

	#region 加载方法
	public override void Load()
	{
		this.LoadCore();
		lock(_sync)
			_subscriptionTask ??= this.SubscribeAsync();
	}

	private void LoadCore()
	{
		var redis = _redis ??= RedisServiceProvider.GetRedis(_source.Name);
		var entry = redis.GetEntry(_source.Namespace, out RedisEntryType entryType);

		if(entry == null || entryType == RedisEntryType.None)
		{
			this.Data = redis.GetStringEntries(_source.Namespace);
			return;
		}

		if(entry is IDictionary<string, string> dictionary)
			this.Data = new Dictionary<string, string>(dictionary, StringComparer.OrdinalIgnoreCase);
		else
			this.Data = redis.GetStringEntries(_source.Namespace);
	}
	#endregion

	internal Task SubscriptionTask
	{
		get
		{
			lock(_sync)
				return _subscriptionTask ?? Task.CompletedTask;
		}
	}

	#region 通知与释放
	public void Dispose() => this.DisposeAsync().AsTask().GetAwaiter().GetResult();
	public async ValueTask DisposeAsync()
	{
		if(Interlocked.Exchange(ref _disposed, 1) != 0)
			return;

		_lifetime.Cancel();
		Task subscriptionTask;
		Task reloadTask;
		lock(_sync)
		{
			subscriptionTask = _subscriptionTask;
			reloadTask = _reloadTask;
		}

		if(subscriptionTask != null)
		{
			try { await subscriptionTask; }
			catch(OperationCanceledException) { }
		}
		if(reloadTask != null)
		{
			try { await reloadTask; }
			catch(OperationCanceledException) { }
		}
		if(_subscription != null)
			await _subscription.DisposeAsync();

		_lifetime.Dispose();
		GC.SuppressFinalize(this);
	}

	private async Task SubscribeAsync()
	{
		try
		{
			var options = new DistributedCacheSubscriptionOptions(_source.Namespace, DistributedCacheNotificationKind.All);
			_subscription = await _redis.SubscribeAsync(Handler.Handle<DistributedCacheNotification>(this.OnNotificationAsync), options, _lifetime.Token);
			this.LoadCore();
			this.OnReload();
		}
		catch(OperationCanceledException) when (_lifetime.IsCancellationRequested)
		{
		}
		catch(Exception exception)
		{
			Zongsoft.Diagnostics.Logging.GetLogging(typeof(RedisConfigurationProvider)).Error(exception);
		}
	}

	private ValueTask OnNotificationAsync(DistributedCacheNotification notification, CancellationToken cancellation)
	{
		if(!string.Equals(notification.Key, _source.Namespace, StringComparison.Ordinal) &&
		   !notification.Key.StartsWith(_source.Namespace + ":", StringComparison.Ordinal))
			return ValueTask.CompletedTask;

		lock(_sync)
		{
			if(_disposed == 0 && (_reloadTask == null || _reloadTask.IsCompleted))
				_reloadTask = this.ReloadAsync(_lifetime.Token);
		}

		return ValueTask.CompletedTask;
	}

	private async Task ReloadAsync(CancellationToken cancellation)
	{
		try
		{
			await Task.Delay(TimeSpan.FromMilliseconds(50), cancellation);
			this.LoadCore();
			this.OnReload();
		}
		catch(OperationCanceledException) when (cancellation.IsCancellationRequested)
		{
		}
		catch(Exception exception)
		{
			Zongsoft.Diagnostics.Logging.GetLogging(typeof(RedisConfigurationProvider)).Error(exception);
		}
	}
	#endregion

	#region 嵌套子类
	private sealed class RedisConfigurationDictionary : IDictionary<string, string>
	{
		private readonly IServer _server;
		private readonly IDatabase _database;
		private readonly string _namespace;

		internal RedisConfigurationDictionary(IServer server, IDatabase database, string @namespace)
		{
			_server  = server ?? throw new ArgumentNullException(nameof(server));
			_database = database ?? throw new ArgumentNullException(nameof(database));

			if(string.IsNullOrEmpty(@namespace))
				throw new ArgumentNullException(nameof(@namespace));

			_namespace = @namespace;
		}

		public int Count => _server.Scan(_database.Database, GetPattern(_namespace)).Count();
		public bool IsReadOnly => false;

		public string this[string key]
		{
			get => _database.StringGet(GetKey(key));
			set => _database.StringSet(GetKey(key), value);
		}

		public ICollection<string> Keys => _server.Scan(_database.Database, GetPattern(_namespace))
			.Select(key => ((string)key)[(_namespace.Length + 1)..])
			.ToArray();

		public ICollection<string> Values
		{
			get
			{
				var keys = _server.Scan(_database.Database, GetPattern(_namespace)).ToArray();

				if(keys.Length == 0)
					return Array.Empty<string>();

				var values = this.GetValues(keys);
				var result = new string[values.Length];

				for(int i = 0; i < values.Length; i++)
					result[i] = values[i];

				return result;
			}
		}

		public void Add(string key, string value)
		{
			if(!_database.StringSet(GetKey(key), value, when: When.NotExists))
				throw new ArgumentException($"The specified '{key}' key already exists in the '{_namespace}' dictionary.");
		}

		void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> field) => this.Add(field.Key, field.Value);

		public bool Remove(string key) => _database.KeyDelete(GetKey(key));
		bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> field) => this.Remove(field.Key);

		public void Clear()
		{
			foreach(var key in _server.Scan(_database.Database, GetPattern(_namespace)))
				_database.KeyDelete(key);
		}

		public bool Contains(string key) => _database.KeyExists(GetKey(key));
		bool IDictionary<string, string>.ContainsKey(string key) => this.Contains(key);
		bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> field) => this.Contains(field.Key);

		public bool TryGetValue(string key, out string value)
		{
			var result = _database.StringGet(GetKey(key));
			value = result;
			return result.HasValue;
		}

		public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
		{
			ArgumentNullException.ThrowIfNull(array);

			if(arrayIndex < 0 || arrayIndex > array.Length)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));

			var keys = _server.Scan(_database.Database, GetPattern(_namespace)).ToArray();

			if(keys.Length > array.Length - arrayIndex)
				throw new ArgumentException("The destination array does not have enough available space.", nameof(array));

			var values = this.GetValues(keys);

			for(int i = 0; i < keys.Length; i++)
				array[arrayIndex + i] = new KeyValuePair<string, string>(((string)keys[i])[(_namespace.Length + 1)..], values[i]);
		}

		internal IDictionary<string, string> Snapshot()
		{
			var keys = _server.Scan(_database.Database, GetPattern(_namespace)).ToArray();
			var result = new Dictionary<string, string>(keys.Length, StringComparer.OrdinalIgnoreCase);
			if(keys.Length == 0)
				return result;

			var values = this.GetValues(keys);
			for(int i = 0; i < keys.Length; i++)
				result[((string)keys[i])[(_namespace.Length + 1)..]] = values[i];
			return result;
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			var keys = _server.Scan(_database.Database, GetPattern(_namespace)).ToArray();
			var values = this.GetValues(keys);

			for(int i = 0; i < keys.Length; i++)
				yield return new(((string)keys[i])[(_namespace.Length + 1)..], values[i]);
		}

		private RedisValue[] GetValues(RedisKey[] keys)
		{
			if(keys == null || keys.Length == 0)
				return Array.Empty<RedisValue>();

			var batch = _database.CreateBatch();
			var tasks = new System.Threading.Tasks.Task<RedisValue>[keys.Length];

			for(int i = 0; i < keys.Length; i++)
				tasks[i] = batch.StringGetAsync(keys[i]);

			batch.Execute();
			System.Threading.Tasks.Task.WaitAll(tasks);

			var values = new RedisValue[keys.Length];

			for(int i = 0; i < tasks.Length; i++)
				values[i] = tasks[i].Result;

			return values;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private string GetKey(string key) => $"{_namespace}:{key}";
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetPattern(string @namespace) => $"{@namespace}:*";
	}
	#endregion
}
