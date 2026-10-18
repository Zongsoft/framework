using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Diagnostics.Protocols.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class OtlpIntegrationTest(ITestOutputHelper output)
{
	[Fact]
	public Task OfficialExportersReachHandlersAndSample() => this.RunAsync(nameof(OfficialExportersReachHandlersAndSample));

	[Fact]
	public Task GeneratedClientsReachAllSignals() => this.RunAsync(nameof(GeneratedClientsReachAllSignals));

	[Fact]
	public Task RegionReflectionAndUnknownCallsStayIsolated() => this.RunAsync(nameof(RegionReflectionAndUnknownCallsStayIsolated));

	[Fact]
	public Task CancellationReachesHandlerAndReleasesListener() => this.RunAsync(nameof(CancellationReachesHandlerAndReleasesListener));

	private async Task RunAsync(string scenario)
	{
		var directory = Path.Combine(Path.GetTempPath(), $"zongsoft-otlp-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);

		try
		{
			var start = new ProcessStartInfo("dotnet")
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				WorkingDirectory = directory,
			};
			start.ArgumentList.Add(typeof(TestProgram).Assembly.Location);
			start.ArgumentList.Add("--otlp-probe");
			start.ArgumentList.Add(scenario);
			start.ArgumentList.Add(directory);
			using var process = Process.Start(start);
			using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
			var standardOutput = process.StandardOutput.ReadToEndAsync();
			var error = process.StandardError.ReadToEndAsync();

			try
			{
				await process.WaitForExitAsync(cancellation.Token);
			}
			finally
			{
				if(!process.HasExited)
				{
					process.Kill(true);
					await process.WaitForExitAsync();
				}
			}

			var diagnostics = await standardOutput + await error;
			Assert.True(process.ExitCode == 0, $"{scenario}: ExitCode={process.ExitCode}\n{diagnostics}");
			Assert.Equal(scenario, File.ReadAllText(Path.Combine(directory, "completed.txt")));

			if(scenario == nameof(OfficialExportersReachHandlersAndSample))
			{
				output.WriteLine(diagnostics);
				Assert.Contains("sdk.counter", diagnostics);
				Assert.Contains("Value:(Object) 37", diagnostics);
			}
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}
}
