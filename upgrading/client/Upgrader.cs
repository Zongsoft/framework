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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Upgrading;

public partial class Upgrader
{
	private const string BOOTSTRAP = ".bootstrap";

	internal static bool IsBootstrap() => IsBootstrap(out _);
	internal static bool IsBootstrap(out FileInfo bootstrap)
	{
		bootstrap = new FileInfo(Path.Combine(Utility.ApplicationPath, BOOTSTRAP));

		if(bootstrap.Exists)
		{
			using var reader = bootstrap.OpenText();
			var version = Utility.GetVersion(reader, out var name);

			if(version != null && version > Utility.ApplicationVersion && string.Equals(Utility.ApplicationName, name, StringComparison.OrdinalIgnoreCase))
				return true;
		}

		bootstrap = null;
		return false;
	}

	public static ValueTask<bool> UpgradeAsync(CancellationToken cancellation = default) => UpgradeAsync(null, cancellation);
	public static async ValueTask<bool> UpgradeAsync(string channel, CancellationToken cancellation = default)
	{
		//如果当前应用的升级程序正待引导，返回成功
		if(IsBootstrap())
			return true;

		//从指定通道获取升级发布信息并下载对应的升级包
		var info = await Fetcher.FetchAsync(channel, cancellation);
		if(info == null)
			return false;

		//从升级信息中提取解压升级包文件
		var version = await Extractor.ExtractAsync(info.Manifest, info.FilePath, cancellation);
		if(string.IsNullOrEmpty(version))
			return false;

		//创建本次升级的引导文件：一个指向展开后的待升级部署目录内的版本号文件
		return File.CreateSymbolicLink(Path.Combine(Utility.ApplicationPath, BOOTSTRAP), version).Exists;
	}

	public static void Restart()
	{
		const string LAUNCH = "upgrader.exe";

		//确保升级程序的引导文件存在
		if(!IsBootstrap(out var bootstrap))
			return;

		//定义升级程序的启动器的路径
		var launcer = Path.Combine(Utility.ApplicationPath, ".upgrader", LAUNCH);

		//确保升级程序的启动器是存在
		if(File.Exists(launcer))
		{
			var info = new ProcessStartInfo(launcer)
			{
				WindowStyle = ProcessWindowStyle.Minimized,
				WorkingDirectory = Utility.ApplicationPath,
			};

			//设置启动器的参数集
			info.ArgumentList.Add($"process={Environment.ProcessId}");
			info.ArgumentList.Add($"bootstrap={bootstrap.FullName}");

			//以独占锁的方式打开引导文件
			using var locking = bootstrap.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);

			//启动升级程序启动器
			var process = Process.Start(info);

			//如果升级程序启动器启动成功则退出当前进程
			if(process != null)
				Exit(locking);
		}
	}

	private static void Exit(FileStream locking, TimeSpan timeout = default)
	{
		//确保超时时长有效
		if(timeout <= TimeSpan.Zero)
			timeout = TimeSpan.FromSeconds(30);

		var host = Services.ApplicationContext.Current?.Services.GetService<IHost>();

		try
		{
			if(host != null)
			{
				host.StopAsync(timeout).Wait();
				host.WaitForShutdown();
			}
		}
		finally
		{
			//释放主机
			host?.Dispose();

			//释放被锁定的文件流
			locking?.Dispose();

			//退出当前应用
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
			//如果升级成功则重启应用程序
			if(await UpgradeAsync(this.Channel, cancellation))
				Restart();
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