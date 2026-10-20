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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace Zongsoft.Plugins.Hosting;

public static partial class Application
{
	#if NET7_0_OR_GREATER
	public static ValueTask<IHost> DaemonAsync(Action<HostApplicationBuilder> configure = null) => DaemonAsync(null, null, configure);
	public static ValueTask<IHost> DaemonAsync(string[] args, Action<HostApplicationBuilder> configure = null) => DaemonAsync(null, args, configure);
	public static ValueTask<IHost> DaemonAsync(string name, Action<HostApplicationBuilder> configure = null) => DaemonAsync(name, null, configure);
	public static ValueTask<IHost> DaemonAsync(string name, string[] args, Action<HostApplicationBuilder> configure = null)
	{
		var builder = new DaemonApplicationBuilder(name, args, configure);

		//加载宿主配置文件
		builder.LoadConfiguration();

		return builder.BuildAsync();
	}

	public static ValueTask<IHost> TerminalAsync(Action<HostApplicationBuilder> configure = null) => TerminalAsync(null, null, configure);
	public static ValueTask<IHost> TerminalAsync(string[] args, Action<HostApplicationBuilder> configure = null) => TerminalAsync(null, args, configure);
	public static ValueTask<IHost> TerminalAsync(string name, Action<HostApplicationBuilder> configure = null) => TerminalAsync(name, null, configure);
	public static ValueTask<IHost> TerminalAsync(string name, string[] args, Action<HostApplicationBuilder> configure = null)
	{
		var builder = new TerminalApplicationBuilder(name, args, configure);

		//加载宿主配置文件
		builder.LoadConfiguration();

		return builder.BuildAsync();
	}
	#else
	public static ValueTask<IHost> DaemonAsync(Action<IHostBuilder> configure = null) => DaemonAsync(null, null, configure);
	public static ValueTask<IHost> DaemonAsync(string[] args, Action<IHostBuilder> configure = null) => DaemonAsync(null, args, configure);
	public static ValueTask<IHost> DaemonAsync(string name, Action<IHostBuilder> configure = null) => DaemonAsync(name, null, configure);
	public static ValueTask<IHost> DaemonAsync(string name, string[] args, Action<IHostBuilder> configure = null)
	{
		var builder = new DaemonApplicationBuilder(name, args, configure);

		//加载宿主配置文件
		builder.LoadConfiguration();

		return builder.BuildAsync();
	}

	public static ValueTask<IHost> TerminalAsync(Action<IHostBuilder> configure = null) => TerminalAsync(null, null, configure);
	public static ValueTask<IHost> TerminalAsync(string[] args, Action<IHostBuilder> configure = null) => TerminalAsync(null, args, configure);
	public static ValueTask<IHost> TerminalAsync(string name, Action<IHostBuilder> configure = null) => TerminalAsync(name, null, configure);
	public static ValueTask<IHost> TerminalAsync(string name, string[] args, Action<IHostBuilder> configure = null)
	{
		var builder = new TerminalApplicationBuilder(name, args, configure);

		//加载宿主配置文件
		builder.LoadConfiguration();

		return builder.BuildAsync();
	}
	#endif
}
