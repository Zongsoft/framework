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

namespace Zongsoft.Upgrading;

partial class Deployer
{
	#region 公共方法
	public static void Deploy(Argument argument)
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
			return;

		//如果升级清单加载失败则退出
		var manifest = Manifest.Load(deployment.Manifest);
		if(manifest == null || manifest.IsEmpty)
			return;

		//获取部署文件所在目录，即为宿主应用程序的根目录
		var root = Path.GetDirectoryName(argument.Deployment);

		//如果当前部署为全量升级模式则先清空宿主应用目录下的所有文件及子目录
		if(manifest.Trunk != null && manifest.Trunk.Kind == ReleaseKind.Fully)
			Helper.Clean(root, argument);

		//将部署包源目录中的所有文件及子目录复制到宿主程序根目录
		Helper.Replicate(packages, root);

		//启动宿主程序
		Launcher.Launch(root, argument);
	}
	#endregion

	#region 私有方法
	private static Deployment GetDeployment(Argument argument)
	{
		var deployment = argument.Deployment;

		if(!string.IsNullOrEmpty(deployment))
		{
			if(!File.Exists(deployment))
				return null;

			try
			{
				return Deployment.Load(deployment, true);
			}
			catch(Exception ex)
			{
				Zongsoft.Diagnostics.Logging.GetLogging<Program>().Error(ex);
			}
		}

		return null;
	}

	private static bool WaitHostExit(Argument argument, TimeSpan timeout = default)
	{
		if(argument.AppId != 0)
		{
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
		}

		return false;

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
}
