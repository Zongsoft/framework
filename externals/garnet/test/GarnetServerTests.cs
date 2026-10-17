/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Garnet library.
 *
 * The Zongsoft.Externals.Garnet is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Garnet is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Garnet library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Common;
using Zongsoft.Plugins;
using Zongsoft.Components;
using Zongsoft.Configuration;
using Zongsoft.Text.Templating;

namespace Zongsoft.Externals.Garnet.Tests;

public class GarnetServerTests
{
	[Fact]
	public async Task PluginBindingMatchesListeningServer()
	{
		if(await RunProbeAsync())
			return;

		using var scope = new PluginScope();
		scope.Workbench.Open();
		var server = Assert.Single(scope.Application.Workers.OfType<GarnetServer>());
		Assert.Same(scope.Server, server);
		Assert.Equal("default", server.Setting);

		var driver = scope.Application.PluginTree.Find("/Workbench/Configuration/ConnectionSettings/Drivers/Garnet").UnwrapValue(ObtainMode.Auto);
		Assert.Same(Configuration.GarnetConnectionSettingsDriver.Instance, driver);
		Assert.Same(driver, ConnectionSettings.Drivers["Garnet"]);

		var instruction = Assert.Single(scope.Option.Nodes().OfType<XProcessingInstruction>(), node => node.Target == "zongsoft.tools.binding");
		var binding = XElement.Parse($"<binding {instruction.Data} />");
		Assert.Equal("garnet", (string)binding.Attribute("name"));
		Assert.Equal($"Externals/Garnet/{server.Setting}", (string)binding.Attribute("path"));
		Assert.Equal("true", (string)binding.Attribute("required"));

		var settings = scope.Configuration.GetOption<SettingsCollection>("/Externals/Garnet")[server.Setting];
		//工具的 this.Value 参数字典视图使用与运行实例相同的设置。
		var evaluator = new TemplateEvaluator { Providers = { new Variables { ["this"] = new { Value = settings } } } };
		var address = evaluator.Evaluate((string)binding.Attribute("template"));
		Assert.Equal($"tcp://127.0.0.1:{scope.Port}", address);
		Assert.Equal("tcp", evaluator.Evaluate((string)binding.Attribute("protocols")));

		Assert.Null(await StartAsync(server));
		Assert.Equal(WorkerState.Running, server.State);
		await PingAsync(IPAddress.Loopback, new Uri(address).Port);
		AssertReleased(IPAddress.Parse("127.0.0.2"), scope.Port);
		AssertReleased(IPAddress.Loopback, scope.AlternatePort);

		await server.StopAsync([]);
		Assert.Equal(WorkerState.Stopped, server.State);
		AssertReleased(IPAddress.Loopback, scope.Port);
	}

	[Fact]
	public async Task SelectionAndLifecycleUseOnlyNamedSettings()
	{
		if(await RunProbeAsync())
			return;

		using var scope = new PluginScope();
		var alternatePort = scope.AlternatePort;
		var server = scope.Server;
		server.Setting = "missing";

		Assert.IsType<InvalidOperationException>(await StartAsync(server));
		Assert.Equal(WorkerState.Stopped, server.State);
		AssertReleased(IPAddress.Loopback, scope.Port);
		AssertReleased(IPAddress.Loopback, alternatePort);

		server.Setting = "alternate";
		Assert.Null(await StartAsync(server));
		await PingAsync(IPAddress.Loopback, alternatePort);
		AssertReleased(IPAddress.Loopback, scope.Port);
		Assert.Throws<InvalidOperationException>(() => server.Setting = "default");
		Assert.Equal("alternate", server.Setting);

		await server.StopAsync([]);
		AssertReleased(IPAddress.Loopback, alternatePort);
		server.Setting = "default";
		Assert.Null(await StartAsync(server));
		await PingAsync(IPAddress.Loopback, scope.Port);

		((IDisposable)server).Dispose();
		Assert.True(server.IsDisposed);
		AssertReleased(IPAddress.Loopback, scope.Port);
		await Assert.ThrowsAsync<ObjectDisposedException>(() => server.StartAsync([]));

		using var named = new GarnetServer("alternate");
		Assert.Equal("alternate", named.Setting);
		Assert.Null(await StartAsync(named));
		await PingAsync(IPAddress.Loopback, alternatePort);
		await named.StopAsync([]);
		AssertReleased(IPAddress.Loopback, alternatePort);
	}

