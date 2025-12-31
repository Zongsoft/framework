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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;

using Zongsoft.Components;

namespace Zongsoft.Terminals.Commands;

[DisplayName("ShellCommand.Name")]
[Description("ShellCommand.Description")]
[CommandOption(TIMEOUT_OPTION, 't', typeof(TimeSpan), "1s")]
public class ShellCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string TIMEOUT_OPTION = "timeout";
	#endregion

	#region 构造函数
	public ShellCommand() : base("Shell") { }
	public ShellCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(Environment.OSVersion.Platform == PlatformID.MacOSX ||
		   Environment.OSVersion.Platform == PlatformID.Unix)
			throw new NotSupportedException(string.Format("Not supported in the {0} OS.", Environment.OSVersion));

		var terminal = context.GetTerminal() ??
			throw new NotSupportedException($"The `{this.Name}` command is only supported running in a terminal executor.");

		if(context.Arguments.Count < 1)
			return ValueTask.FromResult<object>(0);

		ProcessStartInfo info = new ProcessStartInfo(@"cmd.exe", " /C " + context.Arguments[0])
		{
			CreateNoWindow = true,
			UseShellExecute = false,
			RedirectStandardError = true,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
		};

		using(var process = Process.Start(info))
		{
			process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs eventArgs)
			{
				terminal.WriteLine(eventArgs.Data);
			};

			process.BeginOutputReadLine();

			//while(!process.StandardOutput.EndOfStream)
			//{
			//	terminal.WriteLine(process.StandardOutput.ReadLine());
			//}

			//terminal.Write(process.StandardOutput.ReadToEnd());

			//process.WaitForExit();

			if(!process.HasExited)
			{
				var timeout = context.Options.GetValue<TimeSpan>(TIMEOUT_OPTION);

				if(!process.WaitForExit(timeout > TimeSpan.Zero ? timeout : TimeSpan.FromSeconds(30)))
				{
					process.Close();
					return ValueTask.FromResult<object>(-1);
				}
			}

			return ValueTask.FromResult<object>(process.ExitCode);
		}
	}
	#endregion
}
