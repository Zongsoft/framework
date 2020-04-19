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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Plugins.Hosting
{
	public class PluginsHostBuilder : IPluginsHostBuilder
	{
		#region 构造函数
		public PluginsHostBuilder(IHostBuilder builder, PluginOptions options)
		{
			this.Builder = builder ?? throw new ArgumentNullException(nameof(builder));
			this.Options = options ?? throw new ArgumentNullException(nameof(options));
		}
		#endregion

		#region 公共属性
		public IHostBuilder Builder { get; }

		public PluginOptions Options { get; }
		#endregion

		public IPluginsHostBuilder ConfigureConfiguration(Action<PluginsHostBuilder, IConfigurationBuilder> configure)
		{
			throw new NotImplementedException();
		}

		public IPluginsHostBuilder ConfigureServices(Action<PluginsHostBuilder, IServiceCollection> configureServices)
		{
			throw new NotImplementedException();
		}

		public IPluginsHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
		{
			throw new NotImplementedException();
		}

		public string GetSetting(string key)
		{
			throw new NotImplementedException();
		}

		public IPluginsHostBuilder UseSetting(string key, string value)
		{
			throw new NotImplementedException();
		}

	}
}
