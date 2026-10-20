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
	#region 静态方法
	/// <summary>通过默认通道准备升级包及部署描述文件。</summary>
	/// <param name="cancellation">取消升级准备的标记。</param>
	/// <returns>已有或成功创建部署描述文件时返回真，否则返回假。</returns>
	public static ValueTask<bool> UpgradeAsync(CancellationToken cancellation = default) => UpgradeAsync(null, cancellation);
	/// <summary>通过指定通道准备升级包及部署描述文件。</summary>
	/// <param name="channel">发布通道名称；为空时使用默认通道。</param>
	/// <param name="cancellation">取消升级准备的标记。</param>
	/// <returns>已有或成功创建部署描述文件时返回真，否则返回假。</returns>
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

	/// <summary>启动部署器，等待宿主停止后退出当前进程。</summary>
	/// <param name="cancellation">启动等待操作的取消标记。</param>
	/// <returns>部署文件或部署器不可用时完成；成功交接后当前进程退出。</returns>
	/// <remarks>部署器启动成功后刷新日志并请求宿主停止，最多等待三十秒后释放部署锁并退出。没有宿主上下文时直接退出。</remarks>
	public static async ValueTask DeployAsync(CancellationToken cancellation = default)
	{
		//确保升级的部署文件存在
		if(!Deployer.HasDeployment(out var deployment))
		{
			await Diagnostics.Logging.GetLogging<Upgrader>().ErrorAsync("The deployment file was not found.").ConfigureAwait(false);
			return;
		}

		//定义升级部署器程序的路径
		var deployer = Path.Combine(Application.ApplicationPath, Deployer.DIRECTORY, Deployer.FILENAME);

		//确保升级部署器程序是存在的
		if(!File.Exists(deployer))
		{
			await Diagnostics.Logging.GetLogging<Upgrader>().ErrorAsync($"The deployer program '{deployer}' does not exist.").ConfigureAwait(false);
			return;
		}

		//以独占锁的方式打开部署文件
		using var locking = deployment.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);

		//启动升级部署器程序
		using var process = await LaunchAsync(deployer, deployment.FullName, cancellation);

		//如果升级部署器程序启动成功则退出当前进程
		if(process != null)
		{
			//记录升级部署器程序的启动日志
			await Diagnostics.Logging.GetLogging<Upgrader>().InfoAsync(
				$"The upgrader([{Environment.ProcessId}]{Environment.ProcessPath}) has completed(downloaded and extracted), and the deployer program has been launched.",
				Utility.GetProcessInfo(process)).ConfigureAwait(false);

			//关闭当前应用程序
			await ShutdownAsync(locking).ConfigureAwait(false);
		}
	}

	/// <summary>刷新日志并等待宿主停止，最后释放部署锁并退出进程。</summary>
	/// <param name="locking">交接期间持有的部署文件锁。</param>
	/// <returns>宿主停止等待操作；结束时当前进程退出。</returns>
	private static async Task ShutdownAsync(FileStream locking)
	{
		//确保日志存储器落盘完成
		await Diagnostics.Logging.FlushAsync().ConfigureAwait(false);

		try
		{
			var context = Services.ApplicationContext.Current;
			var lifetime = context?.Services.GetService<IHostApplicationLifetime>();

			if(lifetime != null)
			{
				var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				using var registration = lifetime.ApplicationStopped.UnsafeRegister(
					static state => ((TaskCompletionSource)state).TrySetResult(), stopped);

				context.Exit(0);
				await stopped.Task.WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
			}
		}
		finally
		{
			//释放被锁定的文件流
			locking?.Dispose();

			//退出当前应用程序
			Environment.Exit(0);
		}
	}
	#endregion
}

partial class Upgrader : Zongsoft.Components.WorkerBase
{
	#region 常量定义
	private const int ACTIVE = 0;
	private const int STOPPING = 1;
	private const int DEPLOYING = 2;
	#endregion

