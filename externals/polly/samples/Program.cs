using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Terminals;
using Zongsoft.Collections;
using Zongsoft.Components;
using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly.Samples;

internal class Program
{
	static void Main(string[] args)
	{
		var _features = Executor.Features;
		Executor.Pipelines = FeaturePipelineManager.Instance;

		Terminal.Console.Executor.Command("info", context =>
		{
			if(_features == null)
				return;

			var features = _features.Build();

			foreach(var feature in features)
			{
				var content = CommandOutletContent
					.Create(CommandOutletColor.DarkYellow, feature.GetType().GetAlias(true))
					.AppendLine().Append(CommandOutletColor.DarkGray, '{');

				CommandOutletDumper.Dump(content, feature);

				content.Last.AppendLine(CommandOutletColor.DarkGray, '}');
				context.Output.WriteLine(content);
			}
		});

		Terminal.Console.Executor.Command("reset", context =>
		{
			_features = null;
		});

		Terminal.Console.Executor.Command("retry", context =>
		{
			var delay = context.Expression.Options.GetValue("delay", TimeSpan.Zero);
			var limit = context.Expression.Options.GetValue("limit", TimeSpan.Zero);
			var attempts = context.Expression.Options.GetValue("attempts", 3);
			var backoff = context.Expression.Options.GetValue("backoff", RetryBackoff.None);
			var jitterable = context.Expression.Options.GetValue("jitterable", false);

			if(delay <= TimeSpan.Zero)
				delay = TimeSpan.FromSeconds(1);

			_features = _features.Retry(
				backoff,
				new RetryLatency(delay, limit),
				jitterable,
				attempts
			);
		});

		Terminal.Console.Executor.Command("breaker", context =>
		{
			var threshold = context.Expression.Options.GetValue("threshold", 10);
			var duration = context.Expression.Options.GetValue("duration", TimeSpan.Zero);
			var period = context.Expression.Options.GetValue("period", TimeSpan.Zero);
			var ratio = context.Expression.Options.GetValue("ratio", 0.5);

			_features = _features.Breaker(
				duration, ratio, period, threshold
			);
		});

		Terminal.Console.Executor.Command("timeout", context =>
		{
			_features = _features.Timeout(context.Expression.Arguments.GetValue(0, TimeSpan.FromSeconds(1)));
		});

		Terminal.Console.Executor.Command("throttle", context =>
		{
			_features = _features.Throttle();
		});

		Terminal.Console.Executor.Command("fallback", context =>
		{
			_features = _features.Fallback<object>(
				OnFallback,
				Predication.Predicate<object>(argument => true),
				true);
		});

		Terminal.Console.Executor.Command("execute", async (context, cancellation) =>
		{
			if(_features == null)
				return;

			var executor = _features.Build<object, object>(OnExecute);
			var parameters = Parameters
				.Parameter("delay", context.Expression.Options.GetValue("delay", TimeSpan.Zero))
				.Parameter("throw", context.Expression.Options.Contains("throw"));

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			var count = context.Expression.Options.GetValue("count", 1);

			for(int i = 0; i < count; i++)
			{
				stopwatch.Restart();

				try
				{
					parameters.SetValue("round", i);
					parameters.SetValue("index", 0);

					await executor.ExecuteAsync(context.Value, parameters, cancellation);
				}
				catch(Exception ex)
				{
					context.Output.WriteLine(CommandOutletColor.Red, $"[{ex.GetType().Name}] {ex.Message}");
				}

				stopwatch.Stop();

				if(count > 1)
				{
					context.Output.Write(CommandOutletColor.DarkGray, "[");
					context.Output.Write(CommandOutletColor.Red, $"{i + 1}");
					context.Output.Write(CommandOutletColor.DarkGray, "] ");
				}

				context.Output.Write(CommandOutletColor.DarkCyan, $"Elapsed: ");
				context.Output.WriteLine(CommandOutletColor.Green, $"{stopwatch.Elapsed}");
			}
		});

		Terminal.Console.Executor.Aliaser.Set("execute", "exe");
		Terminal.Console.Executor.Aliaser.Set("execute", "exec");
		Terminal.Console.Executor.Aliaser.Set("throttle", "limit");
		Terminal.Console.Executor.Aliaser.Set("throttle", "limiter");

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Yellow, new string('·', 60))
			.AppendLine(CommandOutletColor.Cyan, "Welcome to the Polly resilience pipeline sample.".Justify(60))
			.AppendLine(CommandOutletColor.DarkGray, "https://pollydocs.org".Justify(60))
			.AppendLine(CommandOutletColor.Yellow, new string('·', 60));

		//运行终端命令执行器
		Terminal.Console.Executor.Run(splash);
	}

	static ValueTask<T> OnFallback<T>(Argument<T> argument, CancellationToken cancellation)
	{
		var content = CommandOutletContent.Create()
			.Append(CommandOutletColor.DarkGray, "[")
			.Append(CommandOutletColor.Magenta, "Fallback")
			.Append(CommandOutletColor.DarkGray, "] ");

		if(argument.HasException(out var exception))
		{
			content.Last
				.Append(CommandOutletColor.DarkYellow, exception.GetType().Name)
				.Append(CommandOutletColor.DarkGray, ": ")
				.Append(CommandOutletColor.DarkRed, exception.Message);
		}
		else
		{
			content.Last.Append(CommandOutletColor.DarkGreen, "The fallback result: ");

			if(argument.Value != null)
				content.Last.Dump(argument.Value);
			else
				content.Last.Append(CommandOutletColor.White, "NULL");
		}

		Terminal.WriteLine(content);

		return ValueTask.FromResult(argument.Value);
	}

	static async ValueTask<T> OnExecute<T>(T argument, Parameters parameters, CancellationToken cancellation)
	{
		parameters.TryGetValue<int>("round", out var round);
		parameters.TryGetValue<int>("index", out var index);

		if(round > 0 && index == 0)
			Terminal.WriteLine();

		var content = CommandOutletContent.Create()
			.Append(CommandOutletColor.DarkGray, "[")
			.Append(CommandOutletColor.Yellow, "OnExecute")
			.Append(CommandOutletColor.DarkGray, " #")
			.Append(CommandOutletColor.DarkCyan, $"{round + 1}")
			.Append(CommandOutletColor.DarkGray, ".")
			.Append(CommandOutletColor.DarkGreen, $"{index + 1}")
			.Append(CommandOutletColor.DarkGray, "] ");

		Terminal.WriteLine(content);

		try
		{
			if(parameters.TryGetValue<TimeSpan>("delay", out var delay) && delay > TimeSpan.Zero)
				await Task.Delay(delay, cancellation);
			if(parameters.TryGetValue<bool>("throw", out var throws) && throws)
				throw new InvalidOperationException();

			return argument;
		}
		finally
		{
			parameters.SetValue("index", index + 1);
		}
	}
}
