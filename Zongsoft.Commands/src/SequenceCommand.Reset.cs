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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Commands library.
 *
 * The Zongsoft.Commands is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Commands is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Commands library. If not, see <http://www.gnu.org/licenses/>.
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
				throw new CommandException("The key(s) to be reset is missing.");

			var sequence = context.Find<SequenceCommand>(true)?.Sequence ?? throw new CommandException("The sequence instance is not specified.");
			var value = context.GetOptions().GetValue(VALUE_OPTION, 0);

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
