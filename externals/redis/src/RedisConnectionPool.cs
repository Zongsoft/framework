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
using System.Security.Cryptography;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Collections.Generic;
using System.Collections.Concurrent;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis;

internal static class RedisConnectionPool
{
	private static readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

	public static RedisConnectionLease Acquire(ConfigurationOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		var key = GetKey(options);

		while(true)
		{
			var entry = _entries.GetOrAdd(key, static key => new Entry(key));
			if(!entry.TryAcquire())
			{
				_entries.TryRemove(new KeyValuePair<string, Entry>(key, entry));
				continue;
			}

			try
			{
				return new RedisConnectionLease(entry, entry.GetConnection(options));
			}
			catch
			{
				entry.Release().AsTask().GetAwaiter().GetResult();
				throw;
			}
		}
	}

	public static async ValueTask<RedisConnectionLease> AcquireAsync(ConfigurationOptions options, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(options);
		cancellation.ThrowIfCancellationRequested();
		var key = GetKey(options);

		while(true)
		{
			var entry = _entries.GetOrAdd(key, static key => new Entry(key));
			if(!entry.TryAcquire())
			{
				_entries.TryRemove(new KeyValuePair<string, Entry>(key, entry));
				continue;
			}

			try
			{
				return new RedisConnectionLease(entry, await entry.GetConnectionAsync(options, cancellation));
			}
			catch
			{
				_ = entry.Release().AsTask();
				throw;
			}
		}
	}

	private static string GetKey(ConfigurationOptions options)
	{
		var text = string.Concat(
			options.ToString(), "\n",
			options.User, "\n",
			options.Password, "\n",
			options.SentinelUser, "\n",
			options.SentinelPassword);

		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
	}

	internal sealed class Entry
	{
		private readonly string _key;
		private readonly object _sync = new();
		private Task<ConnectionMultiplexer> _connectionTask;
		private int _references;
		private bool _disposed;

		public Entry(string key) => _key = key;

		public bool TryAcquire()
		{
			lock(_sync)
			{
				if(_disposed)
					return false;

				_references++;
				return true;
			}
		}

		public ConnectionMultiplexer GetConnection(ConfigurationOptions options)
		{
			var task = this.GetConnectionTask(options, false);
			return task.IsCompletedSuccessfully ? task.Result : task.GetAwaiter().GetResult();
		}

		public async ValueTask<ConnectionMultiplexer> GetConnectionAsync(ConfigurationOptions options, CancellationToken cancellation)
		{
			var task = this.GetConnectionTask(options, true);
			return task.IsCompletedSuccessfully ? task.Result : await task.WaitAsync(cancellation);
		}

		public async ValueTask Release()
		{
			Task<ConnectionMultiplexer> task = null;

			lock(_sync)
			{
				if(_references <= 0 || --_references > 0)
					return;

				_disposed = true;
				task = _connectionTask;
			}

			_entries.TryRemove(new KeyValuePair<string, Entry>(_key, this));

			if(task != null)
			{
				try
				{
					var connection = await task;
					try
					{
						await connection.DisposeAsync();
					}
					finally
					{
						RedisDiagnostics.ActiveConnections.Add(-1, new KeyValuePair<string, object>("redis.client.name", connection.ClientName));
					}
				}
				catch
				{
				}
			}
		}

		private Task<ConnectionMultiplexer> GetConnectionTask(ConfigurationOptions options, bool asynchronously)
		{
			TaskCompletionSource<ConnectionMultiplexer> source = null;

			lock(_sync)
			{
				ObjectDisposedException.ThrowIf(_disposed, this);

				if(_connectionTask != null)
					return _connectionTask;

				source = new TaskCompletionSource<ConnectionMultiplexer>(TaskCreationOptions.RunContinuationsAsynchronously);
				_connectionTask = source.Task;
			}

			if(asynchronously)
				_ = ConnectAsync(options, source);
			else
			{
				try
				{
					var connection = ConnectionMultiplexer.Connect(options);
					Initialize(connection);
					source.SetResult(connection);
				}
				catch(Exception exception)
				{
					source.SetException(exception);
				}
			}

			return source.Task;

			static async Task ConnectAsync(ConfigurationOptions options, TaskCompletionSource<ConnectionMultiplexer> source)
			{
				try
				{
					var connection = await ConnectionMultiplexer.ConnectAsync(options);
					Initialize(connection);
					source.SetResult(connection);
				}
				catch(Exception exception)
				{
					source.SetException(exception);
				}
			}

			static void Initialize(ConnectionMultiplexer connection)
			{
				RedisDiagnostics.ActiveConnections.Add(1, new KeyValuePair<string, object>("redis.client.name", connection.ClientName));
				connection.ConnectionFailed += static (_, _) => RedisDiagnostics.ConnectionFailures.Add(1);
				connection.ConnectionRestored += static (_, _) => RedisDiagnostics.ConnectionRestorations.Add(1);
				connection.ErrorMessage += static (_, _) => RedisDiagnostics.ConnectionErrors.Add(1);
			}
		}
	}
}

