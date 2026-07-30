using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using RabbitMQ.Client;

using Xunit;

namespace Zongsoft.Messaging.RabbitMQ.Tests;

public class RabbitQueueBehaviorTests
{
	[Fact]
	public async Task ProduceNormalizesTopicAndMapsMessageOptions()
	{
		var settings = Configuration.RabbitConnectionSettingsDriver.Instance.GetSettings("RabbitMQ",
			"server=127.0.0.1;client=Producer-Unit;group=unit.exchange;queue=unit.queue;");
		using var queue = new RabbitQueue("RabbitMQ", settings);
		var channel = DispatchProxy.Create<IChannel, ChannelProxy>();
		var proxy = (ChannelProxy)(object)channel;
		proxy.IsOpen = true;
		var connection = DispatchProxy.Create<IConnection, ConnectionProxy>();
		var connectionProxy = (ConnectionProxy)(object)connection;
		connectionProxy.IsOpen = true;
		ReplaceField(queue, "_channel", channel);
		ReplaceField(queue, "_connection", connection);
		var options = new MessageEnqueueOptions(priority: 7)
		{
			Expiration = TimeSpan.FromMilliseconds(2500),
		};
		options.Properties["trace"] = "trace-001";
		var payload = new byte[] { 1, 3, 5, 7 };

		var identifier = await queue.ProduceAsync("orders/created", payload, options);

		Assert.Equal("unit.exchange", proxy.Exchange);
		Assert.Equal("orders.created", proxy.RoutingKey);
		Assert.False(proxy.Mandatory);
		Assert.Equal(payload, proxy.Body.ToArray());
		Assert.NotNull(proxy.Properties);
		Assert.Equal(identifier, proxy.Properties.MessageId);
		Assert.Equal(12, identifier.Length);
		Assert.Equal((byte)7, proxy.Properties.Priority);
		Assert.Equal("2500", proxy.Properties.Expiration);
		Assert.Equal("trace-001", proxy.Properties.Headers["trace"]);
	}

	[Fact]
	public void DisposeReleasesChannelAndConnection()
	{
		var settings = Configuration.RabbitConnectionSettingsDriver.Instance.GetSettings("RabbitMQ", "server=127.0.0.1;client=Dispose-Unit;");
		var queue = new RabbitQueue("RabbitMQ", settings);
		var channel = DispatchProxy.Create<IChannel, ChannelProxy>();
		var channelProxy = (ChannelProxy)(object)channel;
		var connection = DispatchProxy.Create<IConnection, ConnectionProxy>();
		var connectionProxy = (ConnectionProxy)(object)connection;
		ReplaceField(queue, "_channel", channel);
		ReplaceField(queue, "_connection", connection);

		queue.Dispose();
		queue.Dispose();

		Assert.True(queue.IsDisposed);
		Assert.Equal(1, channelProxy.DisposeCount);
		Assert.Equal(1, connectionProxy.DisposeCount);
	}

	private static void ReplaceField<T>(object target, string name, T value)
	{
		var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
		field.SetValue(target, value);
	}

	private class ChannelProxy : DispatchProxy
	{
		public bool IsOpen { get; set; }
		public int DisposeCount { get; private set; }
		public string Exchange { get; private set; }
		public string RoutingKey { get; private set; }
		public bool Mandatory { get; private set; }
		public BasicProperties Properties { get; private set; }
		public ReadOnlyMemory<byte> Body { get; private set; }

		protected override object Invoke(MethodInfo targetMethod, object[] args)
		{
			switch(targetMethod.Name)
			{
				case "get_IsOpen":
					return this.IsOpen;
				case nameof(IDisposable.Dispose):
					this.DisposeCount++;
					return null;
				case "BasicPublishAsync":
					this.Exchange = Assert.IsType<string>(args[0]);
					this.RoutingKey = Assert.IsType<string>(args[1]);
					this.Mandatory = Assert.IsType<bool>(args[2]);
					this.Properties = Assert.IsType<BasicProperties>(args[3]);
					this.Body = Assert.IsType<ReadOnlyMemory<byte>>(args[4]);
					return ValueTask.CompletedTask;
				default:
					return Default(targetMethod.ReturnType);
			}
		}
	}

	private class ConnectionProxy : DispatchProxy
	{
		public bool IsOpen { get; set; }
		public int DisposeCount { get; private set; }

		protected override object Invoke(MethodInfo targetMethod, object[] args)
		{
			if(targetMethod.Name == "get_IsOpen")
				return this.IsOpen;

			if(targetMethod.Name == nameof(IDisposable.Dispose))
			{
				this.DisposeCount++;
				return null;
			}

			return Default(targetMethod.ReturnType);
		}
	}

	internal static object Default(Type type)
	{
		if(type == typeof(Task))
			return Task.CompletedTask;
		if(type == typeof(ValueTask))
			return ValueTask.CompletedTask;
		if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
			return typeof(Task).GetMethod(nameof(Task.FromResult)).MakeGenericMethod(type.GenericTypeArguments[0]).Invoke(null, [null]);
		if(type.IsValueType)
			return Activator.CreateInstance(type);

		return null;
	}
}
