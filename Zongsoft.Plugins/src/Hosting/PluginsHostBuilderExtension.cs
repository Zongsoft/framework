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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Plugins;

[Obsolete("Use Application and ApplicationBuilder instead.")]
public static class PluginsHostBuilderExtension
{
	public static IHostBuilder UsePluginsLifetime(this IHostBuilder builder)
	{
		return builder.ConfigureServices(services => services.AddSingleton<IHostLifetime, Hosting.PluginsHostLifetime>());
	}

	public static IHostBuilder ConfigurePlugins<TApplicationContext>(this IHostBuilder builder, Action<Hosting.IPluginsHostBuilder> configure = null) where TApplicationContext : PluginApplicationContext
	{
		return ConfigurePlugins<TApplicationContext>(builder, null, configure);
	}

	public static IHostBuilder ConfigurePlugins<TApplicationContext>(this IHostBuilder builder, string applicationName, Action<Hosting.IPluginsHostBuilder> configure = null) where TApplicationContext : PluginApplicationContext
	{
		if(builder == null)
			throw new ArgumentNullException(nameof(builder));

		if(typeof(TApplicationContext).IsAbstract)
			throw new ArgumentException($"The specified '{typeof(TApplicationContext).FullName}' application context type cannot be abstract.");

		var pluginsBuilder = new Hosting.PluginsHostBuilder(applicationName, builder);
		configure?.Invoke(pluginsBuilder);

		builder.ConfigureServices(services =>
		{
			services.AddSingleton<TApplicationContext>();
			services.AddSingleton<PluginApplicationContext>(srvs => srvs.GetRequiredService<TApplicationContext>());
			services.AddSingleton<Services.IApplicationContext>(srvs => srvs.GetRequiredService<TApplicationContext>());

			//挂载插件宿主生命期
			services.AddSingleton<IHostLifetime, Hosting.PluginsHostLifetime>();
		});

		return builder;
	}
}
