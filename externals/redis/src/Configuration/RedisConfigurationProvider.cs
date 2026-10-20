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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

using Zongsoft.Caching;
using Zongsoft.Components;

namespace Zongsoft.Externals.Redis.Configuration;

public class RedisConfigurationProvider : ConfigurationProvider, IAsyncDisposable
{
	#region 私有字段
	private readonly RedisConfigurationSource _source;
	private readonly CancellationTokenSource _lifetime = new();
	private readonly object _sync = new();
	private RedisService _redis;
	private IDistributedCacheSubscription _subscription;
	private Task _loadTask;
	private TaskCompletionSource _reload;
	private TaskCompletionSource _disposal;
	private bool _reloadRequested;
	private volatile bool _loaded;
	#endregion

	#region 构造函数
	public RedisConfigurationProvider(RedisConfigurationSource source) : this(source, null) { }
	internal RedisConfigurationProvider(RedisConfigurationSource source, RedisService redis)
	{
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_redis = redis;
	}
	#endregion

	#region 内部属性
	internal bool IsLoaded => _loaded;
	internal Task SubscriptionTask
	{
		get
		{
			lock(_sync)
				return _loadTask ?? Task.CompletedTask;
		}
	}
	#endregion

	#region 加载方法
	/// <summary>读取已经异步加载的快照；首次加载必须等待 LoadAsync。</summary>
	public override void Load()
	{
		if(!_loaded)
			throw new InvalidOperationException(Properties.Resources.Configuration_AsyncLoadRequired_Message);
	}

	/// <summary>异步加载配置与变更订阅，并将完整快照发布到配置系统。</summary>
	/// <param name="cancellation">取消标记。</param>
	/// <returns>加载和订阅完成的任务。</returns>
	public ValueTask LoadAsync(CancellationToken cancellation = default)
	{
		lock(_sync)
		{
			ObjectDisposedException.ThrowIf(_disposal != null, this);
			_loadTask = this.LoadCoreAsync(_loadTask ?? Task.CompletedTask, cancellation);
			return new(_loadTask);
		}
	}

	private async Task LoadCoreAsync(Task previous, CancellationToken cancellation)
	{
		await previous.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
		using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _lifetime.Token);
		source.Token.ThrowIfCancellationRequested();
		var redis = _redis ??= RedisServiceProvider.GetRedis(_source.Name);
		var data = await redis.GetConfigurationAsync(_source.Namespace, source.Token);

		if(_subscription == null)
		{
			var options = new DistributedCacheSubscriptionOptions(_source.Namespace, DistributedCacheNotificationKind.All);
			_subscription = await redis.SubscribeAsync(Handler.Handle<DistributedCacheNotification>(this.OnNotificationAsync), options, _lifetime.Token);
			//订阅建立后再次读取，覆盖首次读取与订阅建立之间的变化。
			data = await redis.GetConfigurationAsync(_source.Namespace, source.Token);
		}

		source.Token.ThrowIfCancellationRequested();
		this.Data = data;
		_loaded = true;
		this.OnReload();
	}
	#endregion

	#region 通知方法
	private ValueTask OnNotificationAsync(DistributedCacheNotification notification, CancellationToken cancellation)
	{
		if(!string.Equals(notification.Key, _source.Namespace, StringComparison.Ordinal) &&
		   !notification.Key.StartsWith(_source.Namespace + ":", StringComparison.Ordinal))
			return ValueTask.CompletedTask;

		TaskCompletionSource reload = null;

		lock(_sync)
		{
			if(_disposal != null)
				return ValueTask.CompletedTask;

			_reloadRequested = true;
			if(_reload == null)
				_reload = reload = new(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		if(reload != null)
			_ = this.ReloadAsync(reload);

		return ValueTask.CompletedTask;
	}

	private async Task ReloadAsync(TaskCompletionSource completion)
	{
		try
		{
			while(true)
			{
				await Task.Delay(TimeSpan.FromMilliseconds(50), _lifetime.Token);

				lock(_sync)
					_reloadRequested = false;

				await this.LoadAsync(_lifetime.Token);

				lock(_sync)
				{
					if(_reloadRequested && _disposal == null)
						continue;

					_reload = null;
					completion.TrySetResult();
					return;
				}
			}
		}
		catch(OperationCanceledException) when(_lifetime.IsCancellationRequested) { completion.TrySetResult(); }
		catch(ObjectDisposedException) when(_lifetime.IsCancellationRequested) { completion.TrySetResult(); }
		catch(Exception exception)
		{
			try { await Diagnostics.Logging.GetLogging(typeof(RedisConfigurationProvider)).ErrorAsync(exception); }
			catch(Exception logging) { exception = new AggregateException(exception, logging); }

			completion.TrySetException(exception);
			_ = completion.Task.Exception;
		}
		finally
		{
			lock(_sync)
			{
				if(ReferenceEquals(_reload, completion))
					_reload = null;
			}
		}
	}
	#endregion

	#region 释放方法
	/// <summary>停止重载、等待加载与订阅退出；Redis 服务仍由服务容器或调用方拥有。</summary>
	/// <returns>释放完成的共享任务。</returns>
	public ValueTask DisposeAsync()
	{
		TaskCompletionSource completion;
		Task loading;
		Task reloading;
		bool execute;

		lock(_sync)
		{
			execute = _disposal == null;
			completion = _disposal ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
			loading = _loadTask;
			reloading = _reload?.Task;
		}

		if(execute)
			_ = this.DisposeCoreAsync(completion, loading, reloading);

		return new(completion.Task);
	}

	private async Task DisposeCoreAsync(TaskCompletionSource completion, Task loading, Task reloading)
	{
		List<Exception> errors = null;

		try
		{
			try { await _lifetime.CancelAsync(); }
			catch(Exception exception) { (errors ??= []).Add(exception); }

			foreach(var task in new[] { loading, reloading })
			{
				if(task == null)
					continue;

				try { await task; }
				catch(OperationCanceledException) when(_lifetime.IsCancellationRequested) { }
				catch(Exception exception) { (errors ??= []).Add(exception); }
			}

			try
			{
				if(_subscription != null)
					await _subscription.DisposeAsync();
			}
			catch(Exception exception) { (errors ??= []).Add(exception); }

			_lifetime.Dispose();
			GC.SuppressFinalize(this);

			if(errors != null)
				completion.TrySetException(new AggregateException(errors));
			else
				completion.TrySetResult();
		}
		catch(Exception exception) { completion.TrySetException(exception); }
	}
	#endregion
}
