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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using StackExchange.Redis;

using Zongsoft.Messaging;
using Zongsoft.Externals.Redis.Configuration;

namespace Zongsoft.Externals.Redis.Messaging;

/// <summary>提供基于 Redis 的消息存储。</summary>
public sealed partial class RedisMessageStorage : MessageStorageBase<RedisConnectionSettings>, IDisposable, IAsyncDisposable
{
	#region 常量定义
	private const string PREFIX = "Zongsoft.Messaging.Storage";
	private const int BATCH_SIZE = 100;
	#endregion

	#region 成员字段
	private readonly object _sync = new();
	private readonly SemaphoreSlim _semaphore = new(1, 1);
	private int _activated;
	private int _disposed;
	private string _prefix;
	private IDatabase _database;
	private RedisConnectionLease _lease;
	private ConfigurationOptions _options;
	#endregion

	#region 构造函数
	public RedisMessageStorage(RedisConnectionSettings settings) : base(settings ?? throw new ArgumentNullException(nameof(settings))) { }
	public RedisMessageStorage(string name, string connectionString) : this(RedisConnectionSettingsDriver.Instance.GetSettings(name, connectionString)) { }
	#endregion

	#region 公共属性
	public override string Name => RedisConnectionSettingsDriver.NAME;
	public override bool Disposable => true;
	public override RedisConnectionSettings Settings
	{
		get => base.Settings;
		set
		{
			lock(_sync)
			{
				this.ThrowIfDisposed();

				if(Volatile.Read(ref _activated) != 0)
					throw new InvalidOperationException(Properties.Resources.RedisMessageStorageSettingsImmutable_Message);

				base.Settings = value;
			}
		}
	}
	#endregion

	#region 重写方法
	protected override async ValueTask<int> OnClearAsync(string topic, CancellationToken cancellation)
	{
		var database = await this.GetDatabaseAsync(cancellation);
		var keys = new List<RedisKey>(BATCH_SIZE);
		var count = 0;

		await foreach(var key in this.ScanKeysAsync(database, cancellation))
		{
			if(topic != null)
			{
				var value = await database.StringGetAsync(key).WaitAsync(cancellation);
				if(value.IsNull || !string.Equals(MessageModel.Deserialize((byte[])value).Topic, topic, StringComparison.Ordinal))
					continue;
			}

			keys.Add(key);
			if(keys.Count < BATCH_SIZE)
				continue;

			count = checked(count + await DeleteAsync(database, keys, cancellation));
			keys.Clear();
		}

		if(keys.Count > 0)
			count = checked(count + await DeleteAsync(database, keys, cancellation));

		return count;
	}

	protected override async ValueTask OnSetAsync(Message message, TimeSpan expiry, CancellationToken cancellation)
	{
		var data = MessageModel.Serialize(message);
		var database = await this.GetDatabaseAsync(cancellation);
		var expiration = expiry > TimeSpan.Zero ? expiry : (TimeSpan?)null;
		var result = await database.StringSetAsync(this.GetKey(message.Identifier), (RedisValue)data, expiration, When.Always, CommandFlags.None).WaitAsync(cancellation);

		if(!result)
			throw new InvalidOperationException(Properties.Resources.RedisMessageStorageWriteRejected_Message);
	}

	protected override async ValueTask<bool> OnRemoveAsync(string identifier, CancellationToken cancellation)
	{
		var database = await this.GetDatabaseAsync(cancellation);
		return await database.KeyDeleteAsync(this.GetKey(identifier)).WaitAsync(cancellation);
	}

	protected override async IAsyncEnumerable<Message> OnGetAsync(string topic, [EnumeratorCancellation]CancellationToken cancellation)
	{
		var database = await this.GetDatabaseAsync(cancellation);

		await foreach(var key in this.ScanKeysAsync(database, cancellation))
		{
			var value = await database.StringGetAsync(key).WaitAsync(cancellation);
			if(value.IsNull)
				continue;

			var snapshot = MessageModel.Deserialize((byte[])value);
			if(topic == null || string.Equals(snapshot.Topic, topic, StringComparison.Ordinal))
				yield return snapshot.ToMessage();
		}
	}
	#endregion

