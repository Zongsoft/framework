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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 * The above copyright notice and this permission notice shall be included in all
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Upgrading;

internal class Program
{
	static async Task Main(string[] args)
	{
		try
		{
			//显示本部署程序的进程编号、程序名称和版本号
			Console.WriteLine($"[{Environment.ProcessId}]{Deployer.Name}@{Deployer.Version}");

			//将部署程序的进程编号、程序名称和版本号以及执行参数记录到日志中
			await Diagnostics.Logging.GetLogging().InfoAsync(
				$"[{Environment.ProcessId}]{Deployer.Name}@{Deployer.Version}\n" +
				$"{string.Join('\n', args)}");

			//将执行参数数组转换成参数对象
			var argument = Deployer.Argument.Create(args);

			//如果参数为空则返回
			if(argument == null)
			{
				Console.Error.WriteLine($"Missing the required command-line arguments({Deployer.Argument.Keys.AppId}, {Deployer.Argument.Keys.AppType}, {Deployer.Argument.Keys.Deployment}, ...)");
				Environment.ExitCode = -1;
				return;
			}

			//执行部署任务
			await Deployer.DeployAsync(argument);
		}
		catch(Exception ex)
		{
			await Diagnostics.Logging.GetLogging().ErrorAsync(ex);
		}
		finally
		{
			//包括参数校验失败路径，均等待已接受的日志持久化。
			await Diagnostics.Logging.FlushAsync();
		}
	}
}
