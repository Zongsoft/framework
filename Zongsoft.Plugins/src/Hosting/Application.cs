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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
