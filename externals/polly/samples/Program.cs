using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Terminals;
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
				var content = CommandOutletContent.Create(CommandOutletColor.DarkYellow, feature.GetType().Name);
				CommandOutletDumper.Dump(content, feature);
				context.Output.Write(content);
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
			var attempts = context.Expression.Options.GetValue("attempts", 0);
			var backoff = context.Expression.Options.GetValue("backoff", RetryBackoff.None);
			var jitterable = context.Expression.Options.GetValue("jitterable", true);

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
			_features = _features.Fallback(OnFallback);
		});

		Terminal.Console.Executor.Command("execute", async (context, cancellation) =>
		{
			if(_features == null)
				return;

			var executor = _features.Build<object, object>(OnExecute);
			var parameters = Collections.Parameters
				.Parameter("delay", context.Expression.Options.GetValue("delay", TimeSpan.Zero));

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			var count = context.Expression.Options.GetValue("count", 1);

			for(int i = 0; i < count; i++)
			{
				stopwatch.Restart();

				try
				{
					await executor.ExecuteAsync(context.Value, parameters, cancellation);
				}
				catch(Exception ex)
				{
					context.Output.WriteLine(CommandOutletColor.Red, $"[{ex.GetType().Name}] {ex.Message}");
				}

				stopwatch.Stop();

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

	static ValueTask<object> OnFallback(IExecutorContext context)
	{
		Terminal.WriteLine(CommandOutletColor.Magenta, "The fallback action is executed.");
		return ValueTask.FromResult((object)null);
	}

	static async ValueTask<object> OnExecute(object argument, Collections.Parameters parameters, CancellationToken cancellation)
	{
		if(parameters != null && parameters.TryGetValue<TimeSpan>("delay", out var delay))
			await Task.Delay(delay, cancellation);

		return argument;
	}
}
