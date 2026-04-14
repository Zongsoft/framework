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
using System.Diagnostics;
using System.Collections.Generic;

namespace Zongsoft.Upgrading;

/// <summary>提供宿主应用程序启动功能。</summary>
public abstract partial class Launcher
{
	#region 私有字段
	private static readonly Dictionary<string, ILauncher> _launchers;
	#endregion

	#region 静态构造
	static Launcher()
	{
		_launchers = new(StringComparer.OrdinalIgnoreCase);

		foreach(var type in typeof(Launcher).GetNestedTypes(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
		{
			if(typeof(ILauncher).IsAssignableFrom(type))
			{
				var launcher = (ILauncher)Activator.CreateInstance(type);
				_launchers.Add(launcher.Name, launcher);
			}
		}
	}
	#endregion

	#region 公共属性
	/// <summary>获取默认启动器。</summary>
	public static ILauncher Default => _launchers.TryGetValue(string.Empty, out var launcher) ? launcher : null;
	#endregion

	#region 公共方法
	public static void Launch(Deployer.Deployment deployment, Deployer.Argument argument)
	{
		if(argument == null || deployment == null)
			return;

		try
		{
			//根据宿主应用类型获取对应的启动器
			if(_launchers.TryGetValue(argument.AppType ?? string.Empty, out var launcher))
				launcher.Launch(argument);
			else
				Default.Launch(argument);

			//释放部署文件(解除独占锁)
			deployment.Dispose();

			//删除部署文件
			if(File.Exists(argument.Deployment))
				File.Delete(argument.Deployment);
		}
		finally
		{
			deployment?.Dispose();
		}
	}
	#endregion

	#region 私有方法
	private static string GetService(Deployer.Argument argument)
	{
		//获取宿主应用的根目录
		var directory = Path.GetDirectoryName(argument.AppPath);

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
		var process = this.OnLaunch(argument);
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

		string extra = string.IsNullOrEmpty(process.StartInfo.Verb) ?
			$"{process.StartInfo.FileName} {process.StartInfo.Arguments}":
			$"[{process.StartInfo.Verb}]{process.StartInfo.FileName} {process.StartInfo.Arguments}";

		//被重定向的进程则表明为启动命令的中间进程（即非宿主应用进程）
		if(process.StartInfo.RedirectStandardError || process.StartInfo.RedirectStandardOutput)
		{
			//获取启动进程的标准错误信息
			var text = process.StandardError.ReadToEnd();
			if(!string.IsNullOrWhiteSpace(text))
				extra += text;

			//获取启动进程的标准输出信息
			text = process.StandardOutput.ReadToEnd();
			if(!string.IsNullOrWhiteSpace(text))
				extra += text;

			//等待启动命令进程完成
			process.WaitForExit();
		}

		if(process.HasExited)
			Diagnostics.Logging.GetLogging().Info($"The {(string.IsNullOrEmpty(this.Name) ? "default" : this.Name)} launcher has launched successfully.", extra);
		else
			Diagnostics.Logging.GetLogging().Info($"The {(string.IsNullOrEmpty(this.Name) ? "default" : this.Name)} launcher successfully launched the '[{process.Id}]{process.ProcessName}' program.", extra);

		//确保日志存储器落盘完成
		Diagnostics.Logging.FlushAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
	}
	#endregion
}