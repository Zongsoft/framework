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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.ZeroMQ library.
 *
 * The Zongsoft.Messaging.ZeroMQ is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.ZeroMQ is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.ZeroMQ library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Plugins;
using Zongsoft.Components;
using Zongsoft.Configuration;

using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroPluginTests
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
		var server = Assert.Single(scope.Application.Workers.OfType<ZeroQueueServer>());
		Assert.Same(scope.Application.PluginTree.Find("/Workbench/Messaging/Zero/QueueServer").UnwrapValue(ObtainMode.Auto), server);
		Assert.True(server.Enabled);
		Assert.Equal(scope.Discovery, server.Discovery);
		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.Equal("true", scope.Configuration["Messaging:ZeroMQ:Server:Enabled"]);
		Assert.Equal(scope.Discovery, scope.Configuration["Messaging:ZeroMQ:Server:Discovery"]);
		Assert.Equal("custom-broker", server.Name);
		Assert.Equal("custom-store", server.Storage);
		Assert.Same(scope.Factory, server.Storages);

		var bindings = XDocument.Load(Path.Combine(scope.DirectoryPath, "plugins", "zeromq", "Zongsoft.Messaging.ZeroMQ-daemon.option"))
			.Nodes().OfType<XProcessingInstruction>().Where(instruction => instruction.Target == "zongsoft.tools.binding")
			.Select(instruction => XElement.Parse($"<binding {instruction.Data} />")).ToArray();
		Assert.Equal(4, bindings.Length);

		foreach(var role in new[] { "discovery", "control", "incoming", "outgoing" })
		{
			var binding = Assert.Single(bindings, declaration => (string)declaration.Attribute("name") == $"zeromq/{role}");
			Assert.Equal("Messaging/ZeroMQ/Server", (string)binding.Attribute("path"));
			Assert.Equal("enabled:true", (string)binding.Attribute("filter"));
			Assert.Equal("${this." + role + "}", (string)binding.Attribute("template"));
			Assert.Equal("tcp", (string)binding.Attribute("protocols"));
			Assert.Null(binding.Attribute("required"));
			var address = role switch
			{
				"discovery" => server.Discovery,
				"control" => server.Control,
				"incoming" => server.Incoming,
				_ => server.Outgoing,
			};
			Assert.Equal(address, scope.Configuration[$"Messaging:ZeroMQ:Server:{role}"]);
		}

		await server.StartAsync([]);
		Assert.Equal(WorkerState.Running, server.State);
		Assert.Equal(new Uri(server.Control).Port, ZeroTestUtility.GetServerPorts(scope.Port).Control);

		using(var queue = ZeroTestUtility.CreateQueue(scope.Port, "plugin-events"))
		{
			var transport = Assert.IsType<ZeroQueueEventChannel>(scope.Application.PluginTree.Find("/Workbench/Events/Exchanger/Channels/ZeroMQ").UnwrapValue(ObtainMode.Never));
			transport.Queue = queue;
			var dispatcher = Assert.IsType<EventDispatcher>(EventExchanger.Instance.Dispatcher);
			var original = dispatcher.Pipeline;
			var pipeline = new RecordingPipeline();
			dispatcher.Pipeline = pipeline;

			try
			{
				await EventExchanger.Instance.StartAsync([]);
				Assert.Equal(WorkerState.Running, EventExchanger.Instance.State);
				Assert.Single(queue.Subscribers);
				Assert.True(queue.Subscribers.TryGetValue("Events", out _));

				using var transaction = new Data.Transaction();
				await EventExchanger.Instance.ExchangeAsync(new EventContext<int>(new EventRegistry(), "Changed", 1), CancellationToken.None);

				Assert.Equal(0, pipeline.Delivered);

				transaction.Commit();
				await EventExchanger.Instance.StopAsync([]);

				Assert.Equal(1, pipeline.Delivered);
				Assert.Empty(queue.Subscribers);

				await EventExchanger.Instance.StartAsync([]);

				Assert.Single(queue.Subscribers);
			}
			finally
			{
				await EventExchanger.Instance.StopAsync([]);
				dispatcher.Pipeline = original;
			}
		}

		using(var publisher = ZeroTestUtility.CreateQueue(scope.Port, "plugin-publisher"))
		using(var subscriber = ZeroTestUtility.CreateQueue(scope.Port, "plugin-subscriber"))
		using(var messages = new MessageBuffer())
		{
			var topic = $"tests/plugin/{Guid.NewGuid():N}";
			await using var consumer = await subscriber.SubscribeAsync(topic, messages, new MessageSubscribeOptions(MessageReliability.LeastOnce));
			var payload = Encoding.UTF8.GetBytes("Configured daemon message");
			await ZeroTestUtility.PublishUntilAcceptedAsync(publisher, topic, payload, new MessageEnqueueOptions(MessageReliability.LeastOnce));
			var message = await messages.ReceiveAsync(TimeSpan.FromSeconds(5));

			Assert.Equal(topic, message.Topic);
			Assert.Equal(payload, message.Data);
			await message.AcknowledgeAsync();
			Assert.Equal("custom-store", scope.Factory.LastName);
			Assert.Equal(1, scope.Factory.Count);
		}

		await server.StopAsync([]);
		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.True(ZeroTestUtility.CanBindZeroMq(scope.Port));
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
		Assert.Empty(scope.Application.Workers.OfType<ZeroQueueServer>());
		var server = Assert.IsType<ZeroQueueServer>(scope.Application.PluginTree.Find("/Workbench/Messaging/Zero/QueueServer").UnwrapValue(ObtainMode.Auto));
		Assert.False(server.Enabled);
		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.Equal(scope.Discovery, server.Discovery);
		Assert.Equal("false", scope.Configuration["Messaging:ZeroMQ:Server:Enabled"]);

		var bindings = XDocument.Load(Path.Combine(scope.DirectoryPath, "plugins", "zeromq", "Zongsoft.Messaging.ZeroMQ-daemon.option"))
			.Nodes().OfType<XProcessingInstruction>().Where(instruction => instruction.Target == "zongsoft.tools.binding").ToArray();
		Assert.Equal(4, bindings.Length);
		Assert.All(bindings, binding => Assert.Equal("enabled:true", (string)XElement.Parse($"<binding {binding.Data} />").Attribute("filter")));
		Assert.Equal(0, scope.Factory.Count);
		Assert.True(ZeroTestUtility.CanBindZeroMq(scope.Port));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("Channel")]
	[InlineData("Filtering")]
	public async Task ClientPluginDoesNotCreateServer(string prebuilt = null)
	{
		if(!Global.IsTestingEnabled)
			return;

		if(!TestProgram.IsPluginProbe)
		{
			await RunProbeAsync(prebuilt == null ? nameof(ClientPluginDoesNotCreateServer) : $"{nameof(ClientPluginDoesNotCreateServer)}.{prebuilt}");
			return;
		}

		using var scope = new PluginScope(false, false);
		var exposed = prebuilt == null ? null : scope.Application.PluginTree.Find(prebuilt == "Channel" ? "/Workbench/Events/Exchanger/Channels/ZeroMQ" : "/Workbench/Events/Exchanger/Channels/ZeroMQ/Filtering").UnwrapValue(ObtainMode.Auto);
		scope.Workbench.Open();
		Assert.Empty(scope.Application.Workers.OfType<ZeroQueueServer>());
		Assert.Null(scope.Application.PluginTree.Find("/Workbench/Messaging/Zero/QueueServer"));
		Assert.Null(scope.Configuration["Messaging:ZeroMQ:Server:Discovery"]);
		Assert.NotNull(scope.Application.PluginTree.Find("/Workbench/Configuration/ConnectionSettings/Drivers/ZeroMQ"));
		Assert.Equal("ZeroMQ", scope.Configuration["Messaging:ConnectionSettings:ZeroMQ:Driver"]);
		var channel = Assert.IsType<ZeroQueueEventChannel>(Assert.Single(EventExchanger.Instance.Channels));
		var transport = channel;
		Assert.Null(scope.Application.PluginTree.Find("/Workbench/Events/Exchanger/Transports"));
		Assert.Same(transport, scope.Application.PluginTree.Find("/Workbench/Events/Exchanger/Channels/ZeroMQ").UnwrapValue(ObtainMode.Never));
		Assert.Same(transport.Filtering, scope.Application.PluginTree.Find("/Workbench/Events/Exchanger/Channels/ZeroMQ/Filtering").UnwrapValue(ObtainMode.Never));
		Assert.Same(channel, scope.Application.PluginTree.Find("/Workbench/Events/Exchanger/Channels/ZeroMQ").UnwrapValue(ObtainMode.Never));

		if(prebuilt != null)
			Assert.Same(prebuilt == "Channel" ? transport : (object)transport.Filtering, exposed);

		Assert.True(ZeroTestUtility.CanBindZeroMq(scope.Port));
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
		start.ArgumentList.Add("--zeromq-plugin-probe");
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
			using var endpoints = ZeroTestUtility.CreateServer(storages: new TestMessageStorageFactory(new MemoryMessageStorage()));
			this.Discovery = endpoints.Discovery;
			this.Port = (ushort)new Uri(this.Discovery).Port;
			this.Factory = new TestMessageStorageFactory(new MemoryMessageStorage());
			this.DirectoryPath = Path.Combine(Path.GetTempPath(), $"zongsoft-zeromq-plugins-{Guid.NewGuid():N}");
			var directory = Path.Combine(this.DirectoryPath, "plugins", "zeromq");
			Directory.CreateDirectory(directory);
			File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Main.plugin"), Path.Combine(this.DirectoryPath, "plugins", "Main.plugin"));
			Copy("Zongsoft.Messaging.ZeroMQ.plugin");
			Copy("Zongsoft.Messaging.ZeroMQ.option");

			if(daemon)
			{
				Copy("Zongsoft.Messaging.ZeroMQ-daemon.plugin");
				Copy("Zongsoft.Messaging.ZeroMQ.Storage-daemon.plugin");
				var option = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Zongsoft.Messaging.ZeroMQ-daemon.option"));
				var server = option.Root.Element("option").Element("server");
				server.SetAttributeValue("enabled", enabled);
				server.SetAttributeValue("name", "custom-broker");
				server.SetAttributeValue("storage", "custom-store");
				server.SetAttributeValue("discovery", endpoints.Discovery);
				server.SetAttributeValue("control", endpoints.Control);
				server.SetAttributeValue("incoming", endpoints.Incoming);
				server.SetAttributeValue("outgoing", endpoints.Outgoing);
				option.Save(Path.Combine(directory, "Zongsoft.Messaging.ZeroMQ-daemon.option"));
			}

			this.Configuration = new ConfigurationManager();
			var options = new PluginOptions(this.Configuration, this.DirectoryPath, "Development");
			((IConfigurationBuilder)this.Configuration).Add(new PluginConfigurationSource(options));
			var services = new ServiceCollection();
			services.AddSingleton<IConfiguration>(this.Configuration);
			services.AddSingleton<IConfigurationRoot>(this.Configuration);
			services.AddSingleton<IMessageQueueProvider, ZeroQueueProvider>();
			var provider = new Services.ServiceProviderFactory().CreateServiceProvider(services);
			_services = (IDisposable)provider;
			this.Application = new TestApplicationContext(provider, options);
			this.Application.Initialize();
			this.Application.PluginTree.Mount("/Workspace/Messaging/Storages/Redis", this.Factory);
			this.Workbench = new TestWorkbench(this.Application);
			//关闭工作台会退出进程；测试显式停止实际工作器，仅取消工作台退出。
			this.Workbench.Closing += (_, args) => args.Cancel = true;

			void Copy(string name) => File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", name), Path.Combine(directory, name));
		}

		public ushort Port { get; }
		public string Discovery { get; }
		public TestMessageStorageFactory Factory { get; }
		public string DirectoryPath { get; }
		public ConfigurationManager Configuration { get; }
		public TestApplicationContext Application { get; }
		public TestWorkbench Workbench { get; }

		public void Dispose()
		{
			try
			{
				if(this.Application.PluginTree.Find("/Workbench/Messaging/Zero/QueueServer")?.UnwrapValue(ObtainMode.Never) is IDisposable server)
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
	private sealed class RecordingPipeline : IFeaturePipeline<EventContext>
	{
		public int Delivered { get; private set; }
		public IEnumerable<IFeature> Features => [];
		public void Execute(Action<EventContext> executor, EventContext argument) => throw new NotSupportedException();
		public void Execute(Action<EventContext, Collections.Parameters> executor, EventContext argument, Collections.Parameters parameters) => throw new NotSupportedException();
		public ValueTask ExecuteAsync(Func<EventContext, CancellationToken, ValueTask> executor, EventContext argument, CancellationToken cancellation = default) =>
			this.ExecuteAsync((context, _, token) => executor(context, token), argument, null, cancellation);
		public async ValueTask ExecuteAsync(Func<EventContext, Collections.Parameters, CancellationToken, ValueTask> executor, EventContext argument, Collections.Parameters parameters, CancellationToken cancellation = default)
		{
			await executor(argument, parameters, cancellation);
			this.Delivered++;
		}
	}

	private sealed class EventRegistry : EventRegistryBase
	{
		public EventRegistry() : base("PluginTests") => this.Event<int>("Changed");
	}
	private sealed class TestWorkbench(PluginApplicationContext application) : WorkbenchBase(application);
}
