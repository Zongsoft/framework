using System;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Messaging.RabbitMQ.Tests;

public class RabbitQueueFactoryTests
{
	[Fact]
	public void CreateQueueWithSettingsMapsConnectionFactory()
	{
		var settings = Configuration.RabbitConnectionSettingsDriver.Instance.GetSettings("RabbitMQ",
			"server=127.0.0.1;port=5673;username=program;password=secret;container=/tests;client=Factory-Test;" +
			"queue=factory.queue;group=factory.exchange;timeout=12s;heartbeat=20s;concurrency=3;" +
			"certificate=C:\\certificates\\rabbit.pfx;reconnectable=true;");

		var options = settings.GetOptions();
		Assert.Equal("127.0.0.1", options.HostName);
		Assert.Equal(5673, options.Port);
		Assert.Equal("program", options.UserName);
		Assert.Equal("secret", options.Password);
		Assert.Equal("/tests", options.VirtualHost);
		Assert.Equal("Factory-Test", options.ClientProvidedName);
		Assert.Equal(TimeSpan.FromSeconds(12), options.SocketReadTimeout);
		Assert.Equal(TimeSpan.FromSeconds(12), options.SocketWriteTimeout);
		Assert.Equal(TimeSpan.FromSeconds(12), options.ContinuationTimeout);
		Assert.Equal(TimeSpan.FromSeconds(12), options.RequestedConnectionTimeout);
		Assert.Equal(TimeSpan.FromSeconds(12), options.HandshakeContinuationTimeout);
		Assert.Equal(TimeSpan.FromSeconds(20), options.RequestedHeartbeat);
		Assert.Equal(3, options.ConsumerDispatchConcurrency);
		Assert.Equal("C:\\certificates\\rabbit.pfx", options.Ssl.CertPath);
		Assert.True(options.AutomaticRecoveryEnabled);

		var factory = new RabbitQueueFactory();
		using var queue = factory.Create(settings);
		var rabbit = Assert.IsType<RabbitQueue>(queue);
		Assert.Equal("RabbitMQ", rabbit.Name);
		Assert.Same(settings, rabbit.Settings);
		Assert.False(rabbit.Features.Contains(MessageQueueFeature.Delay.Name));
	}

	[Fact]
	public void CreateQueueWithConnectionString()
	{
		var factory = new RabbitQueueFactory();
		using var queue = factory.Create("Events",
			"server=127.0.0.1;port=5672;username=program;password=xxxxxx;client=Factory-Connection;queue=events.queue;group=events.exchange;");

		var rabbit = Assert.IsType<RabbitQueue>(queue);
		Assert.Equal("Events", rabbit.Name);
		Assert.Equal("127.0.0.1", rabbit.Settings.Server);
		Assert.Equal((ushort)5672, rabbit.Settings.Port);
		Assert.Equal("program", rabbit.Settings.UserName);
		Assert.Equal("events.queue", rabbit.Settings.Queue);
		Assert.Equal("events.exchange", rabbit.Settings.Group);
	}

	[Fact]
	public void ServerUriPopulatesConnectionFactory()
	{
		var settings = Configuration.RabbitConnectionSettingsDriver.Instance.GetSettings("RabbitMQ",
			"server=amqp://program:secret@localhost:5678/tests;client=Uri-Test;");

		var options = settings.GetOptions();

		Assert.Equal("localhost", options.HostName);
		Assert.Equal(5678, options.Port);
		Assert.Equal("program", options.UserName);
		Assert.Equal("secret", options.Password);
		Assert.Equal("tests", options.VirtualHost);
		Assert.Equal("Uri-Test", options.ClientProvidedName);
	}

	[Fact]
	public void MissingClientGeneratesDistinctNames()
	{
		var settings = Configuration.RabbitConnectionSettingsDriver.Instance.GetSettings("RabbitMQ", "server=127.0.0.1;");

		var first = settings.GetOptions();
		var second = settings.GetOptions();

		Assert.StartsWith("C", first.ClientProvidedName);
		Assert.StartsWith("C", second.ClientProvidedName);
		Assert.NotEqual(first.ClientProvidedName, second.ClientProvidedName);
	}

	[Fact]
	public async Task ProduceWithEmptyTopicThrows()
	{
		var settings = Configuration.RabbitConnectionSettingsDriver.Instance.GetSettings("RabbitMQ", "server=127.0.0.1;client=Empty-Topic;");
		using var queue = new RabbitQueue("RabbitMQ", settings);

		await Assert.ThrowsAsync<ArgumentNullException>(() => queue.ProduceAsync(string.Empty, ReadOnlyMemory<byte>.Empty).AsTask());
	}
}
