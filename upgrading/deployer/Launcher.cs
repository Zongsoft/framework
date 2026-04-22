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
using System.Linq;
using System.Diagnostics;

namespace Zongsoft.Upgrading;

/// <summary>提供宿主应用程序启动功能。</summary>
public abstract partial class Launcher
{
	#region 单例字段
	/// <summary>表示 Web 网站应用启动器。</summary>
	public static readonly ILauncher Web = new WebLauncher();
	/// <summary>表示 Daemon 后台应用启动器。</summary>
	public static readonly ILauncher Daemon = new DaemonLauncher();
	/// <summary>表示 Terminal 终端应用启动器。</summary>
	public static readonly ILauncher Terminal = new TerminalLauncher();
	/// <summary>表示通用应用启动器。</summary>
	public static readonly ILauncher Universal = new UniversalLauncher();
	#endregion

	#region 公共方法
	public static void Launch(Deployer.Argument argument, Deployer.Deployment deployment)
	{
		if(argument == null || deployment == null)
			return;

		//释放部署文件(解除独占锁)
		deployment.Dispose();

		//根据宿主应用类型获取对应的启动器
		switch(argument.AppType)
		{
			case "web":
			case WebLauncher.NAME:
				Web.Launch(argument);
				break;
			case "daemon":
			case DaemonLauncher.NAME:
				Daemon.Launch(argument);
				break;
			case "terminal":
			case TerminalLauncher.NAME:
				Terminal.Launch(argument);
				break;
			default:
				Universal.Launch(argument);
				break;
		}
	}
	#endregion

	#region 私有方法
	private static string GetService(Deployer.Argument argument)
	{
		//获取宿主应用的根目录
		var directory = argument.AppPath;

		if(Directory.Exists(directory))
		{
			//在宿主应用的根目录中查找“*.service”服务文件
			var files = Directory.GetFiles(directory, "*.service");
			if(files.Length == 1)
				return Path.GetFileName(files[0]);

			for(int i = 0; i < files.Length; i++)
			{
				if(Path.GetFileName(files[i]).StartsWith(argument.AppName, StringComparison.OrdinalIgnoreCase))
					return Path.GetFileName(files[i]);
			}
		}

		return argument.AppName;
	}
	#endregion
}

partial class Launcher : ILauncher
{
	#region 构造函数
	protected Launcher(string name)
	{
		this.Name = name ?? string.Empty;
	}
	#endregion

	#region 保护属性
	protected string Name { get; }
	#endregion

	#region 显式实现
	string ILauncher.Name => this.Name;
	void ILauncher.Launch(Deployer.Argument argument)
	{
		Diagnostics.Logging.GetLogging().Info($"[OnLaunching]\n" +
			string.Join(Environment.NewLine, argument.Select(entry => $"{entry.Key}={entry.Value}")));

		var process = this.OnLaunch(argument);

		Diagnostics.Logging.GetLogging().Info($"[OnLaunched]\n" +
			$"id: {process.Id}\n" +
			$"name: {process.ProcessName}\n" +
			$"file: {process.StartInfo.FileName}\n" +
			$"args: {string.Join(' ', process.StartInfo.ArgumentList)}\n" +
			$"working: {process.StartInfo.WorkingDirectory}\n");

		this.OnLaunched(argument, process);
	}
	#endregion

	#region 抽象方法
	protected abstract Process OnLaunch(Deployer.Argument argument);
	#endregion

	#region 虚拟方法
	protected virtual void OnLaunched(Deployer.Argument argument, Process process)
	{
		if(process == null)
			return;

		//删除部署文件
		if(argument.Deployment != null && File.Exists(argument.Deployment))
			File.Delete(argument.Deployment);

		string extra = string.IsNullOrEmpty(process.StartInfo.Verb) ?
			$"{process.StartInfo.FileName} {string.Join(' ', process.StartInfo.ArgumentList)}":
			$"[{process.StartInfo.Verb}]{process.StartInfo.FileName} {string.Join(' ', process.StartInfo.ArgumentList)}";

		//被重定向的进程则表明为启动命令的中间进程（即非宿主应用进程）
		if(process.StartInfo.RedirectStandardError || process.StartInfo.RedirectStandardOutput)
		{
			//获取启动进程的标准错误信息
			var text = process.StandardError.ReadToEnd();
			if(!string.IsNullOrWhiteSpace(text))
				extra += $"{Environment.NewLine}{text}";

			//获取启动进程的标准输出信息
			text = process.StandardOutput.ReadToEnd();
			if(!string.IsNullOrWhiteSpace(text))
				extra += $"{Environment.NewLine}{text}";

			//等待启动命令进程完成
			process.WaitForExit();
		}

		if(process.HasExited)
			Diagnostics.Logging.GetLogging().Info($"The {(string.IsNullOrEmpty(this.Name) ? nameof(Universal) : this.Name)} launcher has launched successfully.", extra);
		else
			Diagnostics.Logging.GetLogging().Info($"The {(string.IsNullOrEmpty(this.Name) ? nameof(Universal) : this.Name)} launcher successfully launched the '[{process.Id}]{process.ProcessName}' program.", extra);

		//确保日志存储器落盘完成
		Diagnostics.Logging.FlushAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
	}
	#endregion
}