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
 * This file is part of Zongsoft.Externals.Hangfire library.
 *
 * The Zongsoft.Externals.Hangfire is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Hangfire is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Hangfire library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Hangfire;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Configuration;
using Zongsoft.Configuration.Options;

namespace Zongsoft.Externals.Hangfire;

[System.Reflection.DefaultMember(nameof(Handlers))]
public class Server : WorkerBase
{
	#region 成员字段
	private JobStorage _storage;
	private BackgroundJobServer _server;
	#endregion

	#region 构造函数
	public Server()
	{
		this.CanPauseAndContinue = false;
		this.Handlers = new Dictionary<string, IHandler>(StringComparer.OrdinalIgnoreCase);
	}

	public Server(string name) : base(name)
	{
		this.CanPauseAndContinue = false;
		this.Handlers = new Dictionary<string, IHandler>(StringComparer.OrdinalIgnoreCase);
	}
	#endregion

	#region 公共属性
	[Options(ServerOptions.PATH)]
	public ServerOptions Options { get; set; }
	public IDictionary<string, IHandler> Handlers { get; }

	public JobStorage Storage
	{
		get => _storage ??= ApplicationContext.Current.Services.Resolve<JobStorage>();
		set => _storage = value ?? throw new ArgumentNullException(nameof(value));
	}
	#endregion

	#region 重写方法
	protected override Task OnStartAsync(string[] args, CancellationToken cancellation = default)
	{
		_server = new BackgroundJobServer(GetOptions(this.Name, this.Options), this.Storage ?? JobStorage.Current);
		return Task.CompletedTask;
	}

	protected override Task OnStopAsync(string[] args, CancellationToken cancellation = default)
	{
		var server = Interlocked.Exchange(ref _server, null);
		server?.Dispose();
		return Task.CompletedTask;
	}
	#endregion

	#region 私有方法
	static BackgroundJobServerOptions GetOptions(string name, ServerOptions options)
	{
		var result = new BackgroundJobServerOptions()
		{
			ServerName = string.Equals(name, nameof(Server)) ? null : $"{name}@{Environment.MachineName}",
			SchedulePollingInterval = TimeSpan.FromSeconds(10),
		};

		if(options == null)
			return result;

		if(options.Queues != null && options.Queues.Length > 0)
			result.Queues = options.Queues;
		if(options.WorkerCount > 0)
			result.WorkerCount = options.WorkerCount;
		if(options.StopTimeout > TimeSpan.Zero)
			result.StopTimeout = options.StopTimeout;
		if(options.ShutdownTimeout > TimeSpan.Zero)
			result.ShutdownTimeout = options.ShutdownTimeout;
		if(options.ScheduleInterval > TimeSpan.Zero)
			result.SchedulePollingInterval = options.ScheduleInterval;
		if(options.HeartbeatInterval > TimeSpan.Zero)
			result.HeartbeatInterval = options.HeartbeatInterval;
		if(options.CheckInterval > TimeSpan.Zero)
			result.ServerCheckInterval = options.CheckInterval;
		if(options.ServerTimeout > TimeSpan.Zero)
			result.ServerTimeout = options.ServerTimeout;

		return result;
	}
	#endregion
}
