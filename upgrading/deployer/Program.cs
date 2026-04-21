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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Upgrading;

internal class Program
{
	static async Task Main(string[] args)
	{
		try
		{
			//将执行参数数组转换成参数对象
			var argument = Deployer.Argument.Create(args);

			//如果参数为空则返回
			if(argument == null)
			{
				Console.WriteLine($"{Deployer.Name}@{Deployer.Version}");
				Console.Error.WriteLine($"Missing the required command-line arguments({Deployer.Argument.Keys.AppId}, {Deployer.Argument.Keys.AppType}, {Deployer.Argument.Keys.Deployment}, ...)");
				Environment.ExitCode = -1;
				return;
			}

			//如果执行参数中包含版本号，则通过控制台的标准输出本程序的版本号
			if(argument.Contains(nameof(Deployer.Version)))
				Console.WriteLine(Deployer.Version);

			//执行部署任务
			await Deployer.DeployAsync(argument);
		}
		catch(Exception ex)
		{
			Zongsoft.Diagnostics.Logging.GetLogging().Error(ex);
		}

		//等待后台线程和任务完成
		await Task.Delay(500);
	}
}
