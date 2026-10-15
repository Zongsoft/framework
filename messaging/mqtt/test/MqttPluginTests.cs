using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Components;
using Zongsoft.Configuration;
using Zongsoft.Plugins;

using Xunit;

namespace Zongsoft.Messaging.Mqtt.Tests;

public class MqttPluginTests
{
	[Fact]
	public async Task DaemonPluginLoadsEnabledWorkerAndExchangesMessages()
	{
		if(!Global.IsTestingEnabled)
			return;

		if(!TestProgram.IsPluginProbe)
		{
			await RunProbeAsync(nameof(DaemonPluginLoadsEnabledWorkerAndExchangesMessages));
			return;
		}

		using var scope = new PluginScope(true, true);
		scope.Workbench.Open();
		var server = Assert.Single(scope.Application.Workers.OfType<MqttQueueServer>());
		Assert.Same(scope.Application.PluginTree.Find("/Workbench/Messaging/Mqtt/QueueServer").UnwrapValue(ObtainMode.Auto), server);
		Assert.True(server.Enabled);
		Assert.Equal($"tcp://127.0.0.1:{scope.Port}/", server.Url);
		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.Equal("true", scope.Configuration["Messaging:Mqtt:Server:Enabled"]);
		Assert.Equal($"tcp://127.0.0.1:{scope.Port}", scope.Configuration["Messaging:Mqtt:Server:Url"]);

		var binding = Assert.Single(XDocument.Load(Path.Combine(scope.DirectoryPath, "plugins", "mqtt", "Zongsoft.Messaging.Mqtt-daemon.option"))
			.Nodes().OfType<XProcessingInstruction>(), instruction => instruction.Target == "binding");
		var declaration = XElement.Parse($"<binding {binding.Data} />");
		Assert.Equal("Messaging/Mqtt/Server", (string)declaration.Attribute("path"));
		Assert.Equal("enabled:true", (string)declaration.Attribute("filter"));
		Assert.Equal("${option.url}", (string)declaration.Attribute("template"));
		Assert.Equal("tcp", (string)declaration.Attribute("protocols"));
		Assert.Null(declaration.Attribute("required"));

		await server.StartAsync([]);
		Assert.Equal(WorkerState.Running, server.State);

		using(var publisher = MqttTestUtility.CreateQueue(scope.Port, "plugin-publisher"))
		using(var subscriber = MqttTestUtility.CreateQueue(scope.Port, "plugin-subscriber"))
		using(var messages = new MqttMessageBuffer())
		{
			var topic = $"tests/plugin/{Guid.NewGuid():N}";
			await using var consumer = await subscriber.SubscribeAsync(topic, messages);
			var payload = Encoding.UTF8.GetBytes("Configured daemon message");
			await publisher.ProduceAsync(topic, payload);
			var message = await messages.ReceiveAsync(TimeSpan.FromSeconds(5));

			Assert.Equal(topic, message.Topic);
			Assert.Equal(payload, message.Data);
		}

		await server.StopAsync([]);
		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.True(MqttTestUtility.CanBind(scope.Port));
	}

	[Fact]
	public async Task DisabledDaemonPluginDoesNotStartListener()
	{
		if(!Global.IsTestingEnabled)
			return;

		if(!TestProgram.IsPluginProbe)
		{
			await RunProbeAsync(nameof(DisabledDaemonPluginDoesNotStartListener));
			return;
		}

		using var scope = new PluginScope(true, false);
		scope.Workbench.Open();
		Assert.Empty(scope.Application.Workers.OfType<MqttQueueServer>());
		var server = Assert.IsType<MqttQueueServer>(scope.Application.PluginTree.Find("/Workbench/Messaging/Mqtt/QueueServer").UnwrapValue(ObtainMode.Auto));
		Assert.False(server.Enabled);
		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.Equal($"tcp://127.0.0.1:{scope.Port}/", server.Url);
		Assert.Equal("false", scope.Configuration["Messaging:Mqtt:Server:Enabled"]);

		var binding = Assert.Single(XDocument.Load(Path.Combine(scope.DirectoryPath, "plugins", "mqtt", "Zongsoft.Messaging.Mqtt-daemon.option"))
			.Nodes().OfType<XProcessingInstruction>(), instruction => instruction.Target == "binding");
		Assert.Equal("enabled:true", (string)XElement.Parse($"<binding {binding.Data} />").Attribute("filter"));
		Assert.True(MqttTestUtility.CanBind(scope.Port));
	}

