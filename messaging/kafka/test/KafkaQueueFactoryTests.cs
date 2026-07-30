using System;
using System.Threading.Tasks;

using Confluent.Kafka;

using Xunit;

namespace Zongsoft.Messaging.Kafka.Tests;

public class KafkaQueueFactoryTests
{
	[Fact]
	public void CreateQueueWithSettingsMapsConsumerAndProducerOptions()
	{
		var settings = Configuration.KafkaConnectionSettingsDriver.Instance.GetSettings("Kafka",
			"server=127.0.0.1:9092;client=Factory-Test;group=Factory-Group;securityProtocol=Plaintext;username=program;password=secret;" +
			"compressionType=Gzip;compressionLevel=5;isolationLevel=ReadCommitted;heartbeat=3s;timeout=12s;" +
			"transactionId=Factory-Transaction;transactionTimeout=45s;");

		var consumer = settings.GetConsumerOptions();
		Assert.Equal("127.0.0.1:9092", consumer.BootstrapServers);
		Assert.Equal("Factory-Test", consumer.ClientId);
		Assert.Equal("Factory-Group", consumer.GroupId);
		Assert.Equal(SecurityProtocol.Plaintext, consumer.SecurityProtocol);
		Assert.Equal("program", consumer.SaslUsername);
		Assert.Equal("secret", consumer.SaslPassword);
		Assert.Equal(IsolationLevel.ReadCommitted, consumer.IsolationLevel);
		Assert.Equal(3000, consumer.HeartbeatIntervalMs);
		Assert.Equal(12000, consumer.SessionTimeoutMs);

		var producer = settings.GetProducerOptions();
		Assert.Equal("127.0.0.1:9092", producer.BootstrapServers);
		Assert.Equal("Factory-Test", producer.ClientId);
		Assert.Equal(SecurityProtocol.Plaintext, producer.SecurityProtocol);
		Assert.Equal("program", producer.SaslUsername);
		Assert.Equal("secret", producer.SaslPassword);
		Assert.Equal(CompressionType.Gzip, producer.CompressionType);
		Assert.Equal(5, producer.CompressionLevel);
		Assert.Equal("Factory-Transaction", producer.TransactionalId);
		Assert.Equal(45000, producer.TransactionTimeoutMs);
		Assert.Equal(12000, producer.RequestTimeoutMs);
		Assert.Equal(12000, producer.MessageTimeoutMs);

		var factory = new KafkaQueueFactory();
		using var queue = factory.Create(settings);
		var kafka = Assert.IsType<KafkaQueue>(queue);
		Assert.Equal("Kafka", kafka.Name);
		Assert.Same(settings, kafka.Settings);
	}

	[Fact]
	public void CreateQueueWithConnectionString()
	{
		var factory = new KafkaQueueFactory();
		using var queue = factory.Create("Events", "server=127.0.0.1:9092;client=Factory-ConnectionString;group=Factory-Group;");

		var kafka = Assert.IsType<KafkaQueue>(queue);
		Assert.Equal("Events", kafka.Name);
		Assert.Equal("127.0.0.1:9092", kafka.Settings.Server);
		Assert.Equal("Factory-ConnectionString", kafka.Settings.Client);
		Assert.Equal("Factory-Group", kafka.Settings.Group);
	}

	[Fact]
	public void MissingClientAndGroupGenerateIdentifiers()
	{
		var settings = Configuration.KafkaConnectionSettingsDriver.Instance.GetSettings("Kafka", "server=127.0.0.1:9092;");

		var first = settings.GetConsumerOptions();
		var second = settings.GetConsumerOptions();
		var producer = settings.GetProducerOptions();

		Assert.StartsWith("C", first.ClientId);
		Assert.StartsWith("G", first.GroupId);
		Assert.NotEqual(first.ClientId, second.ClientId);
		Assert.NotEqual(first.GroupId, second.GroupId);
		Assert.StartsWith("C", producer.ClientId);
		Assert.NotEqual(first.ClientId, producer.ClientId);
	}

	[Fact]
	public async Task ProduceWithEmptyTopicThrows()
	{
		var settings = Configuration.KafkaConnectionSettingsDriver.Instance.GetSettings("Kafka", "server=127.0.0.1:9092;client=Empty-Topic;");
		using var queue = new KafkaQueue("Kafka", settings);

		await Assert.ThrowsAsync<ArgumentNullException>(() => queue.ProduceAsync(string.Empty, ReadOnlyMemory<byte>.Empty).AsTask());
	}
}
