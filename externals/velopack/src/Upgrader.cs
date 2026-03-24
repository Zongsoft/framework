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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Velopack library.
 *
 * The Zongsoft.Externals.Velopack is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Velopack is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Velopack library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Diagnostics;

using Velopack;

namespace Zongsoft.Externals.Velopack;

public sealed class Upgrader : WorkerBase
{
	#region 常量定义
	const int IDLE_STATE = 0;
	const int UPGRADING_STATE = 1;
	#endregion

	#region 私有字段
	private volatile int _upgrading;
	private UpdateManager _manager;
	private readonly Common.Timer _timer;
	#endregion

	#region 构造函数
	public Upgrader()
	{
		_timer = new(TimeSpan.FromMinutes(1), (_, cancellation) => this.UpgradeAsync(TimeSpan.Zero, cancellation));
	}
	#endregion

	#region 重写方法
	protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		var settings = Configuration.VelopackConnectionSettingsDriver.GetConnectionSettings();
		if(settings == null)
		{
			Logging.GetLogging(this).Error($"No upgrade configuration was found for the '{ApplicationContext.Current.Name}' application.");
			return Task.CompletedTask;
		}

		var source = VelopackSourceFactory.Create(settings);
		_manager = new(source, Utility.GetOptions(settings));

		_timer.Period = Utility.GetPeriod(settings);
		_timer.Start(args, cancellation);

		//如果升级检测周期过长则开启一个短延迟的升级操作
		if(_timer.Period >= TimeSpan.FromMinutes(5))
			Task.Run(() => this.UpgradeAsync(TimeSpan.FromSeconds(30), cancellation), cancellation);

		return Task.CompletedTask;
	}

	protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		_timer.Stop();
		_manager = null;
		return Task.CompletedTask;
	}
	#endregion

	#region 公共方法
	public async ValueTask UpgradeAsync(TimeSpan delay, CancellationToken cancellation)
	{
		if(delay > TimeSpan.Zero)
			await Task.Delay(delay, cancellation);

		var manager = _manager;
		if(manager == null)
			return;

		//如果当前升级程序未运行在安装模式则不支持进行升级
		if(string.IsNullOrEmpty(manager.AppId) || !manager.IsInstalled)
			return;

		try
		{
			//设置升级标志
			var state = Interlocked.CompareExchange(ref _upgrading, UPGRADING_STATE, IDLE_STATE);

			//如果当前处于升级中则退出
			if(state == UPGRADING_STATE)
				return;

			//检查升级更新
			var info = await manager.CheckForUpdatesAsync();
			if(info == null)
				return;

			//下载升级安装包
			await manager.DownloadUpdatesAsync(info, null, cancellation);

			//应用升级安装并重启程序
			manager.ApplyUpdatesAndRestart(info);
		}
		catch(Exception ex)
		{
			await Logging.GetLogging<Upgrader>().ErrorAsync(ex, cancellation);
		}
		finally
		{
			Interlocked.Exchange(ref _upgrading, IDLE_STATE);
		}
	}
	#endregion
}
