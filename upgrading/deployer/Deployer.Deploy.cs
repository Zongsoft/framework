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
 * The MIT License (MIT)
 * 
 * Copyright (C) 2020-2026 Zongsoft Corporation <http://zongsoft.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Zongsoft.Upgrading;

partial class Deployer
{
	#region 事件声明
	public static event EventHandler<DeployEventArgs> Deployed;
	public static event EventHandler<DeployEventArgs> Deploying;
	#endregion

	#region 公共方法
	public static async ValueTask DeployAsync(Argument argument, CancellationToken cancellation = default)
	{
		//等待宿主程序退出
		if(!WaitHostExit(argument))
			return;

		//获取部署配置信息并以排他性的锁定部署文件
		using var deployment = GetDeployment(argument);
		if(deployment == null)
			return;

		//如果部署包源目录不存在则退出
		var packages = new DirectoryInfo(deployment.Packages);
		if(!packages.Exists)
		{
			await Diagnostics.Logging.GetLogging().ErrorAsync($"The '{packages.FullName}' packages directory specified in the deployment file does not exist.");
			return;
		}

		//如果升级清单加载失败则退出
		var manifest = Manifest.Load(deployment.Manifest);
		if(manifest == null || manifest.IsEmpty)
		{
			await Diagnostics.Logging.GetLogging().ErrorAsync($"The '{deployment.Manifest}' manifest file specified in the deployment file failed to load or is empty.");
			return;
		}

		//获取部署文件所在目录，即为宿主应用程序的根目录
		var root = Path.GetDirectoryName(argument.Deployment);

		//如果当前部署为全量升级模式则先清空宿主应用目录下的所有文件及子目录
		if(manifest.Trunk != null && manifest.Trunk.Kind == ReleaseKind.Fully)
			Helper.Clean(root, argument);

		//激发“Deploying”事件
		await OnDeployingAsync(argument, manifest, cancellation);

		//将部署包源目录中的所有文件及子目录复制到宿主程序根目录
		Helper.Replicate(packages, root);

		//激发“Deployed”事件
		await OnDeployedAsync(argument, manifest, cancellation);

		//启动宿主程序
		Launcher.Launch(argument, deployment);
	}
	#endregion

	#region 触发事件
	private static async ValueTask OnDeployedAsync(Argument argument, Manifest manifest, CancellationToken cancellation)
	{
		var args = new DeployEventArgs(argument, manifest);
		OnDeployed(args);
		await Executor.ExecuteAsync(nameof(Deployed), null, args, cancellation);
	}

	private static async ValueTask OnDeployingAsync(Argument argument, Manifest manifest, CancellationToken cancellation)
	{
		var args = new DeployEventArgs(argument, manifest);
		OnDeploying(args);
		await Executor.ExecuteAsync(nameof(Deploying), null, args, cancellation);
	}

	private static void OnDeployed(DeployEventArgs args) => Deployed?.Invoke(null, args);
	private static void OnDeploying(DeployEventArgs args) => Deploying?.Invoke(null, args);
	#endregion

	#region 私有方法
	private static Deployment GetDeployment(Argument argument)
	{
		var deployment = argument.Deployment;

		if(string.IsNullOrWhiteSpace(deployment))
		{
			Diagnostics.Logging.GetLogging<Program>().Error("No deployment file specified.");
			return null;
		}

		if(!File.Exists(deployment))
		{
			Diagnostics.Logging.GetLogging<Program>().Error($"The specified '{deployment}' deployment file does not exist.");
			return null;
		}

		try
		{
			return Deployment.Load(deployment, true);
		}
		catch(Exception ex)
		{
			Diagnostics.Logging.GetLogging<Program>().Error(ex);
		}

		return null;
	}

	private static bool WaitHostExit(Argument argument, TimeSpan timeout = default)
	{
		if(argument.AppId == 0)
			return true;

		//获取指定编号的进程
		var process = GetProcess(argument.AppId);

		//如果进程已退出则返回成功
		if(HasExited(process))
			return true;

		try
		{
			process.EnableRaisingEvents = true;

			if(timeout > TimeSpan.Zero)
				return process.WaitForExit(timeout);
			else
				return process.WaitForExit(TimeSpan.FromMinutes(1));
		}
		catch { return false; }

		static Process GetProcess(int id)
		{
			try { return id == 0 ? null : Process.GetProcessById(id); }
			catch { return null; }
		}

		static bool HasExited(Process process)
		{
			try { return process == null || process.HasExited; }
			catch { return false; }
		}
	}
	#endregion

	#region 嵌套子类
	/// <summary>表示部署事件的参数类。</summary>
	public class DeployEventArgs : ReleaseEventArgs
	{
		public DeployEventArgs(Argument argument, Manifest manifest) : base(manifest.Trunk == null ? manifest.Deltas : [manifest.Trunk, ..manifest.Deltas])
		{
			this.Argument = argument;
		}

		/// <summary>获取部署参数对象。</summary>
		public Argument Argument { get; }
	}
	#endregion
}
