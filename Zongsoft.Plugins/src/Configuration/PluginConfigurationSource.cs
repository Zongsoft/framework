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
using System.Collections.Generic;

using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration.Plugins
{
	public class PluginConfigurationSource : IConfigurationSource, IDisposable
	{
		#region 单例字段
		public static readonly PluginConfigurationSource Instance = new PluginConfigurationSource();
		#endregion

		#region 成员字段
		private Zongsoft.Plugins.PluginLoader _loader;
		private readonly ConfigurationReloadToken _reloadToken;
		#endregion

		#region 构造函数
		private PluginConfigurationSource()
		{
			_reloadToken = new ConfigurationReloadToken();
		}
		#endregion

		#region 公共属性
		public IEnumerable<Zongsoft.Plugins.Plugin> Plugins
		{
			get => (IEnumerable<Zongsoft.Plugins.Plugin>)_loader?.Plugins ?? Array.Empty<Zongsoft.Plugins.Plugin>();
		}

		public Zongsoft.Plugins.PluginLoader Loader
		{
			get => _loader;
		}
		#endregion

		#region 公共方法
		public IConfigurationProvider Build(IConfigurationBuilder builder)
		{
			return new PluginConfigurationProvider(this, _reloadToken);
		}

		public void SetLoader(Zongsoft.Plugins.PluginLoader loader)
		{
			if(loader == null)
				throw new ArgumentNullException(nameof(loader));

			if(object.ReferenceEquals(loader, _loader))
				return;

			_loader.Loaded -= PluginLoader_Loaded;
			_loader.PluginLoaded -= PluginLoader_PluginLoaded;
			_loader.PluginUnloaded -= PluginLoader_PluginUnloaded;

			_loader = loader;

			_loader.Loaded += PluginLoader_Loaded;
			_loader.PluginLoaded += PluginLoader_PluginLoaded;
			_loader.PluginUnloaded += PluginLoader_PluginUnloaded;

			if(_loader.Plugins.Count > 0)
				_reloadToken.OnReload();
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			var loader = System.Threading.Interlocked.Exchange(ref _loader, null);

			if(loader != null)
			{
				loader.Loaded -= PluginLoader_Loaded;
				loader.PluginLoaded -= PluginLoader_PluginLoaded;
				loader.PluginUnloaded -= PluginLoader_PluginUnloaded;
			}
		}
		#endregion

		#region 事件处理
		private void PluginLoader_Loaded(object sender, Zongsoft.Plugins.PluginLoadEventArgs e)
		{
			_reloadToken.OnReload();
		}

		private void PluginLoader_PluginLoaded(object sender, Zongsoft.Plugins.PluginLoadedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void PluginLoader_PluginUnloaded(object sender, Zongsoft.Plugins.PluginUnloadedEventArgs e)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
