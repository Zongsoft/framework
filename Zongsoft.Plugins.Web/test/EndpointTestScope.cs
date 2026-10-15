using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Plugins.Web.Tests;

internal sealed class EndpointTestScope : IDisposable
{
	private readonly bool _ownsDirectory;

	public EndpointTestScope(string directory = null)
	{
		_ownsDirectory = directory == null;
		this.DirectoryPath = directory ?? Path.Combine(Path.GetTempPath(), $"zongsoft-web-tests-{Guid.NewGuid():N}");

		if(_ownsDirectory)
		{
			Directory.CreateDirectory(Path.Combine(this.DirectoryPath, "plugins"));
			var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Main.plugin"));
			document.Root.Elements("extension").Remove();
			this.Write("plugins/Main.plugin", document.ToString());
		}
	}

	public string DirectoryPath { get; }

	public string Write(string path, string content)
	{
		var target = Path.GetFullPath(Path.Combine(this.DirectoryPath, path));
		Directory.CreateDirectory(Path.GetDirectoryName(target));
		File.WriteAllText(target, content.ReplaceLineEndings("\r\n"));
		return target;
	}

	public void Plugin(string name, string option = null)
	{
		this.Write($"plugins/{name}.plugin", $"<plugin name=\"{name}\"><manifest><dependencies><dependency name=\"Main\" /></dependencies></manifest></plugin>");
		if(option != null)
			this.Write($"plugins/{name}.option", option);
	}

	public static string Endpoint(string name, string url, string protocols = "http1") => $"<options><option path=\"Kestrel/Endpoints\"><{name} url=\"{url}\" protocols=\"{protocols}\" /></option></options>";

	public static int Port()
	{
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		return ((IPEndPoint)listener.LocalEndpoint).Port;
	}

	public static async Task RunAsync(string scenario, string argument = "")
	{
		using var scope = new EndpointTestScope();
		var start = new ProcessStartInfo("dotnet")
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			WorkingDirectory = scope.DirectoryPath,
		};
		start.ArgumentList.Add(typeof(TestProgram).Assembly.Location);
		start.ArgumentList.Add("--endpoint-probe");
		start.ArgumentList.Add(scenario);
		start.ArgumentList.Add(scope.DirectoryPath);
		start.ArgumentList.Add(argument);
		using var process = Process.Start(start);
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(45));
		var output = process.StandardOutput.ReadToEndAsync();
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

		var diagnostics = await output + await error;
		Assert.True(process.ExitCode == 0, $"{scenario}: ExitCode={process.ExitCode}\n{diagnostics}");
		var completed = Path.Combine(scope.DirectoryPath, "completed.txt");
		Assert.True(File.Exists(completed), $"{scenario}: 未执行到最终断言。\n{diagnostics}");
		Assert.Equal(scenario, File.ReadAllText(completed));
	}

	public void Dispose()
	{
		if(_ownsDirectory && Directory.Exists(this.DirectoryPath))
			Directory.Delete(this.DirectoryPath, true);
	}
}
