using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class XmlConfigurationTest
{
	public static IConfigurationRoot GetConfiguration1()
	{
		return new ConfigurationBuilder()
			.AddOptionFile("Configuration/Xml/OptionConfigurationTest-1.option")
			.Build();
	}

	public static IConfigurationRoot GetConfiguration2()
	{
		return new ConfigurationBuilder()
			.AddOptionFile("Configuration/Xml/OptionConfigurationTest-2.option")
			.Build();
	}

	[Fact]
	public void TestLoad1() => TestConfiguration1(GetConfiguration1());
	private static void TestConfiguration1(IConfigurationRoot configuration)
	{
		Assert.NotNull(configuration);
		Assert.NotEmpty(configuration.Providers);

		Assert.Equal("general.name", configuration.GetSection("general:name").Value);
		Assert.Equal("true", configuration.GetSection("general:intranet").Value);
		Assert.Equal("main", configuration.GetSection("general:certificates:default").Value);

		Assert.Equal("main", configuration.GetSection("general:certificates:main:name").Value);
		Assert.Equal("C001", configuration.GetSection("general:certificates:main:code").Value);
		Assert.Equal("xxxx", configuration.GetSection("general:certificates:main:secret").Value);

		Assert.Equal("test", configuration.GetSection("general:certificates:test:name").Value);
		Assert.Equal("C002", configuration.GetSection("general:certificates:test:code").Value);
		Assert.Equal("zzzz", configuration.GetSection("general:certificates:test:secret").Value);

		Assert.Equal("Shenzhen", configuration.GetSection("mobile:region").Value);
		Assert.Null(configuration.GetSection("mobile.certificate").Value);

		Assert.Equal("Alarm", configuration.GetSection("mobile:messages:alarm:name").Value);
		Assert.Equal("SMS_01", configuration.GetSection("mobile:messages:alarm:code").Value);
		Assert.Equal("Zongsoft", configuration.GetSection("mobile:messages:alarm:scheme").Value);

		Assert.Equal("400123456, 400666888", configuration.GetSection("mobile:voices:numbers").Value);
		Assert.Equal("Alarm", configuration.GetSection("mobile:voices:alarm:name").Value);
		Assert.Equal("TTS_01", configuration.GetSection("mobile:voices:alarm:code").Value);
		Assert.Equal("Password.Forget", configuration.GetSection("mobile:voices:password.forget:name").Value);
		Assert.Equal("TTS_02", configuration.GetSection("mobile:voices:password.forget:code").Value);

		Assert.Equal("wechat", configuration.GetSection("mobile:pushing:wechat:key").Value);
		Assert.Equal("A123", configuration.GetSection("mobile:pushing:wechat:code").Value);
		Assert.Equal("****", configuration.GetSection("mobile:pushing:wechat:secret").Value);

		Assert.Equal("db1", configuration.GetSection("data:connectionSettings:default").Value);
		Assert.Equal("db1", configuration.GetSection("data:connectionSettings:db1:name").Value);
		Assert.Equal("all", configuration.GetSection("data:connectionSettings:db1:mode").Value);
		Assert.Equal("mysql", configuration.GetSection("data:connectionSettings:db1:driver").Value);
		Assert.Equal("server=localhost", configuration.GetSection("data:connectionSettings:db1:value").Value);

		Assert.Equal("redis", configuration.GetSection("externals:redis:connectionSettings:redis:name").Value);
		Assert.Equal("server=127.0.0.1", configuration.GetSection("externals:redis:connectionSettings:redis:value").Value);
	}

	[Fact]
	public void TestLoad2() => TestConfiguration2(GetConfiguration2());
	private static void TestConfiguration2(IConfigurationRoot configuration)
	{
		Assert.NotNull(configuration);
		Assert.NotEmpty(configuration.Providers);

		Assert.Equal("db2", configuration.GetSection("data:connectionSettings:db2:name").Value);
		Assert.Equal("all", configuration.GetSection("data:connectionSettings:db2:mode").Value);
		Assert.Equal("MySQL", configuration.GetSection("data:connectionSettings:db2:driver").Value);
		Assert.Equal("server=192.168.0.1", configuration.GetSection("data:connectionSettings:db2:value").Value);

		Assert.Equal("avm", configuration.GetSection("messaging:queues:avm:name").Value);
		Assert.Equal("MostOnce", configuration.GetSection("messaging:queues:avm:subscription:reliability").Value);

		Assert.Equal("heartbeat", configuration.GetSection("messaging:queues:avm:subscription:heartbeat:name").Value);
		Assert.Equal("uplink", configuration.GetSection("messaging:queues:avm:subscription:uplink:name").Value);
		Assert.Equal("downlink", configuration.GetSection("messaging:queues:avm:subscription:downlink:name").Value);

		var tags = configuration.GetSection("messaging:queues:avm:subscription:heartbeat:tag").GetChildren().ToArray();
		Assert.Empty(tags);

		tags = configuration.GetSection("messaging:queues:avm:subscription:uplink:tag").GetChildren().ToArray();
		Assert.NotEmpty(tags);
		Assert.Equal(3, tags.Length);
		Assert.Contains(tags, tag => string.IsNullOrEmpty(tag.Value));
		Assert.Contains(tags, tag => tag.Value == "ping");
		Assert.Contains(tags, tag => tag.Value == "synchronize");

		tags = configuration.GetSection("messaging:queues:avm:subscription:downlink:tag").GetChildren().ToArray();
		Assert.NotEmpty(tags);
		Assert.Equal(3, tags.Length);
		Assert.Contains(tags, tag => string.IsNullOrEmpty(tag.Value));
		Assert.Contains(tags, tag => tag.Value == "control");
		Assert.Contains(tags, tag => tag.Value == "synchronize");
	}

	[Fact]
	public void TestResolve()
	{
		var configuration = GetConfiguration2();
		var options = configuration.GetOption<QueueOptions>("/Messaging/Queues/avm");

		Assert.NotNull(options);
		Assert.NotNull(options.Subscription);
		Assert.Equal(Messaging.MessageReliability.MostOnce, options.Subscription.Reliability);

		var topics = options.Subscription.Topics;
		Assert.NotEmpty(topics);
		Assert.Equal(3, topics.Count);

		Assert.True(topics.TryGetValue("heartbeat", out var topic));
		Assert.Null(topic.Tags);

		Assert.True(topics.TryGetValue("uplink", out topic));
		Assert.NotEmpty(topic.Tags);
		Assert.Equal(3, topic.Tags.Count);
		Assert.Contains(topic.Tags, string.IsNullOrEmpty);
		Assert.Contains(topic.Tags, tag => tag == "ping");
		Assert.Contains(topic.Tags, tag => tag == "synchronize");

		Assert.True(topics.TryGetValue("downlink", out topic));
		Assert.NotEmpty(topic.Tags);
		Assert.Equal(3, topic.Tags.Count);
		Assert.Contains(topic.Tags, string.IsNullOrEmpty);
		Assert.Contains(topic.Tags, tag => tag == "control");
		Assert.Contains(topic.Tags, tag => tag == "synchronize");
	}
}

public class QueueOptions
{
	public string Name { get; set; }
	public SubscriptionOptions Subscription { get; set; }

	public class SubscriptionOptions
	{
		public Messaging.MessageReliability Reliability { get; set; }

		[ConfigurationProperty]
		public TopicOptionsCollection Topics { get; set; }
	}

	public class TopicOptions
	{
		public string Name { get; set; }

		[ConfigurationProperty("tag")]
		public IList<string> Tags { get; set; }

		public override string ToString() => this.Name;
	}

	public class TopicOptionsCollection() : KeyedCollection<string, TopicOptions>(StringComparer.OrdinalIgnoreCase)
	{
		protected override string GetKeyForItem(TopicOptions topic) => topic.Name;
	}
}