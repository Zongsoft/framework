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
using System.Collections.Generic;
using System.Collections.Concurrent;

using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration
{
	public class PluginConfigurationProvider : ICompositeConfigurationProvider, IConfigurationProvider, IDisposable
	{
		#region 成员字段
		private readonly PluginConfigurationSource _source;
		private readonly Zongsoft.Plugins.PluginTree _pluginTree;
		private readonly ConfigurationReloadToken _reloadToken;
		private readonly ConcurrentDictionary<Zongsoft.Plugins.Plugin, CompositeConfigurationProvider> _providers;
		#endregion

		#region 构造函数
		public PluginConfigurationProvider(PluginConfigurationSource source)
		{
			_source = source ?? throw new ArgumentNullException(nameof(source));
			_reloadToken = new ConfigurationReloadToken();
			_providers = new ConcurrentDictionary<Zongsoft.Plugins.Plugin, CompositeConfigurationProvider>();

			_pluginTree = Zongsoft.Plugins.PluginTree.Get(source.Options);
			_pluginTree.Loader.PluginLoaded += PluginLoader_PluginLoaded;
			_pluginTree.Loader.PluginUnloaded += PluginLoader_PluginUnloaded;
		}
		#endregion

		#region 公共属性
		public IEnumerable<IConfigurationProvider> Providers { get => _providers.Values; }
		#endregion

		#region 公共方法
		public bool TryGet(string key, out string value)
		{
			foreach(var provider in _providers.Values)
			{
				if(provider.TryGet(key, out value))
					return true;
			}

			value = null;
			return false;
		}

		public void Set(string key, string value)
		{
			foreach(var provider in _providers.Values)
			{
				if(provider.TryGet(key, out _))
					provider.Set(key, value);
			}
		}

		public void Load()
		{
			foreach(var plugin in _providers.Keys)
			{
				if(_providers.TryRemove(plugin, out var provider))
					provider.Dispose();
			}

			foreach(var plugin in _pluginTree.Plugins)
			{
				this.LoadOptionFile(plugin);
			}
		}

		public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
		{
			return _providers.Values
				.SelectMany(p => p.GetChildKeys(Enumerable.Empty<string>(), parentPath))
				.Concat(earlierKeys)
				.OrderBy(k => k, ConfigurationKeyComparer.Instance);
		}

		public IChangeToken GetReloadToken()
		{
			return _reloadToken;
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			return string.Join(Environment.NewLine, _providers.Select(provider => provider.ToString()));
		}
		#endregion

		#region 事件处理
		private void PluginLoader_PluginLoaded(object sender, Zongsoft.Plugins.PluginLoadedEventArgs e)
		{
			this.LoadOptionFile(e.Plugin);
		}

		private void PluginLoader_PluginUnloaded(object sender, Zongsoft.Plugins.PluginUnloadedEventArgs e)
		{
			if(_providers.TryRemove(e.Plugin, out var provider))
				provider.Dispose();
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
				_pluginTree.Loader.PluginLoaded -= PluginLoader_PluginLoaded;
				_pluginTree.Loader.PluginUnloaded -= PluginLoader_PluginUnloaded;

				foreach(var plugin in _providers.Keys)
				{
					if(_providers.TryRemove(plugin, out var provider))
						provider.Dispose();
				}
			}
		}
		#endregion

		#region 私有方法
		private bool LoadOptionFile(Zongsoft.Plugins.Plugin plugin)
		{
			if(_providers.ContainsKey(plugin))
				return false;

			var directory = Path.GetDirectoryName(plugin.FilePath);
			var fileName = Path.GetFileNameWithoutExtension(plugin.FilePath);
			var environment = _source.Options.EnvironmentName.ToLowerInvariant();
			List<IConfigurationProvider> providers = null;

			var optionFile = Path.Combine(directory, $"{fileName}.option");
			if(File.Exists(optionFile))
			{
				if(providers == null)
					providers = new List<IConfigurationProvider>(4);

				providers.Add(new Xml.XmlConfigurationSource()
				{
					Path = optionFile,
					Optional = true,
					ReloadOnChange = true,
				}.Build(null));
			}

			optionFile = Path.Combine(directory, $"{fileName}.{environment}.option");
			if(File.Exists(optionFile))
			{
				if(providers == null)
					providers = new List<IConfigurationProvider>(4);

				providers.Add(new Xml.XmlConfigurationSource()
				{
					Path = optionFile,
					Optional = true,
					ReloadOnChange = true,
				}.Build(null));
			}

			//获取当前目录下的当前环境的从属配置文件
			var slaves = GetOptionSlaveFiles(directory, fileName, _source.Options.EnvironmentName);

			if(slaves != null && slaves.Length > 0)
			{
				if(providers == null)
					providers = new List<IConfigurationProvider>(slaves.Length);

				for(int i = 0; i < slaves.Length; i++)
				{
					providers.Add(new Xml.XmlConfigurationSource()
					{
						Path = slaves[i],
						Optional = true,
						ReloadOnChange = true,
					}.Build(null));
				}
			}

			if(providers == null || providers.Count == 0)
				return false;

			var composite = new CompositeConfigurationProvider(providers);

			//加载配置文件
			composite.Load();

			return _providers.TryAdd(plugin, composite);
		}

		private static string[] GetOptionSlaveFiles(string directory, string fileName, string environment) =>
			Directory.GetFiles(directory, $"{fileName}.{environment}-*.option");
		#endregion
	}
}
