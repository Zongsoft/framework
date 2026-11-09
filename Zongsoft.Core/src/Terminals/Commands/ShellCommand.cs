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
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.IO;
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
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(Environment.OSVersion.Platform == PlatformID.MacOSX ||
		   Environment.OSVersion.Platform == PlatformID.Unix)
			throw new NotSupportedException(string.Format(Properties.Resources.Terminal_UnsupportedOperatingSystem_Message, Environment.OSVersion));

		var terminal = context.GetTerminal() ??
			throw new NotSupportedException(string.Format(Properties.Resources.Terminal_CommandRequiresTerminal_Message, this.Name));

		if(context.Arguments.Count < 1)
			return 0;

		cancellation.ThrowIfCancellationRequested();

		var timeout = context.Options.GetValue<TimeSpan>(TIMEOUT_OPTION);
		using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
		deadline.CancelAfter(timeout > TimeSpan.Zero ? timeout : TimeSpan.FromSeconds(30));

		var info = new ProcessStartInfo(@"cmd.exe", " /C " + context.Arguments[0])
		{
			CreateNoWindow = true,
			UseShellExecute = false,
			RedirectStandardError = true,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
		};

		using var process = Process.Start(info);

		try
		{
			await Task.WhenAll(
				process.WaitForExitAsync(deadline.Token),
				CopyAsync(process.StandardOutput, terminal.Output, deadline.Token),
				CopyAsync(process.StandardError, terminal.Error, deadline.Token));

			return process.ExitCode;
		}
		catch(OperationCanceledException) when(deadline.IsCancellationRequested)
		{
			cancellation.ThrowIfCancellationRequested();
			return -1;
		}
	}
	#endregion

	#region 私有方法
	private static async Task CopyAsync(StreamReader reader, TextWriter writer, CancellationToken cancellation)
	{
		while(await reader.ReadLineAsync(cancellation) is string line)
			await writer.WriteLineAsync(line.AsMemory(), cancellation);
	}
	#endregion
}
