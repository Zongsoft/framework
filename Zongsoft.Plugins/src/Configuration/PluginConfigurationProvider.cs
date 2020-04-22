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

namespace Zongsoft.Plugins.Configuration
{
	public class PluginConfigurationProvider : IConfigurationProvider, IDisposable
	{
		#region 成员字段
		private readonly PluginConfigurationSource _source;
		private ConfigurationReloadToken _reloadToken;
		private readonly ConcurrentDictionary<Plugin, Zongsoft.Configuration.CompositeConfigurationProvider> _providers;
		#endregion

		#region 构造函数
		public PluginConfigurationProvider(PluginConfigurationSource source)
		{
			_source = source ?? throw new ArgumentNullException(nameof(source));
			_reloadToken = new ConfigurationReloadToken();
			_providers = new ConcurrentDictionary<Plugin, Zongsoft.Configuration.CompositeConfigurationProvider>();

			_source.Loader.PluginLoaded += Loader_PluginLoaded;
			_source.Loader.PluginUnloaded += Loader_PluginUnloaded;
		}
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

		public virtual void Load()
		{
			_providers.Clear();

			foreach(var plugin in _source.GetPlugins())
			{
				this.LoadOptionFile(plugin);
			}
		}

		public virtual IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
		{
			return _providers.Values
				.SelectMany(p => p.GetChildKeys(null, parentPath))
				.Concat(earlierKeys)
				.OrderBy(k => k, ConfigurationKeyComparer.Instance);
		}

		public IChangeToken GetReloadToken()
		{
			return _reloadToken;
		}
		#endregion

		#region 释放处置
		public void Dispose()
		{
			var loader = _source.Loader;

			if(loader != null)
			{
				loader.PluginLoaded -= Loader_PluginLoaded;
				loader.PluginUnloaded -= Loader_PluginUnloaded;
			}
		}
		#endregion

		#region 事件处理
		private void Loader_PluginLoaded(object sender, PluginLoadedEventArgs e)
		{
			this.LoadOptionFile(e.Plugin);
		}

		private void Loader_PluginUnloaded(object sender, PluginUnloadedEventArgs e)
		{
			this.UnloadOptionFile(e.Plugin);
		}
		#endregion

		#region 私有方法
		private void LoadOptionFile(Plugin plugin)
		{
			var filePath = Path.GetDirectoryName(plugin.FilePath);
			var fileName = Path.GetFileNameWithoutExtension(plugin.FilePath);

			var providers = new []
			{
				new Zongsoft.Configuration.Xml.XmlConfigurationProvider(
					new Zongsoft.Configuration.Xml.XmlConfigurationSource()
					{
						Path = Path.Combine(filePath, $"{fileName}.option"),
						Optional = true,
					}),
				new Zongsoft.Configuration.Xml.XmlConfigurationProvider(
					new Zongsoft.Configuration.Xml.XmlConfigurationSource()
					{
						Path = Path.Combine(filePath, $"{fileName}.{plugin.Context.ApplicationContext.Environment.Name}.option"),
						Optional = true,
					})
			};

			_providers.TryAdd(plugin, new Zongsoft.Configuration.CompositeConfigurationProvider(providers));
		}

		private void UnloadOptionFile(Plugin plugin)
		{
			if(_providers.TryRemove(plugin, out var provider))
				provider.Dispose();
		}
		#endregion
	}
}
