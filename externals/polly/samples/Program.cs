using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Terminals;
using Zongsoft.Collections;
using Zongsoft.Components;
using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly.Samples;

internal class Program
{
	private static readonly FeatureBuilder _features = new();

	static void Main(string[] args)
	{
		Executor.Pipelines = FeaturePipelineBuilder.Instance;

		Terminal.Console.Executor.Command("info", context =>
		{
			var index = 0;

			foreach(var feature in _features)
			{
				var content = CommandOutletContent.Create()
					.Append(CommandOutletColor.DarkGray, "[")
					.Append(CommandOutletColor.Magenta, $"{++index}")
					.Append(CommandOutletColor.DarkGray, "] ")
					.Append(CommandOutletColor.DarkYellow, feature.GetType().GetAlias(true))
					.AppendLine().Append(CommandOutletColor.DarkGray, '{');

				CommandOutletDumper.Dump(content, feature);

				content.Last.AppendLine(CommandOutletColor.DarkGray, '}');
				context.Output.WriteLine(content);
			}
		});

		Terminal.Console.Executor.Command("reset", context =>
		{
			if(context.Arguments.IsEmpty)
			{
				_features.Reset();
				return;
			}

			var removables = new HashSet<IFeature>(context.Arguments.Count);

			foreach(var feature in _features)
			{
				if(context.Arguments.Contains(GetFeatureName(feature)))
					removables.Add(feature);
			}

			if(removables.Count > 0)
			{
				foreach(var removable in removables)
					_features.Features.Remove(removable);
			}
		});

		Terminal.Console.Executor.Command("retry", context =>
		{
			var delay = context.Options.GetValue("delay", TimeSpan.Zero);
			var limit = context.Options.GetValue("limit", TimeSpan.Zero);
			var attempts = context.Options.GetValue("attempts", 3);
			var backoff = context.Options.GetValue("backoff", RetryBackoff.None);
			var jitterable = context.Options.GetValue("jitterable", false);

			if(delay <= TimeSpan.Zero)
				delay = TimeSpan.FromSeconds(1);

			_features.Retry(
				backoff,
				new RetryLatency(delay, limit),
				jitterable,
				attempts,
				Predication.Predicate<RetryArgument>(argument => argument.Value == null || argument.Exception != null)
			);
		});

		Terminal.Console.Executor.Command("breaker", context =>
		{
			var threshold = context.Options.GetValue("threshold", 10);
			var duration = context.Options.GetValue("duration", TimeSpan.Zero);
			var period = context.Options.GetValue("period", TimeSpan.Zero);
			var ratio = context.Options.GetValue("ratio", 0.5);

			_features.Breaker(
				duration, ratio, period, threshold
			);
		});

		Terminal.Console.Executor.Command("timeout", context =>
		{
			_features.Timeout(context.Arguments.GetValue(0, TimeSpan.FromSeconds(1)));
		});

		Terminal.Console.Executor.Command("throttle", context =>
		{
			var permit = context.Options.GetValue("permit", 1);
			var queue = context.Options.GetValue("queue", 0);
			var order = context.Options.GetValue("order", ThrottleQueueOrder.Oldest);
			var window = context.Options.GetValue("window", TimeSpan.FromSeconds(1));
			var handler = context.Options.Switch("handled") ? Handler.Handle<ThrottleArgument>(OnRejected) : null;

			if(context.Arguments.IsEmpty)
				_features.Throttle(permit, queue, order, null, handler);

			foreach(var argument in context.Arguments)
			{
				switch(argument)
				{
					case "token":
						_features.Throttle(permit, queue, order, ThrottleLimiter.Token(context.Options.GetValue("threshold", 0), context.Options.GetValue("period", TimeSpan.Zero)), handler);
						break;
					case "fixed":
						_features.Throttle(permit, queue, order, ThrottleLimiter.Fixed(window), handler);
						break;
					case "sliding":
						_features.Throttle(permit, queue, order, ThrottleLimiter.Sliding(window, context.Options.GetValue("windowSize", 0)), handler);
						break;
					case "concurrency":
						_features.Throttle(permit, queue, order, null, handler);
						break;
					default:
						Terminal.Console.WriteLine(CommandOutletColor.DarkMagenta, $"The specified '{argument}' is an unrecognized limiter.");
						break;
				}
			}

			static ValueTask OnRejected(ThrottleArgument argument, CancellationToken cancellation)
			{
				Console.Beep();

				var content = CommandOutletContent.Create()
					.AppendLine(CommandOutletColor.Cyan, new String('·', 50))
					.AppendLine(CommandOutletColor.DarkYellow, $"[OnRejected] {argument.Name}".Justify(50))
					.AppendLine(CommandOutletColor.Cyan, new String('·', 50));

				Terminal.Console.WriteLine(content);
				return ValueTask.CompletedTask;
			}
		});

		Terminal.Console.Executor.Command("fallback", context =>
		{
			_features.Fallback<object>(
				OnFallback,
				Predication.Predicate<object>(argument => true),
				true);
		});

		Terminal.Console.Executor.Command("execute", async (context, cancellation) =>
		{
			var executor = _features.Build<object, object>(OnExecute);
			var parameters = Parameters
				.Parameter("delay", context.Options.GetValue("delay", TimeSpan.Zero))
				.Parameter("throw", context.Options.Switch("throw"))
				.Parameter("result", context.Options.GetValue<object>("result", null));

			var round = context.Options.GetValue("round", 1);

			if(context.Options.Switch("concurrency"))
			{
				Parallel.For(0, round, async i => await ExecuteAsync(executor, parameters, round, i, cancellation));
			}
			else
			{
				for(int i = 0; i < round; i++)
					await ExecuteAsync(executor, parameters, round, i, cancellation);
			}

			static async ValueTask ExecuteAsync(IExecutor executor, Parameters parameters, int round, int index, CancellationToken cancellation)
			{
				var stopwatch = System.Diagnostics.Stopwatch.StartNew();

				try
				{
					parameters.SetValue("round", index);
					parameters.SetValue("index", 0);

					await executor.ExecuteAsync(null, parameters, cancellation);
				}
				catch(Exception ex)
				{
					Terminal.WriteLine(CommandOutletColor.DarkRed, $"[{ex.GetType().Name}] {ex.Message}");
				}

				stopwatch.Stop();

				var content = CommandOutletContent.Create();

				if(round > 1)
				{
					content
						.Append(CommandOutletColor.DarkGray, "[")
						.Append(CommandOutletColor.Red, $"{index + 1}")
						.Append(CommandOutletColor.DarkGray, "] ");
				}

				content.Last
					.Append(CommandOutletColor.DarkCyan, $"Elapsed: ")
					.Append(CommandOutletColor.Green, $"{stopwatch.Elapsed}");

				Terminal.WriteLine(content);
			}
		});

		Terminal.Console.Executor.Aliaser.Set("execute", "exe");
		Terminal.Console.Executor.Aliaser.Set("execute", "exec");

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Yellow, new string('·', 60))
			.AppendLine(CommandOutletColor.Cyan, "Welcome to the Polly resilience pipeline sample.".Justify(60))
			.AppendLine(CommandOutletColor.DarkGray, "https://pollydocs.org".Justify(60))
			.AppendLine(CommandOutletColor.Yellow, new string('·', 60));