	#region 私有字段
	private int _ending;
	private bool _initial;
	private bool _deployment;
	private Task _execution = Task.CompletedTask;
	private Common.Timer _timer;
	#endregion

	#region 公共属性
	public string Channel { get; set; }
	public TimeSpan Period { get => field > TimeSpan.Zero ? field : TimeSpan.FromMinutes(10); set; }
	#endregion

	#region 重写方法
	/// <summary>启动串行升级检查，并建立部署交接任务。</summary>
	/// <param name="args">工作器启动参数。</param>
	/// <param name="cancellation">工作器启动的取消标记；周期运行由工作器停止流程结束。</param>
	/// <returns>周期检查已经启动的任务。</returns>
	protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		Volatile.Write(ref _ending, ACTIVE);

		_deployment = false;
		_initial = this.Period >= TimeSpan.FromMinutes(5);
		var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_timer = new(_initial ? TimeSpan.FromSeconds(10) : this.Period, this.OnTickAsync);
		_timer.Stopped += (_, _) => stopped.TrySetResult();
		_timer.Start(args);
		_execution = this.RunAsync(_timer, stopped.Task);

		return Task.CompletedTask;
	}

	/// <summary>停止升级检查并释放计时器；部署交接已经开始时不反向等待该流程。</summary>
	/// <param name="args">工作器停止参数。</param>
	/// <param name="cancellation">收尾令牌，不随调用者取消等待。</param>
	/// <returns>本次检查及所属结束流程的停止任务。</returns>
	protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		var ending = Interlocked.CompareExchange(ref _ending, STOPPING, ACTIVE);
		var timer = _timer;
		if(timer == null)
			return;

		//部署交接取得所有权后负责关闭宿主，任何停止调用都不能反向等待该流程
		await Task.WhenAll(timer.DisposeAsync().AsTask(), ending == DEPLOYING ? Task.CompletedTask : _execution).ConfigureAwait(false);
		_timer = null;
		_execution = Task.CompletedTask;
	}
	#endregion

	#region 私有方法
	/// <summary>在计时器停止通知完成后执行部署交接。</summary>
	/// <param name="timer">本次运行使用的计时器。</param>
	/// <param name="stopped">由计时器停止事件完成的业务信号。</param>
	/// <returns>本次运行的部署交接任务。</returns>
	private async Task RunAsync(Common.Timer timer, Task stopped)
	{
		//停止通知只唤醒部署流程；退出全部计时器通知后才能关闭宿主。
		await stopped.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
		await timer.StopAsync().ConfigureAwait(false);

		if(!_deployment || Interlocked.CompareExchange(ref _ending, DEPLOYING, ACTIVE) != ACTIVE)
			return;

		try { await DeployAsync().ConfigureAwait(false); }
		finally { Volatile.Write(ref _ending, STOPPING); }
	}

	/// <summary>执行一轮升级准备；准备成功后请求结束周期运行。</summary>
	/// <param name="state">本次周期运行的启动状态。</param>
	/// <param name="cancellation">本次周期运行的取消标记。</param>
	/// <returns>本轮升级准备操作。</returns>
	private async ValueTask OnTickAsync(object state, CancellationToken cancellation)
	{
		if(_initial)
		{
			_initial = false;
			_timer.Period = this.Period;
		}

		try
		{
			//升级准备完成后结束周期运行，由所属任务执行部署及宿主关闭
			if(await UpgradeAsync(this.Channel, cancellation))
			{
				cancellation.ThrowIfCancellationRequested();
				_deployment = true;
				_timer.Stop();
			}
		}
		catch(OperationCanceledException exception) when(cancellation.IsCancellationRequested && exception.CancellationToken == cancellation)
		{
			throw;
		}
		catch(Exception ex)
		{
			await Zongsoft.Diagnostics.Logging.GetLogging<Upgrader>().ErrorAsync(ex, cancellation);
		}
	}
	#endregion
}
