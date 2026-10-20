using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Plugins.IntegrationTests;

public sealed class TerminalSignalTests
{
	[Fact]
	[Trait("Category", "Integration")]
	public Task IdleInterruptStopsWithoutWaitingForStandardInput() => RunAsync("idle");

	[Fact]
	[Trait("Category", "Integration")]
	public Task HandledIdleInterruptPreservesStandardInput() => RunAsync("handled-idle");

	[Fact]
	[Trait("Category", "Integration")]
	public Task ReactiveInterruptReturnsToNextCommand() => RunAsync("reactive");

	[Theory]
	[Trait("Category", "Integration")]
	[MemberData(nameof(TerminationSignals))]
	public Task TerminationBypassesReactiveAndWaitsForCleanup(string signal) => RunAsync(signal);

	[Fact]
	[Trait("Category", "Integration")]
	public Task AbortFailureIsObservedByStop() => RunAsync("failure");

	[Theory]
	[Trait("Category", "Integration")]
	[MemberData(nameof(InterruptionStops))]
	public Task StopDuringInterruptWaitsForNotification(string source, bool failure) =>
		RunAsync($"blocking-{source}{(failure ? "-failure" : string.Empty)}");

	[Fact]
	[Trait("Category", "Integration")]
	public Task DisposedLifetimeStopsReceivingInterrupts() => RunAsync("disposed");

	[Fact]
	[Trait("Category", "Integration")]
	public Task ExplicitHostLifetimeIsPreserved() => RunAsync("custom");

	public static IEnumerable<object[]> TerminationSignals()
	{
		yield return ["quit"];

		if(OperatingSystem.IsLinux())
			yield return ["terminate"];
	}

	public static IEnumerable<object[]> InterruptionStops()
	{
		yield return ["host", false];
		yield return ["host", true];

		if(OperatingSystem.IsLinux())
		{
			yield return ["quit", false];
			yield return ["quit", true];
		}
	}

	private static async Task RunAsync(string scenario)
	{
		using var scope = new HostTestDirectory();
		var dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
		if(string.IsNullOrEmpty(dotnet))
			dotnet = "dotnet";

		using var process = Start(dotnet, scenario, scope.DirectoryPath);
		using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(40));
		var output = OperatingSystem.IsWindows() ? Task.FromResult(string.Empty) : ReadOutputAsync(process);
		var error = OperatingSystem.IsWindows() ? Task.FromResult(string.Empty) : process.StandardError.ReadToEndAsync();
		var timedOut = false;

		try { await process.WaitForExitAsync(deadline.Token); }
		catch(OperationCanceledException) when(deadline.IsCancellationRequested) { timedOut = true; }
		finally
		{
			if(!process.HasExited)
			{
				process.Kill();
				await process.WaitForExitAsync();
			}
		}

		var report = Path.Combine(scope.DirectoryPath, "lifecycle.txt");
		var trace = File.Exists(report) ? await File.ReadAllTextAsync(report) : string.Empty;
		var diagnostic = $"Scenario={scenario}; ExitCode={process.ExitCode}; TimedOut={timedOut}\n{trace}\n{await output}\n{await error}";

		Assert.False(timedOut, diagnostic);

		if(scenario == "disposed")
		{
			Assert.Equal(OperatingSystem.IsWindows() ? unchecked((int)0xC000013A) : 130, process.ExitCode);
			Assert.Equal($"disposed-ready{Environment.NewLine}", trace);
		}
		else
		{
			Assert.True(process.ExitCode == 0, diagnostic);
			Assert.Equal($"terminal-signal-{scenario}{Environment.NewLine}", trace);
		}
	}

	private static Process Start(string dotnet, string scenario, string directory)
	{
		var assembly = typeof(TestProgram).Assembly.Location;
		if(OperatingSystem.IsWindows())
			return TerminalSignalNative.StartHiddenConsole(dotnet, assembly, $"terminal-signal-{scenario}", directory);

		var start = new ProcessStartInfo(dotnet)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			WorkingDirectory = directory,
		};
		start.ArgumentList.Add(assembly);
		start.ArgumentList.Add("--plugin-probe");
		start.ArgumentList.Add($"terminal-signal-{scenario}");
		start.ArgumentList.Add(directory);
		return Process.Start(start) ?? throw new InvalidOperationException("Unable to start the signal probe.");
	}

	private static async Task<string> ReadOutputAsync(Process process)
	{
		var output = new System.Text.StringBuilder();

		while(await process.StandardOutput.ReadLineAsync() is string line)
		{
			output.AppendLine(line);

			if(line.StartsWith("INPUT:", StringComparison.Ordinal))
			{
				await process.StandardInput.WriteLineAsync(line[6..]);
				await process.StandardInput.FlushAsync();
			}
		}

		return output.ToString();
	}
}
