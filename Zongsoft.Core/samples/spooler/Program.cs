using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Caching;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Samples;

internal class Program
{
	const int PERIOD = 100;
	const int LIMIT  = 100000;

	static void Main(string[] args)
	{
		var stopwater = new Stopwatch();
		var executor = Terminal.Console.Executor;

		executor.Command("stash", context =>
		{
			(var count, var collision) = GetCommandOptions(context);
			stopwater.Restart();
			TestStash(count, collision);
			stopwater.Stop();

			Terminal.Console.Write(CommandOutletColor.DarkMagenta, $"Elapsed: ");
			Terminal.Console.Write(CommandOutletColor.Cyan, stopwater.Elapsed);
		});

		executor.Command("spooler", context =>
		{
			(var count, var collision) = GetCommandOptions(context);
			stopwater.Restart();
			TestSpooler(count, collision);
			stopwater.Stop();

			Terminal.Console.Write(CommandOutletColor.DarkMagenta, $"Elapsed: ");
			Terminal.Console.Write(CommandOutletColor.Cyan, stopwater.Elapsed);
		});

		executor.Run($"Welcome to the Stash & Spooler tester.{Environment.NewLine}{new string('-', 50)}");
	}

	static void TestStash(int count, int collision = 0)
	{
		using var stash = new Stash<int>(Handler.Handle, TimeSpan.FromMilliseconds(PERIOD), LIMIT);

		var result = Parallel.For(0, count, index =>
		{
			var value = collision > 0 ?
				Random.Shared.Next(0, collision) :
				Random.Shared.Next();

			stash.Put(value);
		});

		Handler.Reset(result, count);
	}

	static void TestSpooler(int count, int collision = 0)
	{
		using var spooler = new Spooler<int>(Handler.Handle, collision > 0, TimeSpan.FromMilliseconds(PERIOD), LIMIT);

		var result = Parallel.For(0, count, index =>
		{
			var value = collision > 0 ?
				Random.Shared.Next(0, collision) :
				Random.Shared.Next();

			spooler.Put(value);
		});

		Handler.Reset(result, count);
	}

	private static (int count, int collision) GetCommandOptions(CommandContext context)
	{
		const int COUNT = 100_0000;

		if(!context.Expression.Options.TryGetValue<int>("count", out var count))
			count = context.Expression.Arguments.GetValue(0, COUNT);

		if(!context.Expression.Options.TryGetValue<int>("collision", out var collision))
			collision = context.Expression.Arguments.GetValue(1, 0);

		return (Math.Max(count, 100), collision);
	}

	static class Handler
	{
		private static volatile int _count;
		private static volatile int _times;

		internal static void Reset(ParallelLoopResult result, int count)
		{
			var content = CommandOutletContent.Create(Environment.NewLine);

			if(result.IsCompleted)
			{
				if(_count != count)
				{
					content
						.Append(CommandOutletColor.DarkCyan, $"[Completed] ")
						.AppendLine(CommandOutletColor.DarkGray, "Waiting...");

					//等待最后一波的缓冲过期被刷新
					SpinWait.SpinUntil(() => _count >= count, PERIOD);

					content
						.Append(CommandOutletColor.DarkCyan, $"[Completed] ")
						.AppendLine(CommandOutletColor.DarkGreen, "Waiting was finished.");
				}

				content.AppendLine()
					.AppendLine(CommandOutletColor.Yellow, new string('-', 30))
					.Append(CommandOutletColor.DarkGray, "[")
					.Append(CommandOutletColor.DarkMagenta, $"{_times}")
					.Append(CommandOutletColor.DarkGray, "] ")
					.Append(CommandOutletColor.DarkYellow, $"{count:0,0000}")
					.Append(CommandOutletColor.DarkGray, " : ")
					.Append(count == _count ? CommandOutletColor.DarkGreen : CommandOutletColor.DarkRed, $"{_count:0,0000}");

				Terminal.Console.WriteLine(content);
			}

			_count = 0;
			_times = 0;
		}

		internal static void Handle(IEnumerable<int> items)
		{
			var times = Interlocked.Increment(ref _times);
			var count = items.Count();
			var total = Interlocked.Add(ref _count, count);

			//Console.WriteLine($"[Flush#{Environment.CurrentManagedThreadId}] {count}/{total} @ {times}");
		}
	}
}
