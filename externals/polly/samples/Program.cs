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
	static void Main(string[] args)
	{
		var _features = Executor.Features;
		Executor.Pipelines = FeaturePipelineManager.Instance;

		Terminal.Console.Executor.Command("info", context =>
		{
			if(_features == null)
				return;

			var index = 0;
			var features = _features.Build();

			foreach(var feature in features)
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
			if(_features == null)
				return;

			if(context.Arguments.IsEmpty)
				_features = null;

			if(_features is FeatureBuilder builder)
			{
				var removables = new HashSet<IFeature>(context.Arguments.Count);

				foreach(var feature in builder.Features)
				{
					if(context.Arguments.Contains(GetFeatureName(feature)))
						removables.Add(feature);
				}

				if(removables.Count > 0)
				{
					foreach(var removable in removables)
						builder.Features.Remove(removable);
				}
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

			_features = _features.Retry(
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

			_features = _features.Breaker(
				duration, ratio, period, threshold
			);
		});

		Terminal.Console.Executor.Command("timeout", context =>
		{
			_features = _features.Timeout(context.Arguments.GetValue(0, TimeSpan.FromSeconds(1)));
		});

		Terminal.Console.Executor.Command("throttle", context =>
		{
			var permit = context.Options.GetValue("permit", 100);
			var queue = context.Options.GetValue("queue", 0);
			var order = context.Options.GetValue("order", ThrottleQueueOrder.Oldest);
			var limit = context.Options.GetValue("limit", string.Empty);
			var window = context.Options.GetValue("window", TimeSpan.FromSeconds(1));

			switch(limit)
			{
				case "token":
					_features = _features.Throttle(permit, queue, order, ThrottleLimiter.Token(context.Options.GetValue("threshold", 0)));
					break;
				case "fixed":
					_features = _features.Throttle(permit, queue, order, ThrottleLimiter.Fixed(window));
					break;
				case "sliding":
					_features = _features.Throttle(permit, queue, order, ThrottleLimiter.Sliding(window, context.Options.GetValue("windowSize", 0)));
					break;
				default:
					_features = _features.Throttle(permit, queue, order);
					break;
			}
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
				.Parameter("delay", context.Options.GetValue("delay", TimeSpan.Zero))
				.Parameter("throw", context.Options.Switch("throw"))
				.Parameter("result", context.Options.GetValue<object>("result", null));

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			var round = context.Options.GetValue("round", 1);

			if(context.Options.Switch("concurrency"))
			{
				Parallel.For(0, round, async i => await ExecuteAsync(executor, parameters, round, i, stopwatch, cancellation));
			}
			else
			{
				for(int i = 0; i < round; i++)
					await ExecuteAsync(executor, parameters, round, i, stopwatch, cancellation);
			}

			static async ValueTask ExecuteAsync(IExecutor executor, Parameters parameters, int round, int index, System.Diagnostics.Stopwatch stopwatch, CancellationToken cancellation)
			{
				stopwatch.Restart();

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

				if(round > 1)
				{
					Terminal.Write(CommandOutletColor.DarkGray, "[");
					Terminal.Write(CommandOutletColor.Red, $"{index + 1}");
					Terminal.Write(CommandOutletColor.DarkGray, "] ");
				}

				Terminal.Write(CommandOutletColor.DarkCyan, $"Elapsed: ");
				Terminal.WriteLine(CommandOutletColor.Green, $"{stopwatch.Elapsed}");
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

		if(round > 0 && index == 0)
			Terminal.WriteLine();

		var content = CommandOutletContent.Create()
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