internal sealed class RedisConnectionLease : IDisposable, IAsyncDisposable
{
	private RedisConnectionPool.Entry _entry;

	internal RedisConnectionLease(RedisConnectionPool.Entry entry, ConnectionMultiplexer connection)
	{
		_entry = entry ?? throw new ArgumentNullException(nameof(entry));
		this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
	}

	public ConnectionMultiplexer Connection { get; }
	internal IConnectionMultiplexer CreateProxy()
	{
		var proxy = DispatchProxy.Create<IConnectionMultiplexer, LeaseProxy>();
		((LeaseProxy)(object)proxy).Initialize(this);
		return proxy;
	}

	public void Dispose()
	{
		var entry = Interlocked.Exchange(ref _entry, null);
		entry?.Release().AsTask().GetAwaiter().GetResult();
	}

	public ValueTask DisposeAsync()
	{
		var entry = Interlocked.Exchange(ref _entry, null);
		return entry?.Release() ?? ValueTask.CompletedTask;
	}

	private class LeaseProxy : DispatchProxy
	{
		private RedisConnectionLease _lease;

		internal void Initialize(RedisConnectionLease lease) => _lease = lease;

		protected override object Invoke(MethodInfo targetMethod, object[] args)
		{
			if((targetMethod.Name == nameof(IDisposable.Dispose) && targetMethod.GetParameters().Length == 0) ||
			   (targetMethod.Name == nameof(IConnectionMultiplexer.Close) && targetMethod.ReturnType == typeof(void)))
			{
				Interlocked.Exchange(ref _lease, null)?.Dispose();
				return null;
			}

			if(targetMethod.Name == nameof(IConnectionMultiplexer.CloseAsync) && targetMethod.ReturnType == typeof(Task))
			{
				var closing = Interlocked.Exchange(ref _lease, null);
				return closing?.DisposeAsync().AsTask() ?? Task.CompletedTask;
			}

			if(targetMethod.Name == nameof(IAsyncDisposable.DisposeAsync) && targetMethod.GetParameters().Length == 0)
			{
				var disposing = Interlocked.Exchange(ref _lease, null);
				return disposing == null ? ValueTask.CompletedTask : new ValueTask(disposing.DisposeAsync().AsTask());
			}

			var current = Volatile.Read(ref _lease) ?? throw new ObjectDisposedException(nameof(IConnectionMultiplexer));
			try
			{
				var result = targetMethod.Invoke(current.Connection, args);
				return result is IDatabase database ? DatabaseProxy.Create(database, (IConnectionMultiplexer)(object)this) : result;
			}
			catch(TargetInvocationException exception) when (exception.InnerException != null)
			{
				ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
				throw;
			}
		}
	}

	private class DatabaseProxy : DispatchProxy
	{
		private IDatabase _database;
		private IConnectionMultiplexer _connection;

		internal static IDatabase Create(IDatabase database, IConnectionMultiplexer connection)
		{
			var proxy = DispatchProxy.Create<IDatabase, DatabaseProxy>();
			var target = (DatabaseProxy)(object)proxy;
			target._database = database;
			target._connection = connection;
			return proxy;
		}

		protected override object Invoke(MethodInfo targetMethod, object[] args)
		{
			if(targetMethod.Name == "get_Multiplexer")
				return _connection;

			try
			{
				return targetMethod.Invoke(_database, args);
			}
			catch(TargetInvocationException exception) when (exception.InnerException != null)
			{
				ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
				throw;
			}
		}
	}
}