	[Fact]
	public async Task InvalidSettingsNeverStartListener()
	{
		if(await RunProbeAsync())
			return;

		using var scope = new PluginScope();
		var server = scope.Server;
		string[] invalid =
		[
			"",
			"address=127.0.0.1",
			"address=127.0.0.1:0",
			"address=127.0.0.1:65536",
			"address=127.0.0.1:-1",
			"address=127.0.0.1:abc",
			"address=127.0.0.1:6379/",
			"address=127.0.0.1:6379/x",
			"address=127.0.0.1:6379?mode=server",
			"address=127.0.0.1:6379#server",
			"address=tcp://127.0.0.1:6379",
			"address=http://127.0.0.1:6379",
			$"address=localhost:{scope.Port}",
			$"address=*:{scope.Port}",
			$"address=127.0.0.1,127.0.0.2:{scope.Port}",
			$"address=::1:{scope.Port}",
			$"address=user@127.0.0.1:{scope.Port}",
			$"bind=127.0.0.1;port={scope.Port}",
		];

		foreach(var value in invalid)
			await RejectAsync(value);

		foreach(var suffix in new[]
		{
			"bind=127.0.0.1", "port=6379", "unixsocket=~/garnet.sock", "UnixSocketPath=~/garnet.sock",
			"config-import-path=~/garnet.conf", "ConfigImportPath=~/garnet.conf",
			"CommitFrequency=invalid", "CertificateRefreshFrequency=invalid",
			"CommitFrequency=00:00:00.0000001", "CertificateRefreshFrequency=500ms",
			"CommitFrequency=-2ms", "CertificateRefreshFrequency=-1s",
			"aof-commit-freq=2147483648", "cert-refresh-freq=2147483648",
			"auth=not-a-mode", "help", "h", "version", "password=--help", "password=--version", "password=--help=true", "password=--version=true",
		})
			await RejectAsync(CreateSettings(IPAddress.Loopback, scope.Port) + suffix);

		scope.Configuration["Externals:Garnet:default:Value"] = CreateSettings(IPAddress.Loopback, scope.Port);
		Assert.Null(await StartAsync(server));
		await PingAsync(IPAddress.Loopback, scope.Port);
		await server.StopAsync([]);
		AssertReleased(IPAddress.Loopback, scope.Port);

		async Task RejectAsync(string value)
		{
			scope.Configuration["Externals:Garnet:default:Value"] = value;
			Assert.IsType<InvalidOperationException>(await StartAsync(server));
			Assert.Equal(WorkerState.Stopped, server.State);
			AssertReleased(IPAddress.Loopback, scope.Port);
		}
	}

	[Fact]
	public async Task CancellationDoesNotOpenListener()
	{
		if(await RunProbeAsync())
			return;

		using var scope = new PluginScope();
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		Assert.IsAssignableFrom<OperationCanceledException>(await StartAsync(scope.Server, cancellation.Token));
		Assert.Equal(WorkerState.Stopped, scope.Server.State);
		AssertReleased(IPAddress.Loopback, scope.Port);

		Assert.Null(await StartAsync(scope.Server));
		await PingAsync(IPAddress.Loopback, scope.Port);
		await scope.Server.StopAsync([]);
		AssertReleased(IPAddress.Loopback, scope.Port);
	}

	[Fact]
	public async Task FailedStartCanRetryAfterPortIsReleased()
	{
		if(await RunProbeAsync())
			return;

		using var scope = new PluginScope();
		var server = scope.Server;

		using(var occupied = new TcpListener(IPAddress.Loopback, scope.Port))
		{
			occupied.Server.ExclusiveAddressUse = true;
			occupied.Start();

			Assert.IsType<SocketException>(await StartAsync(server));
			Assert.Equal(WorkerState.Stopped, server.State);

			using var client = new TcpClient();
			using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			await client.ConnectAsync(IPAddress.Loopback, scope.Port, timeout.Token);
			using var accepted = await occupied.AcceptTcpClientAsync(timeout.Token);
			Assert.True(accepted.Connected);
		}

		Assert.Null(await StartAsync(server));
		await PingAsync(IPAddress.Loopback, scope.Port);
		await server.StopAsync([]);
		AssertReleased(IPAddress.Loopback, scope.Port);

		Assert.Null(await StartAsync(server));
		await PingAsync(IPAddress.Loopback, scope.Port);
		((IDisposable)server).Dispose();
		Assert.True(server.IsDisposed);
		AssertReleased(IPAddress.Loopback, scope.Port);
	}

	[Fact]
	public async Task Ipv6ListenerRespondsAndReleasesPort()
	{
		if(await RunProbeAsync())
			return;

		Assert.True(Socket.OSSupportsIPv6);
		using var scope = new PluginScope(IPAddress.IPv6Loopback);

		Assert.Null(await StartAsync(scope.Server));
		Assert.Equal(WorkerState.Running, scope.Server.State);
		await PingAsync(IPAddress.IPv6Loopback, scope.Port);

		await scope.Server.StopAsync([]);
		Assert.Equal(WorkerState.Stopped, scope.Server.State);
		AssertReleased(IPAddress.IPv6Loopback, scope.Port);
	}

