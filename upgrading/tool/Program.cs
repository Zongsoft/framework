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

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Upgrading;

internal class Program
{
	public static ITerminalExecutor Executor => Terminal.Console.Executor;

	public static async Task Main(string[] args)
	{
		if(args == null || args.Length == 0)
		{
			Terminal.WriteLine(CommandOutletColor.DarkRed, Properties.Resources.CommandLineEmpty_Message);
			Environment.ExitCode = -1;
			return;
		}

		//初始化
		Executor.Root.Children.Clear();
		Executor.Root.Children.Add(new Packager.PackCommand());
		Executor.Root.Children.Add(new Packager.PublishCommand());
		Executor.Root.Children.Add(new Packager.ChecksumCommand());

		try
		{
			var expression = GetCommandLine(args);
			if(string.IsNullOrEmpty(expression))
			{
				Environment.ExitCode = -2;
				return;
			}

			//执行命令
			await Executor.ExecuteAsync(expression);
		}
		catch(Exception ex)
		{
			//打印异常消息
			Terminal.WriteLine(CommandOutletColor.DarkRed, ex.Message + Environment.NewLine + ex.StackTrace);
		}

		static string GetCommandLine(string[] args)
		{
			if(Executor.Root.HasChildren && Executor.Root.Children.Contains(args[0]))
				return CommandLine.Get(args);

			Terminal.WriteLine(CommandOutletColor.Red, string.Format(Properties.Resources.UnrecognizedCommand_Message, args[0]));
			return null;
		}
	}
}
