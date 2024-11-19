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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Externals.Hangfire
{
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
		public JobStorage Storage
		{
			get => _storage ??= ApplicationContext.Current.Services.Resolve<JobStorage>();
			set => _storage = value ?? throw new ArgumentNullException(nameof(value));
		}

		public IDictionary<string, IHandler> Handlers { get; }
		#endregion

		#region 重写方法
		protected override Task OnStartAsync(string[] args, CancellationToken cancellation = default)
		{
			_server = new BackgroundJobServer(new BackgroundJobServerOptions()
			{
				ServerName = string.Equals(this.Name, nameof(Server)) ? null : $"{this.Name}.{Environment.MachineName}",
				SchedulePollingInterval = TimeSpan.FromSeconds(5),
			}, this.Storage ?? JobStorage.Current);

			return Task.CompletedTask;
		}

		protected override Task OnStopAsync(string[] args, CancellationToken cancellation = default)
		{
			var server = Interlocked.Exchange(ref _server, null);
			server?.Dispose();
			return Task.CompletedTask;
		}
		#endregion
	}
}
