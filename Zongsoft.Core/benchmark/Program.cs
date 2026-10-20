using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Running;

namespace Zongsoft.Benchmarks;

internal class Program
{
	static int Main(string[] args)
	{
		string baseline = null, candidate = null, preset = null;
		var arguments = new List<string>();

		for(int index = 0; index < args.Length; index++)
		{
			switch(args[index])
			{
				case "--compare":
					if(baseline != null || index + 2 >= args.Length)
						return Fail("Use --compare <baseline DLL> <candidate DLL> exactly once.");

					baseline = args[++index];
					candidate = args[++index];
					break;
				case "--compare-job":
					if(preset != null || index + 1 >= args.Length)
						return Fail("Use --compare-job Dry|Short|Default at most once.");

					preset = args[++index];
					break;
				default:
					arguments.Add(args[index]);
					break;
			}
		}

		if(baseline == null)
		{
			if(preset != null)
				return Fail("--compare-job requires --compare.");

			var configuration = ManualConfig.Create(DefaultConfig.Instance).AddJob(
				Job.Default.WithMsBuildArguments("/p:GeneratePackageOnBuild=false").AsMutator());
			return Run(args, configuration);
		}

		if(arguments.Any(IsComparisonConflict))
			return Fail("Comparison uses isolated .NET 8 jobs. Use --compare-job instead of --job; --runtimes and --inProcess are unsupported.");

		var job = (preset ?? "Default").ToLowerInvariant() switch
		{
			"dry" => Job.Dry,
			"short" => Job.ShortRun,
			"default" => Job.Default,
			_ => null,
		};

		if(job == null)
			return Fail("--compare-job must be Dry, Short, or Default.");

		try
		{
			baseline = Path.GetFullPath(baseline);
			candidate = Path.GetFullPath(candidate);
		}
		catch(Exception exception) when(exception is ArgumentException or NotSupportedException or PathTooLongException)
		{
			return Fail("Comparison requires two valid DLL paths.");
		}

		if(!File.Exists(baseline) || !File.Exists(candidate))
			return Fail("Both comparison DLLs must exist before starting the benchmark.");
		if(string.Equals(baseline, candidate, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
			return Fail("Baseline and candidate must be saved in different locations.");

		//Different MSBuild arguments create distinct BDN build partitions and output directories.
		var config = ManualConfig.Create(DefaultConfig.Instance).AddJob(
			CreateJob(job, "Baseline", baseline).AsBaseline(),
			CreateJob(job, "Candidate", candidate));

		Console.WriteLine($"Baseline: {baseline}");
		Console.WriteLine($"Candidate: {candidate}");
		return Run(arguments.ToArray(), config);
	}

	private static int Run(string[] args, IConfig configuration)
	{
		var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, configuration).ToArray();
		if(args.Contains("--list"))
			return 0;

		return summaries.Length > 0 && summaries.All(summary => !summary.HasCriticalValidationErrors && summary.Reports.All(report => report.Success)) ? 0 : 1;
	}

	private static Job CreateJob(Job preset, string name, string path) => preset
		.WithId(name)
		.WithRuntime(CoreRuntime.Core80)
		.WithMsBuildArguments($"/p:BenchmarkCorePath=\"{path}\"", "/p:GeneratePackageOnBuild=false");

	private static bool IsComparisonConflict(string argument) => argument.Split('=')[0].ToLowerInvariant() is
		"--job" or "-j" or "--runtimes" or "-r" or "--inprocess" or "-i";

	private static int Fail(string message)
	{
		Console.Error.WriteLine(message);
		return 2;
	}
}
