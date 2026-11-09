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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Commands;

partial class SequenceCommand
{
	[CommandOption(VALUE_OPTION, 'v', typeof(int), DefaultValue = 0)]
	public class ResetCommand : CommandBase<CommandContext>
	{
		#region 常量定义
		const string VALUE_OPTION = "value";
		#endregion

		#region 构造函数
		public ResetCommand() : base("Reset") { }
		public ResetCommand(string name) : base(name) { }
		#endregion

		#region 重写方法
		protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			if(context.Arguments.IsEmpty)
				throw new CommandException(Properties.Resources.Sequence_KeysRequired_Message);

			var sequence = context.Find<SequenceCommand>(true)?.Sequence ?? throw new CommandException(Properties.Resources.Sequence_InstanceRequired_Message);
			var value = context.Options.GetValue(VALUE_OPTION, 0);

			for(int i = 0; i < context.Arguments.Count; i++)
			{
				await sequence.ResetAsync(context.Arguments[i], value, cancellation);

				context.Output.WriteLine(
					CommandOutletContent.Create()
					.Append(CommandOutletColor.DarkCyan, $"[{i + 1}] ")
					.Append(CommandOutletColor.DarkGreen, $"The specified '{context.Arguments[i]}' sequence has been reset successfully."));
			}

			return null;
		}
		#endregion
	}
}