	#region 释放方法
	public void Dispose() => this.DisposeAsync().AsTask().GetAwaiter().GetResult();
	public async ValueTask DisposeAsync()
	{
		RedisConnectionLease lease = null;

		await _semaphore.WaitAsync();
		try
		{
			if(Interlocked.Exchange(ref _disposed, 1) != 0)
				return;

			_database = null;
			lease = Interlocked.Exchange(ref _lease, null);
		}
		finally
		{
			_semaphore.Release();
		}

		if(lease != null)
			await lease.DisposeAsync();

		GC.SuppressFinalize(this);
	}
	#endregion

	#region 私有方法
	private void Activate()
	{
		if(Volatile.Read(ref _activated) != 0)
		{
			this.ThrowIfDisposed();
			return;
		}

		lock(_sync)
		{
			this.ThrowIfDisposed();
			if(Volatile.Read(ref _activated) != 0)
				return;

			var settings = base.Settings ?? throw new InvalidOperationException(Properties.Resources.RedisMessageStorageSettingsUnavailable_Message);
			var @namespace = string.IsNullOrWhiteSpace(settings.Namespace) ? settings.Name : settings.Namespace;

			_options = settings.GetOptions();
			_prefix = $"{PREFIX}:{(string.IsNullOrWhiteSpace(@namespace) ? string.Empty : @namespace.Trim())}";
			Volatile.Write(ref _activated, 1);
		}
	}

	private async ValueTask<IDatabase> GetDatabaseAsync(CancellationToken cancellation)
	{
		this.Activate();
		var database = Volatile.Read(ref _database);
		if(database != null)
			return database;

		await _semaphore.WaitAsync(cancellation);
		try
		{
			this.ThrowIfDisposed();
			database = _database;
			if(database != null)
				return database;

			var lease = await RedisConnectionPool.AcquireAsync(_options, cancellation);
			try
			{
				database = lease.Connection.GetDatabase(_options.DefaultDatabase ?? -1);
				_lease = lease;
				Volatile.Write(ref _database, database);
				return database;
			}
			catch
			{
				await lease.DisposeAsync();
				throw;
			}
		}
		finally
		{
			_semaphore.Release();
		}
	}

	private RedisKey GetKey(string identifier)
	{
		this.Activate();
		return $"{_prefix}:{identifier}";
	}

	private RedisValue GetPattern()
	{
		this.Activate();
		return EscapePattern(_prefix + ":") + "*";
	}

	private async IAsyncEnumerable<RedisKey> ScanKeysAsync(IDatabase database, [EnumeratorCancellation] CancellationToken cancellation)
	{
		var pattern = this.GetPattern();
		var keys = new HashSet<RedisKey>();

		foreach(var endpoint in database.Multiplexer.GetEndPoints())
		{
			cancellation.ThrowIfCancellationRequested();
			var server = database.Multiplexer.GetServer(endpoint);
			if(!server.IsConnected || server.IsReplica)
				continue;

			await foreach(var key in server.KeysAsync(database.Database, pattern).WithCancellation(cancellation))
			{
				if(keys.Add(key))
					yield return key;
			}
		}
	}

	private static async ValueTask<int> DeleteAsync(IDatabase database, IReadOnlyList<RedisKey> keys, CancellationToken cancellation)
	{
		var tasks = new Task<bool>[keys.Count];
		for(int i = 0; i < keys.Count; i++)
			tasks[i] = database.KeyDeleteAsync(keys[i]);

		var results = await Task.WhenAll(tasks).WaitAsync(cancellation);
		var count = 0;
		for(int i = 0; i < results.Length; i++)
		{
			if(results[i])
				count++;
		}

		return count;
	}

	private static string EscapePattern(string value)
	{
		StringBuilder builder = null;
		for(int i = 0; i < value.Length; i++)
		{
			var character = value[i];
			if(character is '*' or '?' or '[' or ']' or '\\')
			{
				builder ??= new StringBuilder(value.Length + 8).Append(value, 0, i);
				builder.Append('\\');
			}

			builder?.Append(character);
		}

		return builder?.ToString() ?? value;
	}

	private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
	#endregion
}
