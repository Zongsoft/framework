using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Caching;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Samples;

internal class Program
{
	static int _PERIOD_    = 100;
	static int _LIMIT_     = 100000;
	static int _COUNT_     = 100_0000;
	static int _COLLISION_ = 0;

	static void Main(string[] args)
	{
		var stopwater = new Stopwatch();
		var executor = Terminal.Console.Executor;

		executor.Command("info", context =>
		{
			const int PADDING = 10;

			if(context.Options.TryGetValue<int>("period", out var period))
				_PERIOD_ = period;

			if(context.Options.TryGetValue<int>("limit", out var limit))
				_LIMIT_ = limit;

			if(context.Options.TryGetValue<int>("count", out var count))
				_COUNT_ = count;

			if(context.Options.TryGetValue<int>("collision", out var collision))
				_COLLISION_ = collision;

			var content = CommandOutletContent.Create()
				.Append(CommandOutletColor.DarkCyan, "Period".PadRight(PADDING))
				.Append(CommandOutletColor.DarkGray, "= ")
				.AppendLine(CommandOutletColor.DarkYellow, $"{_PERIOD_:#,####} ms")
				.Append(CommandOutletColor.DarkCyan, "Limit".PadRight(PADDING))
				.Append(CommandOutletColor.DarkGray, "= ")
				.AppendLine(CommandOutletColor.DarkYellow, $"{_LIMIT_:#,####}")
				.Append(CommandOutletColor.DarkCyan, "Count".PadRight(PADDING))
				.Append(CommandOutletColor.DarkGray, "= ")
				.AppendLine(CommandOutletColor.DarkYellow, $"{_COUNT_:#,####}")
				.Append(CommandOutletColor.DarkCyan, "Collision".PadRight(PADDING))
				.Append(CommandOutletColor.DarkGray, "= ")
				.AppendLine(CommandOutletColor.DarkYellow, $"{_COLLISION_}");

			Terminal.Write(content);
		});

		executor.Command("spool", async (context, cancellation) =>
		{
			(var count, var collision) = GetCommandOptions(context);
			stopwater.Restart();
			await TestSpoolerAsync(count, collision, cancellation);
			stopwater.Stop();

			Terminal.Console.Write(CommandOutletColor.DarkMagenta, $"Elapsed: ");
			Terminal.Console.Write(CommandOutletColor.Cyan, stopwater.Elapsed);
		});

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Yellow, new string('*', 50))
			.AppendLine(CommandOutletColor.DarkCyan, "Welcome to the Stasher & Spooler sample.".Justify(50))
			.AppendLine(CommandOutletColor.Yellow, new string('*', 50));

		executor.Run(splash);
	}

	static async ValueTask TestSpoolerAsync(int count, int collision, CancellationToken cancellation)
	{
		using var spooler = new Spooler<int>(Handler.HandleAsync, TimeSpan.FromMilliseconds(_PERIOD_), _LIMIT_);

		await Parallel.ForAsync(0, count, cancellation, async (index, cancellation) =>
		{
			var value = collision > 0 ?
				Random.Shared.Next(0, collision) :
				Random.Shared.Next();

			await spooler.PutAsync(value, cancellation);
		});

		Handler.Reset(count);
	}

	private static (int count, int collision) GetCommandOptions(CommandContext context)
	{
		if(!context.Options.TryGetValue<int>("count", out var count))
			count = context.Arguments.GetValue(0, _COUNT_);

		if(!context.Options.TryGetValue<int>("collision", out var collision))
			collision = context.Arguments.GetValue(1, _COLLISION_);

		return (Math.Max(count, 100), collision);
	}

	static class Handler
	{
		private static volatile int _count;
		private static volatile int _times;

		internal static void Reset(int count)
		{
			var content = CommandOutletContent.Create(Environment.NewLine);

			if(_count != count)
			{
				content
					.Append(CommandOutletColor.DarkGray, "[")
					.Append(CommandOutletColor.DarkYellow, $"Completed")
					.Append(CommandOutletColor.DarkGray, "] ")
					.AppendLine(CommandOutletColor.DarkGray, "Waiting...");

				Terminal.Write(content);

				//等待最后一波的缓冲过期（注意：等待的时长必须是计时器的倍数）
				SpinWait.SpinUntil(() => _count >= count, _PERIOD_ * 3);

				content.Last
					.Append(CommandOutletColor.DarkGray, "[")
					.Append(CommandOutletColor.DarkYellow, $"Completed")
					.Append(CommandOutletColor.DarkGray, "] ")
					.AppendLine(CommandOutletColor.Green, "Waiting was finished.");

				Terminal.WriteLine(content);
			}

			content.Last.AppendLine()
				.AppendLine(CommandOutletColor.Yellow, new string('-', 30))
				.Append(CommandOutletColor.DarkGray, "[")
				.Append(CommandOutletColor.DarkMagenta, $"{_times}")
				.Append(CommandOutletColor.DarkGray, "] ")
				.Append(CommandOutletColor.DarkYellow, $"{count:0,0000}")
				.Append(CommandOutletColor.DarkGray, " : ")
				.Append(count == _count ? CommandOutletColor.DarkGreen : CommandOutletColor.DarkRed, $"{_count:0,0000}");

			Terminal.Console.WriteLine(content);

			_count = 0;
			_times = 0;
		}

		internal static ValueTask HandleAsync(IEnumerable<int> items, CancellationToken cancellation)
		{
			var times = Interlocked.Increment(ref _times);
			var count = items.Count();
			var total = Interlocked.Add(ref _count, count);

			if(times < 100)
			{
				var content = CommandOutletContent.Create()
					.Append(CommandOutletColor.DarkGray, "[")
					.Append(CommandOutletColor.DarkCyan, "Flushed")
					.Append(CommandOutletColor.DarkYellow, "@")
					.Append(CommandOutletColor.Cyan, $"T{Environment.CurrentManagedThreadId:00}")
					.Append(CommandOutletColor.DarkGray, "]")
					.Append(CommandOutletColor.DarkGray, " (")
					.Append(CommandOutletColor.DarkMagenta, $"{times}")
					.Append(CommandOutletColor.DarkGray, ") ")
					.Append(CommandOutletColor.Yellow, $"{count}")
					.Append(CommandOutletColor.DarkGray, "/")
					.Append(CommandOutletColor.DarkGreen, $"{total}");

				Terminal.Console.WriteLine(content);
			}
			else if(times == 100)
			{
				Terminal.Console.WriteLine(CommandOutletColor.Gray, "\t... ...");
			}

			return ValueTask.CompletedTask;
		}
	}
}