	[Fact]
	public async Task PasswordAuthenticationRejectsAnonymousRequestsAndAcceptsAuthenticatedPing()
	{
		if(await RunProbeAsync())
			return;

		using var scope = new PluginScope();
		var password = Guid.NewGuid().ToString("N");
		scope.Configuration["Externals:Garnet:default:Value"] = CreateSettings(IPAddress.Loopback, scope.Port) + $"auth=password;password={password}";

		Assert.Null(await StartAsync(scope.Server));
		Assert.Equal(WorkerState.Running, scope.Server.State);

		using(var client = new TcpClient())
		using(var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
		{
			await client.ConnectAsync(IPAddress.Loopback, scope.Port, timeout.Token);
			await using var stream = client.GetStream();
			using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
			Assert.StartsWith("-NOAUTH ", await ExchangeAsync("*1\r\n$4\r\nPING\r\n"));
			Assert.Equal("+OK", await ExchangeAsync($"*2\r\n$4\r\nAUTH\r\n${password.Length}\r\n{password}\r\n"));
			Assert.Equal("+PONG", await ExchangeAsync("*1\r\n$4\r\nPING\r\n"));

			async Task<string> ExchangeAsync(string command)
			{
				await stream.WriteAsync(Encoding.ASCII.GetBytes(command), timeout.Token);
				return await reader.ReadLineAsync(timeout.Token);
			}
		}

		await scope.Server.StopAsync([]);
		Assert.Equal(WorkerState.Stopped, scope.Server.State);
		AssertReleased(IPAddress.Loopback, scope.Port);
	}

	[Fact]
	public async Task TlsUsesApplicationRelativeCertificateAndFriendlyAliases()
	{
		if(await RunProbeAsync())
			return;

		using var scope = new PluginScope();
		using var key = RSA.Create(2048);
		var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
		using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1));
		var password = Guid.NewGuid().ToString("N");
		File.WriteAllBytes(Path.Combine(scope.DirectoryPath, "server.pfx"), certificate.Export(X509ContentType.Pfx, password));

