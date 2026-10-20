using System;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Terminals.IntegrationTests;

internal static class TerminalProcess
{
	public static async Task RunAsync(string scenario)
	{
		var dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
		if(string.IsNullOrEmpty(dotnet))
			dotnet = "dotnet";

		var start = new ProcessStartInfo(dotnet)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8,
		};

		start.ArgumentList.Add(typeof(TestProgram).Assembly.Location);
		start.ArgumentList.Add("--terminal-input");
		start.ArgumentList.Add(scenario);

		using var process = Process.Start(start) ?? throw new InvalidOperationException("Unable to start terminal test child process.");
		using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		//保留此次启动的进程句柄；清理不按历史 PID 重新查找进程。
		_ = process.SafeHandle;

		var output = new StringBuilder();
		var error = process.StandardError.ReadToEndAsync(deadline.Token);

		try
		{
			while(await process.StandardOutput.ReadLineAsync(deadline.Token) is string line)
			{
				output.AppendLine(line);

				if(line.StartsWith("WRITE:", StringComparison.Ordinal))
				{
					await process.StandardInput.BaseStream.WriteAsync(Convert.FromBase64String(line[6..]), deadline.Token);
					await process.StandardInput.BaseStream.FlushAsync(deadline.Token);
				}
				else if(line == "EOF")
					process.StandardInput.Close();
			}

			await process.WaitForExitAsync(deadline.Token);
			var diagnostic = $"Scenario={scenario}; ExitCode={process.ExitCode}\n{output}\n{await error}";

			Assert.True(process.ExitCode == 0, diagnostic);
			Assert.Contains("PASS", output.ToString(), StringComparison.Ordinal);
		}
		finally
		{
			if(!process.HasExited)
			{
				process.Kill();
				await process.WaitForExitAsync();
			}
		}
	}
}
