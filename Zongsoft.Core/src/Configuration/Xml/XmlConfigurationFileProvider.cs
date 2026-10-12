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
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Zongsoft.Configuration.Xml;

//将有效导入的通知接到根文件的变化令牌，仍由 FileConfigurationProvider 完成重载及异常处理。
internal sealed class XmlConfigurationFileProvider(IFileProvider provider) : IFileProvider, IDisposable
{
	private readonly object _sync = new();
	private readonly Dictionary<XmlConfigurationSession.Source, Subscription> _subscriptions = [];
	private CancellationTokenSource _changeTokenSource = new();
	private bool _disposed;

	internal IFileProvider Provider { get; } = provider;

	public IDirectoryContents GetDirectoryContents(string subpath) => this.Provider.GetDirectoryContents(subpath);
	public IFileInfo GetFileInfo(string subpath)
	{
		var file = this.Provider.GetFileInfo(subpath);
		if(!file.Exists)
			this.UpdateDependencies([]);
		return file;
	}

	public IChangeToken Watch(string filter)
	{
		var rootToken = this.Provider.Watch(filter);

		lock(_sync)
			return _disposed ? NullChangeToken.Singleton : new CompositeChangeToken([rootToken, new CancellationChangeToken(_changeTokenSource.Token)]);
	}

	internal void UpdateDependencies(HashSet<XmlConfigurationSession.Source> dependencies)
	{
		List<Subscription> removedSubscriptions = [];
		List<XmlConfigurationSession.Source> addedSources;

		lock(_sync)
		{
			if(_disposed)
				return;

			foreach(var entry in _subscriptions.ToArray())
			{
				if(!dependencies.Contains(entry.Key))
				{
					_subscriptions.Remove(entry.Key);
					removedSubscriptions.Add(entry.Value);
				}
			}

			addedSources = dependencies.Where(source => !_subscriptions.ContainsKey(source)).ToList();
		}

		//取消订阅可能等待正在执行的回调，不在内部锁中释放。
		foreach(var subscription in removedSubscriptions)
			subscription.Dispose();

		foreach(var source in addedSources)
		{
			var subscription = new Subscription(source, this.Provider, this.Signal);

			lock(_sync)
			{
				if(!_disposed && _subscriptions.TryAdd(source, subscription))
					continue;
			}

			subscription.Dispose();
		}
	}

	private void Signal()
	{
		CancellationTokenSource previousTokenSource;

		lock(_sync)
		{
			if(_disposed)
				return;
			previousTokenSource = _changeTokenSource;
			_changeTokenSource = new();
		}

		try
		{
			previousTokenSource.Cancel();
		}
		finally
		{
			previousTokenSource.Dispose();
		}
	}

	public void Dispose()
	{
		Subscription[] subscriptions;
		CancellationTokenSource changeTokenSource;

		lock(_sync)
		{
			if(_disposed)
				return;
			_disposed = true;
			subscriptions = _subscriptions.Values.ToArray();
			_subscriptions.Clear();
			changeTokenSource = _changeTokenSource;
		}

		foreach(var subscription in subscriptions)
			subscription.Dispose();

		//基类先解除加载回调，再触发组合令牌释放对根文件令牌的内部订阅。
		try
		{
			changeTokenSource.Cancel();
		}
		finally
		{
			changeTokenSource.Dispose();
		}
	}

	private sealed class Subscription : IDisposable
	{
		private readonly PhysicalFileProvider _ownedProvider;
		private readonly IDisposable _registration;

		public Subscription(XmlConfigurationSession.Source source, IFileProvider rootProvider, Action onChanged)
		{
			var provider = source.Provider;
			var path = source.Path;

			if(provider == null && rootProvider is PhysicalFileProvider physicalProvider)
			{
				var relativePath = Path.GetRelativePath(physicalProvider.Root, path).Replace('\\', '/');
				if(!Path.IsPathRooted(relativePath) && relativePath != ".." && !relativePath.StartsWith("../", StringComparison.Ordinal))
				{
					provider = physicalProvider;
					path = relativePath;
				}
			}

			if(provider == null)
			{
				var directory = Path.GetDirectoryName(path);
				while(!Directory.Exists(directory))
				{
					directory = Path.GetDirectoryName(directory);
					if(string.IsNullOrEmpty(directory))
						return;
				}

				_ownedProvider = new PhysicalFileProvider(directory);
				provider = _ownedProvider;
				path = Path.GetRelativePath(directory, path).Replace('\\', '/');
			}

			try
			{
				_registration = ChangeToken.OnChange(() => provider.Watch(path), onChanged);
			}
			catch
			{
				_ownedProvider?.Dispose();
				throw;
			}
		}

		public void Dispose()
		{
			_registration?.Dispose();
			_ownedProvider?.Dispose();
		}
	}
}
