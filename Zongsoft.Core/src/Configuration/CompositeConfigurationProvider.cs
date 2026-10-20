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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration;

public class CompositeConfigurationProvider : ICompositeConfigurationProvider, IConfigurationProvider, IDisposable, IAsyncDisposable
{
	#region 私有变量
	private IConfigurationProvider[] _providers;
	private readonly object _sync = new();
	private TaskCompletionSource _disposal;
	private CompositeChangeToken _reloadToken;
	#endregion

	#region 构造函数
	public CompositeConfigurationProvider(IReadOnlyCollection<IConfigurationProvider> providers)
	{
		if(providers == null)
			throw new ArgumentNullException(nameof(providers));

		int index = providers.Count;
		_providers = new IConfigurationProvider[providers.Count];

		//注意：必须反转配置提供程序
		foreach(var provider in providers)
			_providers[--index] = provider;

		_reloadToken = new CompositeChangeToken(providers.Select(p => p.GetReloadToken()).ToArray());
	}
	#endregion

	#region 公共属性
	public IEnumerable<IConfigurationProvider> Providers => _providers;
	#endregion

	#region 公共方法
	public IChangeToken GetReloadToken() => _reloadToken;
	public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
	{
		return _providers
			.SelectMany(p => p.GetChildKeys(Enumerable.Empty<string>(), parentPath))
			.Concat(earlierKeys)
			.OrderBy(k => k, ConfigurationKeyComparer.Instance);
	}

	public void Load()
	{
		var providers = _providers;

		for(int i = 0; i < providers.Length; i++)
			providers[i].Load();
	}

	public void Set(string key, string value)
	{
		var providers = _providers;

		for(int i = 0; i < providers.Length; i++)
			providers[i].Set(key, value);
	}

	public bool TryGet(string key, out string value)
	{
		var providers = _providers;

		for(int i = 0; i < providers.Length; i++)
		{
			if(providers[i].TryGet(key, out value))
				return true;
		}

		value = null;
		return false;
	}
	#endregion

	#region 重写方法
	public override string ToString() => string.Join(Environment.NewLine, _providers.Select(provider => provider.ToString()));
	#endregion

	#region 处置方法
	/// <summary>同步释放同步配置源；含有仅支持异步释放的配置源时必须使用 DisposeAsync。</summary>
	public void Dispose()
	{
		lock(_sync)
		{
			if(_disposal != null)
			{
				if(!_disposal.Task.IsCompleted)
					throw new InvalidOperationException(Properties.Resources.Disposal_AsyncRequired_Message);
				return;
			}

			if(_providers.Any(provider => provider is IAsyncDisposable && provider is not IDisposable))
				throw new InvalidOperationException(Properties.Resources.Disposal_AsyncRequired_Message);

			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if(!disposing)
			return;

		var providers = Interlocked.Exchange(ref _providers, []);
		List<Exception> errors = null;

		foreach(var provider in providers)
		{
			try { (provider as IDisposable)?.Dispose(); }
			catch(Exception exception) { (errors ??= []).Add(exception); }
		}

		if(errors != null)
			throw new AggregateException(errors);
	}

	/// <summary>等待所有子配置源释放，并传播清理故障；并发调用共享同一结果。</summary>
	/// <returns>所有子配置源释放完成的任务。</returns>
	public ValueTask DisposeAsync()
	{
		TaskCompletionSource completion;
		bool execute;

		lock(_sync)
		{
			execute = _disposal == null;
			completion = _disposal ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		if(execute)
			_ = this.DisposeCoreAsync(completion);

		return new(completion.Task);
	}

	protected virtual async ValueTask DisposeAsync(bool disposing)
	{
		if(!disposing)
			return;

		var providers = Interlocked.Exchange(ref _providers, []);
		List<Exception> errors = null;

		foreach(var provider in providers)
		{
			try
			{
				if(provider is IAsyncDisposable asynchronous)
					await asynchronous.DisposeAsync().ConfigureAwait(false);
				else if(provider is IDisposable synchronous)
					synchronous.Dispose();
			}
			catch(Exception exception) { (errors ??= []).Add(exception); }
		}

		if(errors != null)
			throw new AggregateException(errors);
	}

	private async Task DisposeCoreAsync(TaskCompletionSource completion)
	{
		try
		{
			await this.DisposeAsync(true).ConfigureAwait(false);
			GC.SuppressFinalize(this);
			completion.TrySetResult();
		}
		catch(Exception exception)
		{
			completion.TrySetException(exception);
			_ = completion.Task.Exception;
		}
	}
	#endregion
}
