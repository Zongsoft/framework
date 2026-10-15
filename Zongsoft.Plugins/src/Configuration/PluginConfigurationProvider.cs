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
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration;

public class PluginConfigurationProvider : ICompositeConfigurationProvider, IConfigurationProvider, IDisposable
{
	#region 成员字段
	private bool _disposed;
	private readonly object _sync = new();
	private readonly Plugins.PluginTree _pluginTree;
	private ConfigurationReloadToken _reloadToken;
	private readonly ConcurrentDictionary<Plugins.Plugin, OptionProvider> _providers;
	#endregion

	#region 构造函数
	public PluginConfigurationProvider(PluginConfigurationSource source)
	{
		ArgumentNullException.ThrowIfNull(source);
		_reloadToken = new ConfigurationReloadToken();
		_providers = new ConcurrentDictionary<Plugins.Plugin, OptionProvider>();

		_pluginTree = Plugins.PluginTree.Get(source.Options);
		_pluginTree.Loader.PluginLoaded += this.PluginLoader_PluginLoaded;
		_pluginTree.Loader.PluginUnloaded += this.PluginLoader_PluginUnloaded;
	}
	#endregion

	#region 公共属性
	/// <summary>获取各插件入口及其显式导入的配置提供程序，不保证跨插件的枚举顺序。</summary>
	public IEnumerable<IConfigurationProvider> Providers => _providers.Values.Select(provider => provider.Configuration);
	#endregion

	#region 公共方法
	public bool TryGet(string key, out string value)
	{
		foreach(var provider in _providers.Values)
		{
			if(provider.Configuration.TryGet(key, out value))
				return true;
		}

		value = null;
		return false;
	}

	public void Set(string key, string value)
	{
		foreach(var provider in _providers.Values)
		{
			if(provider.Configuration.TryGet(key, out _))
				provider.Configuration.Set(key, value);
		}
	}

	/// <summary>重新加载所有已加载插件及其子插件的配置，并通知配置发生变化。</summary>
	/// <remarks>调用方应串行执行整体重载、插件树加载或卸载及释放操作；重载失败会清理本次建立的提供程序。</remarks>
	/// <exception cref="ObjectDisposedException">当前提供程序已被释放。</exception>
	public void Load()
	{
		ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);
		this.Clear();

		try
		{
			foreach(var plugin in _pluginTree.Plugins)
				this.LoadOptionFiles(plugin);
		}
		catch
		{
			this.Clear();
			throw;
		}

		this.OnReload();
	}

	/// <summary>获取当前配置变更令牌，每次通知后均替换为新令牌。</summary>
	/// <returns>用于订阅下一次配置变更的令牌。</returns>
	public IChangeToken GetReloadToken() => Volatile.Read(ref _reloadToken);
	public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath) => _providers.Values
		.SelectMany(p => p.Configuration.GetChildKeys([], parentPath))
		.Concat(earlierKeys)
		.OrderBy(k => k, ConfigurationKeyComparer.Instance);
	#endregion

	#region 重写方法
	public override string ToString() => string.Join(Environment.NewLine, _providers.Select(provider => provider.ToString()));
	#endregion

	#region 事件处理
	private void PluginLoader_PluginLoaded(object sender, Zongsoft.Plugins.PluginLoadedEventArgs e)
	{
		if(this.LoadOptionFile(e.Plugin))
			this.OnReload();
	}

	private void PluginLoader_PluginUnloaded(object sender, Zongsoft.Plugins.PluginUnloadedEventArgs e)
	{
		if(_providers.TryRemove(e.Plugin, out var provider))
		{
			provider.Dispose();
			this.OnReload();
		}
	}
	#endregion

	#region 释放处置
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if(disposing)
		{
			lock(_sync)
			{
				if(_disposed)
					return;

				_disposed = true;
			}

			_pluginTree.Loader.PluginLoaded -= this.PluginLoader_PluginLoaded;
			_pluginTree.Loader.PluginUnloaded -= this.PluginLoader_PluginUnloaded;

			this.Clear();
		}
	}
	#endregion

	#region 私有方法
	private void Clear()
	{
		OptionProvider[] providers;

		lock(_sync)
		{
			providers = _providers.Values.ToArray();
			_providers.Clear();
		}

		foreach(var provider in providers)
			provider.Dispose();
	}

	private void OnReload()
	{
		if(!Volatile.Read(ref _disposed))
			Interlocked.Exchange(ref _reloadToken, new ConfigurationReloadToken()).OnReload();
	}

	private void LoadOptionFiles(Plugins.Plugin plugin)
	{
		if(plugin.Status != Plugins.PluginStatus.Loaded)
			return;

		this.LoadOptionFile(plugin);

		foreach(var child in plugin.Children)
			this.LoadOptionFiles(child);
	}

	private bool LoadOptionFile(Plugins.Plugin plugin)
	{
		if(Volatile.Read(ref _disposed) || _providers.ContainsKey(plugin))
			return false;

		var file = GetOptionFile(plugin);
		if(file == null)
			return false;

		var provider = OptionProvider.Load(file, this.OnReload);

		lock(_sync)
		{
			if(!_disposed && _providers.TryAdd(plugin, provider))
				return true;
		}

		provider.Dispose();
		return false;
	}

	private static string GetOptionFile(Plugins.Plugin plugin)
	{
		var path = Path.ChangeExtension(plugin.FilePath, ".option");
		if(File.Exists(path))
			return path;

		path = Path.Combine(Path.GetDirectoryName(plugin.FilePath), plugin.Name + ".option");
		return File.Exists(path) ? path : null;
	}
	#endregion

	#region 嵌套类型
	private sealed class OptionProvider : IDisposable
	{
		private int _disposed;
		private IDisposable _subscription;
		private OptionProvider(IConfigurationProvider provider) => this.Configuration = new([provider]);
		public CompositeConfigurationProvider Configuration { get; }
		public override string ToString() => this.Configuration.ToString();

		public static OptionProvider Load(string path, Action changed)
		{
			var provider = new Xml.XmlConfigurationSource()
			{
				Path = path,
				Optional = true,
				ReloadOnChange = true,
			}.Build(null);
			var result = new OptionProvider(provider);

			try
			{
				result.Configuration.Load();
				result._subscription = ChangeToken.OnChange(provider.GetReloadToken, changed);
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}

		public void Dispose()
		{
			if(Interlocked.Exchange(ref _disposed, 1) != 0)
				return;

			_subscription?.Dispose();
			this.Configuration.Dispose();
		}
	}
	#endregion
}
