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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Commands;

partial class SequenceCommand
{
	[CommandOption(KEY_QUIET_OPTION, 'q', typeof(bool), false)]
	public class InfoCommand : CommandBase<CommandContext>
	{
		#region 常量定义
		private const string KEY_QUIET_OPTION = "quiet";
		#endregion

		#region 构造函数
		public InfoCommand() : base("Info") { }
		public InfoCommand(string name) : base(name) { }
		#endregion

		#region 重写方法
		protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			if(context.Arguments.IsEmpty)
				throw new CommandException("The key(s) required to retrieve the sequence information is missing.");

			var quiet = context.GetOptions().GetValue(KEY_QUIET_OPTION, true);
			var sequence = context.Find<SequenceCommand>(true)?.Sequence ?? throw new CommandException("The sequence instance is not specified.");

			if(sequence != null && !quiet)
				context.Output.WriteLine(CommandOutletColor.Blue, sequence);

			if(sequence is Sequence.IVariator variator)
			{
				if(context.Arguments.Count == 1)
				{
					if(!quiet)
						DumpStatistics(context, null, variator.GetStatistics(context.Arguments[0]));

					return ValueTask.FromResult<object>(variator.GetStatistics(context.Arguments[0]));
				}
				else
				{
					if(!quiet)
					{
						for(int i = 0; i < context.Arguments.Count; i++)
						{
							var dumped = DumpStatistics(context, context.Arguments[i], variator.GetStatistics(context.Arguments[i]));

							if(i < context.Arguments.Count - 1 && dumped)
								context.Output.WriteLine();
						}
					}

					return ValueTask.FromResult<object>(context.Arguments.Select(variator.GetStatistics));
				}
			}

			return ValueTask.FromResult<object>(null);
		}
		#endregion

		#region 私有方法
		private static bool DumpStatistics(CommandContext context, string key, Sequence.IVariatorStatistics statistics)
		{
			if(statistics == null)
			{
				if(string.IsNullOrEmpty(key))
					context.Output.WriteLine(CommandOutletColor.DarkMagenta, "The specified sequence information does not exist.");
				else
					context.Output.WriteLine(CommandOutletColor.DarkMagenta, $"The specified '{key}' sequence information does not exist.");

				return false;
			}

			var content = (string.IsNullOrEmpty(key) ? CommandOutletContent.Create() : CommandOutletContent.Create(CommandOutletColor.DarkCyan, key + Environment.NewLine))
				.Append(CommandOutletColor.DarkGreen, nameof(statistics.Count))
				.Append(CommandOutletColor.DarkGray, " : ")
				.AppendLine(CommandOutletColor.DarkYellow, statistics.Count)
				.Append(CommandOutletColor.DarkGreen, nameof(statistics.Value))
				.Append(CommandOutletColor.DarkGray, " : ")
				.AppendLine(CommandOutletColor.DarkYellow, statistics.Value)
				.Append(CommandOutletColor.DarkGreen, nameof(statistics.Interval))
				.Append(CommandOutletColor.DarkGray, " : ")
				.AppendLine(CommandOutletColor.DarkYellow, statistics.Interval)
				.Append(CommandOutletColor.DarkGreen, nameof(statistics.Threshold))
				.Append(CommandOutletColor.DarkGray, " : ")
				.AppendLine(CommandOutletColor.DarkYellow, statistics.Threshold)
				.Append(CommandOutletColor.DarkGreen, nameof(statistics.Timestamp))
				.Append(CommandOutletColor.DarkGray, " : ")
				.AppendLine(CommandOutletColor.DarkYellow, statistics.Timestamp);

			context.Output.Write(content);
			return true;
		}
		#endregion
	}
}
