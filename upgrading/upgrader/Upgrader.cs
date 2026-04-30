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
 * This file is part of Zongsoft.Upgrading library.
 *
 * The Zongsoft.Upgrading is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Upgrading is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Upgrading library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Upgrading;

public partial class Upgrader
{
	public static ValueTask<bool> UpgradeAsync(CancellationToken cancellation = default) => UpgradeAsync(null, cancellation);
	public static async ValueTask<bool> UpgradeAsync(string channel, CancellationToken cancellation = default)
	{
		//如果当前应用的部署器正待部署，返回成功
		if(Deployer.HasDeployment())
			return true;

		//从指定通道获取升级发布信息并下载对应的升级包
		var info = await Fetcher.FetchAsync(channel, cancellation);
		if(info == null)
			return false;

		//从升级信息中提取解压升级包文件
		var directory = await Extractor.ExtractAsync(info.Manifest, info.FilePath, cancellation);
		if(string.IsNullOrEmpty(directory))
			return false;

		//创建本次升级的部署文件文件
		return Deployer.Deployment.Save(info.FilePath, directory) != null;
	}

	public static void Deploy()
	{
		//确保升级的部署文件存在
		if(!Deployer.HasDeployment(out var deployment))
		{
			Diagnostics.Logging.GetLogging<Upgrader>().Error("The deployment file was not found.");
			return;
		}

		//定义升级部署器程序的路径
		var deployer = Path.Combine(Application.ApplicationPath, Deployer.DIRECTORY, Deployer.FILENAME);

		//确保升级部署器程序是存在的
		if(!File.Exists(deployer))
		{
			Diagnostics.Logging.GetLogging<Upgrader>().Error($"The deployer program '{deployer}' does not exist.");
			return;
		}

		//以独占锁的方式打开部署文件
		using var locking = deployment.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);

		//启动升级部署器程序
		var process = Launch(deployer, deployment.FullName);

		//如果升级部署器程序启动成功则退出当前进程
		if(process != null)
		{
			//记录升级部署器程序的启动日志
			Diagnostics.Logging.GetLogging<Upgrader>().Info(
				$"The upgrader([{Environment.ProcessId}]{Environment.ProcessPath}) has completed(downloaded and extracted), and the deployer program has been launched.",
				Utility.GetProcessInfo(process));

			//关闭当前应用程序
			Shutdown(locking);
		}
	}

	private static void Shutdown(FileStream locking, TimeSpan timeout = default)
	{
		//确保等待超时有效
		if(timeout <= TimeSpan.Zero)
			timeout = TimeSpan.FromSeconds(30);

		//确保日志存储器落盘完成
		Diagnostics.Logging.Flush();

		//获取当前应用程序的主机接口
		var host = Services.ApplicationContext.Current?.Services.GetService<IHost>();

		try
		{
			if(host != null)
			{
				if(timeout > TimeSpan.Zero)
					host.StopAsync(timeout).GetAwaiter().GetResult();
				else
					host.StopAsync().GetAwaiter().GetResult();

				host.WaitForShutdown();
			}
		}
		finally
		{
			//释放主机
			host?.Dispose();

			//释放被锁定的文件流
			locking?.Dispose();

			//退出当前应用程序
			Environment.Exit(0);
		}
	}
}

partial class Upgrader : Zongsoft.Components.WorkerBase
{
	#region 常量定义
	const int IDLE_FLAG = 0;
	const int UPGRADING_FLAG = 1;
	#endregion

	#region 私有字段
	private volatile int _flag;
	private readonly Common.Timer _timer;
	#endregion

	#region 构造函数
	public Upgrader()
	{
		_timer = new(this.Period, (_, cancellation) => this.OnUpgradeAsync(TimeSpan.Zero, cancellation));
	}
	#endregion

	#region 公共属性
	public string Channel { get; set; }
	public TimeSpan Period { get => field > TimeSpan.Zero ? field : TimeSpan.FromMinutes(10); set; }
	#endregion

	#region 重写方法
	protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		_timer.Period = this.Period;
		_timer.Start(args, cancellation);

		//如果升级检测周期过长则开启一个短延迟的升级操作
		if(_timer.Period >= TimeSpan.FromMinutes(5))
			Task.Run(() => this.OnUpgradeAsync(TimeSpan.FromSeconds(10), cancellation), cancellation);

		return Task.CompletedTask;
	}

	protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		_timer.Stop();
		return Task.CompletedTask;
	}
	#endregion

	#region 私有方法
	private async ValueTask OnUpgradeAsync(TimeSpan delay, CancellationToken cancellation)
	{
		if(delay > TimeSpan.Zero)
			await Task.Delay(delay, cancellation);

		//设置升级标志
		var flag = Interlocked.CompareExchange(ref _flag, UPGRADING_FLAG, IDLE_FLAG);

		//如果当前处于升级中则退出
		if(flag == UPGRADING_FLAG)
			return;

		try
		{
			//如果升级成功则执行升级部署
			if(await UpgradeAsync(this.Channel, cancellation))
				Deploy();
		}
		catch(Exception ex)
		{
			await Zongsoft.Diagnostics.Logging.GetLogging<Upgrader>().ErrorAsync(ex, cancellation);
		}
		finally
		{
			Interlocked.Exchange(ref _flag, IDLE_FLAG);
		}
	}
	#endregion
}