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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

using Zongsoft.Plugins;
using Zongsoft.Services;

namespace Zongsoft.Configuration.Plugins
{
	public class OptionInitializer : Zongsoft.Services.IApplicationInitializer, IDisposable
	{
		#region 私有变量
		private PluginLoader _loader;
		#endregion

		#region 初始化器
		public void Initialize(PluginApplicationContext context)
		{
			_loader = context.PluginTree.Loader;

			_loader.PluginLoaded += Loader_PluginLoaded;
			_loader.PluginUnloaded += Loader_PluginUnloaded;
		}

		void IApplicationInitializer.Initialize(IApplicationContext context)
		{
			if(context is PluginApplicationContext ctx)
				this.Initialize(ctx);
		}
		#endregion

		#region 释放处置
		public void Dispose()
		{
			var loader = System.Threading.Interlocked.Exchange(ref _loader, null);

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
			var filePath = Path.GetDirectoryName(e.Plugin.FilePath);
			var fileName = Path.GetFileNameWithoutExtension(e.Plugin.FilePath);

			var configurator = new ConfigurationBuilder()
				.AddOptionFile(Path.Combine(filePath, $"{fileName}.option"), true)
				.AddOptionFile(Path.Combine(filePath, $"{fileName}.{e.Plugin.PluginTree.ApplicationContext.Environment.Name}.option"), true)
				.AddConfiguration(e.Plugin.PluginTree.ApplicationContext.Configuration);

			configurator.Build();
		}

		private void Loader_PluginUnloaded(object sender, PluginUnloadedEventArgs e)
		{
		}
		#endregion
	}
}