		//运行终端命令执行器
		Terminal.Console.Executor.Run(splash);
	}

	static string GetFeatureName(IFeature feature)
	{
		const string SUFFIX = "Feature";
		const int SUFFIX_LENGTH = 7;

		ArgumentNullException.ThrowIfNull(feature);

		var name = feature.GetType().Name;
		return name.Length > SUFFIX_LENGTH &&
			name.EndsWith(SUFFIX, StringComparison.OrdinalIgnoreCase) ? name[..^SUFFIX_LENGTH] : name;
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

		var content = round > 0 && index == 0 ? CommandOutletContent.Create().AppendLine() : CommandOutletContent.Create();

		content.Last
			.Append(CommandOutletColor.DarkGray, "[")
			.Append(CommandOutletColor.Yellow, "OnExecute")
			.Append(CommandOutletColor.DarkGray, " #")
			.Append(CommandOutletColor.Red, $"{round + 1}")
			.Append(CommandOutletColor.DarkGray, ".")
			.Append(CommandOutletColor.Cyan, $"{index}")
			.Append(CommandOutletColor.DarkGray, "] ");

		Terminal.WriteLine(content);

		try
		{
			if(parameters.TryGetValue<TimeSpan>("delay", out var delay) && delay > TimeSpan.Zero)
				await Task.Delay(delay, cancellation);
			if(parameters.TryGetValue<bool>("throw", out var throws) && throws)
				throw new InvalidOperationException("🚨🚨🚨 This is a simulation of an exception thrown during execution. 🚨🚨🚨");

			return argument ?? (parameters.TryGetValue("result", out T result) ? result : default);
		}
		finally
		{
			parameters.SetValue("index", index + 1);
		}
	}
}
