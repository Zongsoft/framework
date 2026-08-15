using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Externals.Redis.DistributedLock;

internal sealed class RunCommand : CommandBase<CommandContext>
{
	public RunCommand() : base("Run") { }

	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var settings = RunSettings.Get(context);
		var expected = settings.Workers * settings.Iterations;

		using var redis = Utility.GetRedis(context, settings.Namespace);
		await Utility.ResetAsync(redis, cancellation);

		WriteHeader(context.Output, settings);

		var stopwatch = Stopwatch.StartNew();
		var workers = StartWorkers(settings).ToArray();

		await Task.WhenAll(workers.Select(worker => worker.Completion));
		stopwatch.Stop();

		var entered = await Utility.ReadCounterAsync(redis, Utility.Keys.Entered, cancellation);
		var completed = await Utility.ReadCounterAsync(redis, Utility.Keys.Completed, cancellation);
		var violations = await Utility.ReadCounterAsync(redis, Utility.Keys.Violations, cancellation);
		var active = await Utility.ReadCounterAsync(redis, Utility.Keys.Active, cancellation);
		var failures = workers.Count(worker => worker.ExitCode != 0);

		WriteWorkerOutput(context.Output, workers);
		ReportCommand.Write(context.Output, expected, entered, completed, violations, active, failures, stopwatch.Elapsed);

		var success =
			failures == 0 &&
			entered == expected &&
			completed == expected &&
			active == 0 &&
			(settings.ExpectViolations ? violations > 0 : violations == 0);

		context.Output.WriteLine(success ? CommandOutletColor.DarkGreen : CommandOutletColor.DarkRed, success ? "Result     : PASS" : "Result     : FAIL");

		return success ? 0 : 2;
	}

	private static IEnumerable<WorkerProcess> StartWorkers(RunSettings settings)
	{
		var slaver = Utility.ResolveSlaverExecutable(settings.Slaver);

		if(string.IsNullOrWhiteSpace(slaver) || !File.Exists(slaver))
			throw new CommandException($"The slaver executable was not found. Build the slaver project first or specify --slaver:<path>.");

		for(var index = 1; index <= settings.Workers; index++)
		{
			var start = new ProcessStartInfo
			{
				FileName = slaver,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			foreach(var argument in settings.GetSlaverArguments(index))
				start.ArgumentList.Add(argument);

			var process = Process.Start(start) ?? throw new CommandException("Cannot start the slaver process.");
			yield return new WorkerProcess(process);
		}
	}

	private static void WriteHeader(ICommandOutlet output, RunSettings settings)
	{
		output.WriteLine(CommandOutletColor.Yellow, new string('-', 64));
		output.WriteLine(CommandOutletStyles.Bold, CommandOutletColor.Cyan, "Redis distributed lock");
		Utility.WritePair(output, "Scenario", settings.Scenario);
		Utility.WritePair(output, "Connection", Utility.Sanitize(settings.ConnectionString));
		Utility.WritePair(output, "Namespace", settings.Namespace);
		Utility.WritePair(output, "Workers", settings.Workers);
		Utility.WritePair(output, "Iterations", settings.Iterations);
		Utility.WritePair(output, "Expiry", settings.Expiry);
		Utility.WritePair(output, "Hold", settings.Hold);
		Utility.WritePair(output, "Slaver", Utility.ResolveSlaverExecutable(settings.Slaver));
		output.WriteLine(CommandOutletColor.Yellow, new string('-', 64));
	}

	private static void WriteWorkerOutput(ICommandOutlet output, IEnumerable<WorkerProcess> workers)
	{
		foreach(var worker in workers)
		{
			if(!string.IsNullOrWhiteSpace(worker.Output))
				output.WriteLine(CommandOutletColor.DarkYellow, worker.Output.TrimEnd());

			if(!string.IsNullOrWhiteSpace(worker.Error))
				output.WriteLine(CommandOutletColor.DarkRed, worker.Error.TrimEnd());
		}
	}

	private sealed class RunSettings
	{
		public string Scenario { get; private set; }
		public string Namespace { get; private set; }
		public string ConnectionString { get; private set; }
		public string Slaver { get; private set; }
		public int Workers { get; private set; }
		public int Iterations { get; private set; }
		public TimeSpan Expiry { get; private set; }
		public TimeSpan Hold { get; private set; }
		public TimeSpan Timeout { get; private set; }
		public bool ExpectViolations { get; private set; }

		public static RunSettings Get(CommandContext context)
		{
			var scenario = context.Options.GetValue<string>("scenario", "mutex");
			var settings = new RunSettings
			{
				Scenario = scenario,
				Namespace = Utility.GetNamespace(context),
				ConnectionString = Utility.GetConnectionString(context),
				Slaver = context.Options.GetValue<string>("slaver", null),
				Workers = context.Options.GetValue<int>("workers", 8),
				Iterations = context.Options.GetValue<int>("iterations", 80),
				Expiry = context.Options.GetValue<TimeSpan>("expiry", TimeSpan.FromSeconds(5)),
				Hold = context.Options.GetValue<TimeSpan>("hold", TimeSpan.FromMilliseconds(50)),
				Timeout = context.Options.GetValue<TimeSpan>("timeout", TimeSpan.FromMinutes(5)),
				ExpectViolations = context.Options.Switch("expect-violations"),
			};

			if(string.Equals(scenario, "expiry", StringComparison.OrdinalIgnoreCase))
			{
				settings.Expiry = TimeSpan.FromMilliseconds(300);
				settings.Hold = TimeSpan.FromMilliseconds(900);
				settings.ExpectViolations = true;
			}

			return settings;
		}

		public IEnumerable<string> GetSlaverArguments(int workerId)
		{
			yield return "run";
			yield return $"--worker-id:{workerId}";
			yield return $"--iterations:{this.Iterations}";
			yield return $"--namespace:{this.Namespace}";
			yield return $"--connection:{this.ConnectionString}";
			yield return $"--expiry:{this.Expiry}";
			yield return $"--hold:{this.Hold}";
			yield return $"--timeout:{this.Timeout}";
		}
	}

	private sealed class WorkerProcess
	{
		private readonly Process _process;

		public WorkerProcess(Process process)
		{
			_process = process;
			this.Completion = CompleteAsync(process);
		}

		public Task Completion { get; }
		public int ExitCode => _process.ExitCode;
		public string Output { get; private set; }
		public string Error { get; private set; }

		private async Task CompleteAsync(Process process)
		{
			var output = process.StandardOutput.ReadToEndAsync();
			var error = process.StandardError.ReadToEndAsync();

			await process.WaitForExitAsync();

			this.Output = await output;
			this.Error = await error;
		}
	}
}
