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
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Commands;

partial class SequenceCommand
{
	[CommandOption(QUIET_OPTION, 'q')]
	[CommandOption(ROUND_OPTION, 'r', typeof(int), 1)]
	[CommandOption(JITTER_OPTION, 'j', typeof(TimeSpan), "0")]
	[CommandOption(SEED_OPTION, 's', typeof(int), 0)]
	[CommandOption(INTERVAL_OPTION, 'v', typeof(int), 1)]
	public class IncreaseCommand : CommandBase<CommandContext>
	{
		#region 常量定义
		const string QUIET_OPTION = "quiet";
		const string ROUND_OPTION = "round";
		const string JITTER_OPTION = "jitter";
		const string SEED_OPTION = "seed";
		const string INTERVAL_OPTION = "interval";
		#endregion

		#region 构造函数
		public IncreaseCommand() : base("Increase") { }
		public IncreaseCommand(string name) : base(name) { }
		#endregion

		#region 重写方法
		protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			if(context.Arguments.IsEmpty)
				throw new InvalidOperationException("Missing the required arguments.");

			var sequence = context.Find<SequenceCommand>(true)?.Sequence ?? throw new CommandException("The sequence instance is not specified.");
			var round = context.Options.GetValue(ROUND_OPTION, 1);
			var seed = context.Options.GetValue(SEED_OPTION, 0);
			var interval = context.Options.GetValue(INTERVAL_OPTION, 1);
			var quiet = context.Options.Switch(QUIET_OPTION);

			if(round > 1)
			{
				var jitter = context.Options.GetValue(JITTER_OPTION, TimeSpan.Zero);
				var result = new Dictionary<string, ConcurrentBag<long>>(context.Arguments.Count);

				for(int i = 0; i < context.Arguments.Count; i++)
				{
					var history = await IncreaseAsync(
						context.Output,
						sequence,
						context.Arguments[i],
						interval,
						seed,
						round,
						jitter,
						quiet,
						cancellation);

					System.Diagnostics.Debug.Assert(history.Count == round);
					result.TryAdd(context.Arguments[i], history);
				}

				return result;
			}
			else
			{
				var result = new long[context.Arguments.Count];

				for(int i = 0; i < context.Arguments.Count; i++)
				{
					result[i] = await IncreaseAsync(
						context.Output,
						sequence,
						context.Arguments[i],
						interval,
						seed,
						cancellation);
				}

				return result.Length > 1 ? result : result[0];
			}
		}
		#endregion

		#region 私有方法
		static async ValueTask<long> IncreaseAsync(ICommandOutlet output, ISequenceBase sequence, string key, int interval, int seed, CancellationToken cancellation)
		{
			var value = await sequence.IncreaseAsync(key, interval, seed, cancellation);

			output.WriteLine(CommandOutletContent.Create()
				.Append(CommandOutletColor.DarkGreen, key)
				.Append(CommandOutletColor.DarkBlue, "=")
				.Append(CommandOutletColor.DarkYellow, value));

			return value;
		}

		static async ValueTask<ConcurrentBag<long>> IncreaseAsync(ICommandOutlet output, ISequenceBase sequence, string key, int interval, int seed, int round, TimeSpan jitter, bool quiet, CancellationToken cancellation)
		{
			var result = new ConcurrentBag<long>();
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			await Parallel.ForAsync(0, round, cancellation, async (index, cancellation) =>
			{
				if(jitter > TimeSpan.Zero)
					await Task.Delay(Random.Shared.Next(1, (int)jitter.TotalMilliseconds), cancellation);

				var value = await sequence.IncreaseAsync(key, interval, seed, cancellation);
				result.Add(value);

				if(!quiet)
					output.WriteLine(CommandOutletContent.Create()
						.Append(CommandOutletColor.DarkGray, "[")
						.Append(CommandOutletColor.Cyan, $"{index + 1}")
						.Append(CommandOutletColor.DarkGray, "] ")
						.Append(CommandOutletColor.DarkGreen, key)
						.Append(CommandOutletColor.DarkBlue, "=")
						.Append(CommandOutletColor.DarkYellow, value));
			});

			stopwatch.Stop();
			var elapsed = stopwatch.Elapsed;
			output.WriteLine(CommandOutletContent.Create()
				.Append(CommandOutletColor.Magenta, ">> ")
				.Append(CommandOutletColor.DarkCyan, $"Completed {round:#,###} rounds for '{key}' in {elapsed.TotalMilliseconds:#,###.######} ms."));

			return result;
		}
		#endregion
	}
}