		foreach(var (commit, refresh) in new[] { ("1s", "1m"), ("-1ms", "0") })
		{
			scope.Configuration["Externals:Garnet:default:Value"] = CreateSettings(IPAddress.Loopback, scope.Port) +
				$"EnableTLS=true;CertificateFileName=~/server.pfx;CertificatePassword={password};ClientCertificateRequired=false;" +
				$"CertificateRefreshFrequency={refresh};CommitFrequency={commit};EnableAOF=true;UseAofNullDevice=true;aof-memory=16m;aof-page-size=1m";

			Assert.Null(await StartAsync(scope.Server));
			Assert.Equal(WorkerState.Running, scope.Server.State);
			await PingAsync(IPAddress.Loopback, scope.Port, certificate);

			await scope.Server.StopAsync([]);
			Assert.Equal(WorkerState.Stopped, scope.Server.State);
			AssertReleased(IPAddress.Loopback, scope.Port);
		}
	}

	private static async Task<Exception> StartAsync(GarnetServer server, CancellationToken cancellation = default)
	{
		Exception failure = null;
		server.StateChanged += Capture;

		try
		{
			await server.StartAsync([], cancellation);
			return failure;
		}
		finally
		{
			server.StateChanged -= Capture;
		}

		void Capture(object sender, WorkerStateChangedEventArgs args) => failure = args.Exception;
	}

	private static async Task PingAsync(IPAddress address, int port, X509Certificate2 certificate = null)
	{
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		using var client = new TcpClient(address.AddressFamily);
		await client.ConnectAsync(address, port, timeout.Token);
		await using var network = client.GetStream();

		if(certificate == null)
		{
			await ExchangeAsync(network);
			return;
		}

		await using var secure = new SslStream(network, true, (_, remote, _, _) => remote?.GetCertHashString() == certificate.Thumbprint);
		await secure.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
		{
			TargetHost = "localhost",
			CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
		}, timeout.Token);
		Assert.True(secure.IsAuthenticated);
		Assert.True(secure.IsEncrypted);
		await ExchangeAsync(secure);

		async Task ExchangeAsync(Stream stream)
		{
			await stream.WriteAsync(Encoding.ASCII.GetBytes("*1\r\n$4\r\nPING\r\n"), timeout.Token);
			var response = new byte[7];
			await stream.ReadExactlyAsync(response, timeout.Token);
			Assert.Equal("+PONG\r\n", Encoding.ASCII.GetString(response));
		}
	}

	private static int GetPort(IPAddress address)
	{
		using var listener = new TcpListener(address, 0);
		listener.Start();
		return ((IPEndPoint)listener.LocalEndpoint).Port;
	}

	private static void AssertReleased(IPAddress address, int port)
	{
		using var listener = new TcpListener(address, port);
		listener.Server.ExclusiveAddressUse = true;
		listener.Start();
		Assert.Equal(port, ((IPEndPoint)listener.LocalEndpoint).Port);
	}

	private static string CreateSettings(IPAddress address, int port) =>
		$"address={(address.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{address}]" : address)}:{port};" +
		"quiet;memory=16m;page=1m;segment=16m;index=1m;no-obj=true;checkpointdir=~/checkpoints;";

	private static async Task<bool> RunProbeAsync([CallerMemberName] string scenario = null)
	{
		if(TestProgram.IsProbe)
			return false;

		var start = new ProcessStartInfo("dotnet")
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};
		start.ArgumentList.Add(typeof(TestProgram).Assembly.Location);
		start.ArgumentList.Add("--garnet-probe");
		start.ArgumentList.Add(scenario);

		using var process = Process.Start(start);
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
		var output = process.StandardOutput.ReadToEndAsync();
		var error = process.StandardError.ReadToEndAsync();

		try
		{
			await process.WaitForExitAsync(timeout.Token);
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
		Assert.True(process.ExitCode == 0, diagnostics);
		Assert.Contains($"Completed:{scenario}", diagnostics);
		return true;
	}

	private sealed class PluginScope : IDisposable
	{
		private readonly IDisposable _services;

		public PluginScope(IPAddress address = null)
		{
			address ??= IPAddress.Loopback;
			this.Port = GetPort(address);

			using(var reserved = new TcpListener(address, this.Port))
			{
				reserved.Start();
				this.AlternatePort = GetPort(address);
			}

			this.DirectoryPath = Path.Combine(Path.GetTempPath(), $"zongsoft-garnet-{Guid.NewGuid():N}");
			var directory = Path.Combine(this.DirectoryPath, "plugins", "garnet");
			Directory.CreateDirectory(directory);
			File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Main.plugin"), Path.Combine(this.DirectoryPath, "plugins", "Main.plugin"));
			File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Zongsoft.Externals.Garnet.plugin"), Path.Combine(directory, "Zongsoft.Externals.Garnet.plugin"));

			this.Option = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Zongsoft.Externals.Garnet.option"));
			this.Option.Root.Element("option").Element("server").SetAttributeValue("value", CreateSettings(address, this.Port));
			this.Option.Root.Element("option").Add(new XElement("server",
				new XAttribute("server.name", "alternate"),
				new XAttribute("value", CreateSettings(address, this.AlternatePort).Replace("address=", "Address="))));
			this.Option.Save(Path.Combine(directory, "Zongsoft.Externals.Garnet.option"));

			this.Configuration = new ConfigurationManager();
			var options = new PluginOptions(this.Configuration, this.DirectoryPath, "Development");
			((IConfigurationBuilder)this.Configuration).Add(new PluginConfigurationSource(options));
			var services = new ServiceCollection();
			services.AddSingleton<IConfiguration>(this.Configuration);
			services.AddSingleton<IConfigurationRoot>(this.Configuration);
			var provider = new Services.ServiceProviderFactory().CreateServiceProvider(services);
			_services = (IDisposable)provider;
			this.Application = new TestApplicationContext(provider, options, this.DirectoryPath);
			this.Application.Initialize();
			this.Workbench = new TestWorkbench(this.Application);
			//关闭工作台会退出进程；测试显式停止实际工作器，仅取消工作台退出。
			this.Workbench.Closing += (_, args) => args.Cancel = true;
		}

		public int Port { get; }
		public int AlternatePort { get; }
		public string DirectoryPath { get; }
		public XDocument Option { get; }
		public ConfigurationManager Configuration { get; }
		public TestApplicationContext Application { get; }
		public TestWorkbench Workbench { get; }
		public GarnetServer Server => (GarnetServer)this.Application.PluginTree.Find("/Workbench/Startup/GarnetServer").UnwrapValue(ObtainMode.Auto);

		public void Dispose()
		{
			try
			{
				if(this.Application.PluginTree.Find("/Workbench/Startup/GarnetServer")?.UnwrapValue(ObtainMode.Never) is IDisposable server)
					server.Dispose();

				this.Workbench.Dispose();
				this.Application.Dispose();
				_services.Dispose();
				((IDisposable)this.Configuration).Dispose();
			}
			finally
			{
				Directory.Delete(this.DirectoryPath, true);
			}
		}
	}

	private sealed class TestApplicationContext(IServiceProvider services, PluginOptions options, string directory) : PluginApplicationContext(services, options)
	{
		public override string ApplicationPath => directory;
	}

	private sealed class TestWorkbench(PluginApplicationContext application) : WorkbenchBase(application);
}