	[Fact]
	public async Task ClientPluginDoesNotCreateServer()
	{
		if(!Global.IsTestingEnabled)
			return;

		if(!TestProgram.IsPluginProbe)
		{
			await RunProbeAsync(nameof(ClientPluginDoesNotCreateServer));
			return;
		}

		using var scope = new PluginScope(false, false);
		scope.Workbench.Open();
		Assert.Empty(scope.Application.Workers.OfType<MqttQueueServer>());
		Assert.Null(scope.Application.PluginTree.Find("/Workbench/Messaging/Mqtt/QueueServer"));
		Assert.Null(scope.Configuration["Messaging:Mqtt:Server:Url"]);
		Assert.NotNull(scope.Application.PluginTree.Find("/Workbench/Configuration/ConnectionSettings/Drivers/Mqtt"));
		Assert.Equal("mqtt", scope.Configuration["Messaging:ConnectionSettings:mqtt:Driver"]);
		Assert.True(MqttTestUtility.CanBind(scope.Port));
	}

	private static async Task RunProbeAsync(string scenario)
	{
		var start = new ProcessStartInfo("dotnet")
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};
		start.ArgumentList.Add(typeof(TestProgram).Assembly.Location);
		start.ArgumentList.Add("--mqtt-plugin-probe");
		start.ArgumentList.Add(scenario);

		using var process = Process.Start(start);
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
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
	}

	private sealed class PluginScope : IDisposable
	{
		private readonly IDisposable _services;

		public PluginScope(bool daemon, bool enabled)
		{
			this.Port = MqttTestUtility.GetFreePort();
			this.DirectoryPath = Path.Combine(Path.GetTempPath(), $"zongsoft-mqtt-plugins-{Guid.NewGuid():N}");
			var directory = Path.Combine(this.DirectoryPath, "plugins", "mqtt");
			Directory.CreateDirectory(directory);
			File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Main.plugin"), Path.Combine(this.DirectoryPath, "plugins", "Main.plugin"));
			Copy("Zongsoft.Messaging.Mqtt.plugin");
			Copy("Zongsoft.Messaging.Mqtt.option");

			if(daemon)
			{
				Copy("Zongsoft.Messaging.Mqtt-daemon.plugin");
				var option = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Zongsoft.Messaging.Mqtt-daemon.option"));
				var server = option.Root.Element("option").Element("server");
				server.SetAttributeValue("enabled", enabled);
				server.SetAttributeValue("url", $"tcp://127.0.0.1:{this.Port}");
				option.Save(Path.Combine(directory, "Zongsoft.Messaging.Mqtt-daemon.option"));
			}

			this.Configuration = new ConfigurationManager();
			var options = new PluginOptions(this.Configuration, this.DirectoryPath, "Development");
			((IConfigurationBuilder)this.Configuration).Add(new PluginConfigurationSource(options));
			var services = new ServiceCollection();
			services.AddSingleton<IConfiguration>(this.Configuration);
			services.AddSingleton<IConfigurationRoot>(this.Configuration);
			var provider = new Services.ServiceProviderFactory().CreateServiceProvider(services);
			_services = (IDisposable)provider;
			this.Application = new TestApplicationContext(provider, options);
			this.Application.Initialize();
			this.Workbench = new TestWorkbench(this.Application);
			//关闭工作台会退出进程；测试显式停止实际工作器，仅取消工作台退出。
			this.Workbench.Closing += (_, args) => args.Cancel = true;

			void Copy(string name) => File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", name), Path.Combine(directory, name));
		}

		public ushort Port { get; }
		public string DirectoryPath { get; }
		public ConfigurationManager Configuration { get; }
		public TestApplicationContext Application { get; }
		public TestWorkbench Workbench { get; }

		public void Dispose()
		{
			try
			{
				if(this.Application.PluginTree.Find("/Workbench/Messaging/Mqtt/QueueServer")?.UnwrapValue(ObtainMode.Never) is IDisposable server)
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

	private sealed class TestApplicationContext(IServiceProvider services, PluginOptions options) : PluginApplicationContext(services, options);
	private sealed class TestWorkbench(PluginApplicationContext application) : WorkbenchBase(application);
}
