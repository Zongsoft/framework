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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using dotnet_etcd;
using Google.Protobuf;

using Zongsoft.Configuration;
using Zongsoft.Services;

namespace Zongsoft.Externals.Etcd;

public sealed partial class EtcdService : IDisposable, IAsyncDisposable
{
	private string _namespace;
	private IConnectionSettings _settings;
	private EtcdClient _client;
	private readonly SemaphoreSlim _connectionLock = new(1, 1);
	private bool _activated;
	private int _disposed;

	public EtcdService(string name)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name.Trim();
	}

	public EtcdService(string name, IConnectionSettings settings)
	{
		if(string.IsNullOrWhiteSpace(name))
		{
			if(settings == null || string.IsNullOrEmpty(settings.Name))
				throw new ArgumentNullException(nameof(name));

			name = settings.Name;
		}

		this.Name = name.Trim();
		_settings = settings;
	}

	public EtcdService(string name, string connectionString)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));
		if(string.IsNullOrWhiteSpace(connectionString))
			throw new ArgumentNullException(nameof(connectionString));

		this.Name = name.Trim();
		_settings = Configuration.EtcdConnectionSettingsDriver.Instance.GetSettings(connectionString);
	}

	public string Name { get; }
	public string Namespace
	{
		get => _namespace;
		set
		{
			if(_activated)
				throw new InvalidOperationException("The namespace cannot be changed after the etcd service has been activated.");

			_namespace = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().TrimEnd(':');
		}
	}

	public IConnectionSettings Settings => _settings ??= ApplicationContext.Current?.Configuration.GetConnectionSettings("/Externals/Etcd/ConnectionSettings", this.Name, "etcd");

	public async ValueTask HeartbeatAsync(CancellationToken cancellation = default)
	{
		var client = await this.ConnectAsync(cancellation);
		await client.GetAsync(GetKey("__heartbeat__"), null, null, cancellation);
	}

	public bool Exists(string key) => this.ExistsAsync(key).AsTask().GetAwaiter().GetResult();
	public async ValueTask<bool> ExistsAsync(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			return false;

		var client = await this.ConnectAsync(cancellation);
		var response = await client.GetAsync(GetKey(key), null, null, cancellation);
		return response.Count > 0;
	}

	public string GetValue(string key) => this.GetValueAsync(key).AsTask().GetAwaiter().GetResult();
	public async ValueTask<string> GetValueAsync(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		var client = await this.ConnectAsync(cancellation);
		var response = await client.GetAsync(GetKey(key), null, null, cancellation);
		return response.Kvs.Count == 0 ? null : response.Kvs[0].Value.ToStringUtf8();
	}

	public void SetValue(string key, string value, TimeSpan? expiry = null) => this.SetValueAsync(key, value, expiry).AsTask().GetAwaiter().GetResult();
	public async ValueTask SetValueAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		var client = await this.ConnectAsync(cancellation);
		long leaseId = 0;
		try
		{
			if(expiry > TimeSpan.Zero)
			{
				var lease = await client.LeaseGrantAsync(new Etcdserverpb.LeaseGrantRequest { TTL = GetLeaseSeconds(expiry.Value) }, null, null, cancellation);
				leaseId = lease.ID;
			}

			await client.PutAsync(new Etcdserverpb.PutRequest
			{
				Key = ByteString.CopyFromUtf8(GetKey(key)),
				Value = ByteString.CopyFromUtf8(value ?? string.Empty),
				Lease = leaseId,
			}, null, null, cancellation);
		}
		catch
		{
			if(leaseId != 0)
				await RevokeLeaseAsync(client, leaseId);
			throw;
		}
	}

	public bool Remove(string key) => this.RemoveAsync(key).AsTask().GetAwaiter().GetResult();
	public async ValueTask<bool> RemoveAsync(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			return false;

		var client = await this.ConnectAsync(cancellation);
		var response = await client.DeleteAsync(GetKey(key), null, null, cancellation);
		return response.Deleted > 0;
	}

	public IReadOnlyDictionary<string, string> Find(string prefix = null) => this.FindAsync(prefix).AsTask().GetAwaiter().GetResult();
	public async ValueTask<IReadOnlyDictionary<string, string>> FindAsync(string prefix = null, CancellationToken cancellation = default)
	{
		var client = await this.ConnectAsync(cancellation);
		var physicalPrefix = GetKey(prefix ?? string.Empty);
		var response = await client.GetAsync(new Etcdserverpb.RangeRequest
		{
			Key = ByteString.CopyFromUtf8(physicalPrefix),
			RangeEnd = GetPrefixRangeEnd(physicalPrefix),
		}, null, null, cancellation);
		var result = new Dictionary<string, string>(response.Kvs.Count, StringComparer.Ordinal);
		var namespacePrefix = string.IsNullOrEmpty(_namespace) ? string.Empty : _namespace + ":";
		foreach(var item in response.Kvs)
		{
			var key = item.Key.ToStringUtf8();
			if(namespacePrefix.Length > 0 && key.StartsWith(namespacePrefix, StringComparison.Ordinal))
				key = key[namespacePrefix.Length..];
			result[key] = item.Value.ToStringUtf8();
		}
		return result;
	}

	public long Count(string prefix = null) => this.CountAsync(prefix).AsTask().GetAwaiter().GetResult();
	public async ValueTask<long> CountAsync(string prefix = null, CancellationToken cancellation = default)
	{
		var client = await this.ConnectAsync(cancellation);
		var physicalPrefix = GetKey(prefix ?? string.Empty);
		var response = await client.GetAsync(new Etcdserverpb.RangeRequest
		{
			Key = ByteString.CopyFromUtf8(physicalPrefix),
			RangeEnd = GetPrefixRangeEnd(physicalPrefix),
			CountOnly = true,
		}, null, null, cancellation);
		return response.Count;
	}

	internal string GetKey(string key) => string.IsNullOrEmpty(_namespace) ? key : $"{_namespace}:{key}";

	private static ByteString GetPrefixRangeEnd(string prefix)
	{
		if(string.IsNullOrEmpty(prefix))
			return ByteString.CopyFrom([0]);

		var bytes = System.Text.Encoding.UTF8.GetBytes(prefix);
		for(var index = bytes.Length - 1; index >= 0; index--)
		{
			if(bytes[index] == byte.MaxValue)
				continue;

			bytes[index]++;
			return ByteString.CopyFrom(bytes, 0, index + 1);
		}

		return ByteString.CopyFrom([0]);
	}

	internal async ValueTask<EtcdClient> ConnectAsync(CancellationToken cancellation = default)
	{
		ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

		var client = Volatile.Read(ref _client);
		if(client != null)
			return client;

		await _connectionLock.WaitAsync(cancellation);
		try
		{
			client = _client;
			if(client != null)
				return client;

			var settings = this.Settings as Configuration.EtcdConnectionSettings ??
				throw new ConfigurationException($"Missing the '{this.Name}' etcd connection setting.");
			var server = string.IsNullOrWhiteSpace(settings.Server) ? "127.0.0.1" : settings.Server.Trim();
			var endpoints = string.Join(',', server.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(endpoint =>
			{
				if(Uri.TryCreate(endpoint, UriKind.Absolute, out _))
					return endpoint;
				if(endpoint.LastIndexOf(':') > endpoint.LastIndexOf(']'))
					return $"http://{endpoint}";
				return $"http://{endpoint}:{settings.Port}";
			}));

			client = string.IsNullOrEmpty(settings.UserName) ?
				new EtcdClient(endpoints) :
				new EtcdClient(endpoints, settings.UserName, settings.Password);
			if(Volatile.Read(ref _disposed) != 0)
			{
				client.Dispose();
				throw new ObjectDisposedException(this.GetType().FullName);
			}
			_activated = true;
			Volatile.Write(ref _client, client);
			return client;
		}
		finally
		{
			_connectionLock.Release();
		}
	}

	public void Dispose()
	{
		if(Interlocked.Exchange(ref _disposed, 1) != 0)
			return;

		Interlocked.Exchange(ref _client, null)?.Dispose();
		GC.SuppressFinalize(this);
	}

	public ValueTask DisposeAsync()
	{
		this.Dispose();
		return ValueTask.CompletedTask;
	}
}
