using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Microsoft.Extensions.Configuration;

using Zongsoft.Messaging;
using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Configuration;

using Xunit;

namespace Zongsoft.Externals.Aliyun.Tests;

public class MessageQueueTest(MessageQueueFixture fixture) : IClassFixture<MessageQueueFixture>
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	[Fact]
	public void PackagedOptionsBindDefaultCertificate()
	{
		using var configuration = new ConfigurationManager();
		configuration.AddOptionFile(Path.Combine(AppContext.BaseDirectory, "Zongsoft.Externals.Aliyun.option"));
		var options = configuration.GetOption<Options.GeneralOptions>("Externals/Aliyun/General");
		var certificate = Assert.Single(options.Certificates);
		Assert.Same(certificate, options.Certificates.Default);
		Assert.Same(certificate, options.Certificates.GetCertificate("main"));
		Assert.Equal(configuration["Externals:Aliyun:General:Certificates:main:Code"], certificate.Code);
		Assert.Equal(configuration["Externals:Aliyun:General:Certificates:main:Secret"], certificate.Secret);
	}

	[Fact]
	public async Task SubscribeDispatchesAndAcknowledgesOnlyWhenRequested()
	{
		var received = Signal();
		var release = Signal();
		var acknowledged = Signal();
		var receiving = Signal();
		var gets = 0;
		var deletes = 0;
		var transport = new Transport(async (request, cancellation) =>
		{
			Assert.Equal("/queues/alarms/messages", request.RequestUri.AbsolutePath);
			if(request.Method == HttpMethod.Delete)
			{
				Assert.Equal("?ReceiptHandle=receipt%2B%2F%3D", request.RequestUri.Query);
				Interlocked.Increment(ref deletes);
				return Response(HttpStatusCode.NoContent);
			}

			Assert.Equal(HttpMethod.Get, request.Method);
			Assert.Equal("?waitseconds=30", request.RequestUri.Query);
			Assert.Equal("2015-06-06", Assert.Single(request.Headers.GetValues("x-mns-version")));

			if(Interlocked.Increment(ref gets) == 1)
				return Envelope();

			receiving.TrySetResult();
			return await BlockAsync(cancellation);
		});
		await using var queue = fixture.CreateQueue(transport);
		var handler = new Handler(async (message, cancellation) =>
		{
			Assert.Equal("alarm-1", message.Identifier);
			Assert.Equal("alarm", Encoding.UTF8.GetString(message.Data));
			received.TrySetResult();
			await release.Task.WaitAsync(cancellation);
			await message.AcknowledgeAsync(cancellation);
			acknowledged.TrySetResult();
		});
		await using var consumer = await queue.SubscribeAsync("alarm", handler, new MessageSubscribeOptions(MessageReliability.LeastOnce));

		try
		{
			await received.Task.WaitAsync(_timeout);
			Assert.Same(consumer, await queue.SubscribeAsync("alarm", handler, new MessageSubscribeOptions(MessageReliability.LeastOnce)));
			Assert.Equal(0, Volatile.Read(ref deletes));
			Assert.Equal(1, Volatile.Read(ref gets));
		}
		finally { release.TrySetResult(); }

		await acknowledged.Task.WaitAsync(_timeout);
		await receiving.Task.WaitAsync(_timeout);
		await consumer.UnsubscribeAsync();
		Assert.Equal(1, deletes);
		Assert.True(consumer.IsClosed);
		Assert.Empty(queue.Subscribers);
		Assert.False(transport.IsDisposed);
		Assert.All(transport.Contents, content => Assert.True(content.IsDisposed));
	}

	[Fact]
	public async Task GuarderStartsConsumptionAndReleasesOnlyItsSubscription()
	{
		var acknowledged = Signal();
		var receiving = Signal();
		var gets = 0;
		var transport = new Transport(async (request, cancellation) =>
		{
			if(request.Method == HttpMethod.Delete)
				return Response(HttpStatusCode.NoContent);

			if(Interlocked.Increment(ref gets) == 1)
				return Envelope();

			receiving.TrySetResult();
			return await BlockAsync(cancellation);
		});
		await using var queue = fixture.CreateQueue(transport);
		await using var guarder = new MessageQueueGuarder("alarms")
		{
			Queue = queue,
			Handler = new Handler(async (message, cancellation) =>
			{
				await message.AcknowledgeAsync(cancellation);
				acknowledged.TrySetResult();
			}),
		};
		await guarder.StartAsync(["alarm"]);
		await acknowledged.Task.WaitAsync(_timeout);
		await receiving.Task.WaitAsync(_timeout);
		var consumer = Assert.Single(queue.Subscribers);
		await guarder.StopAsync().WaitAsync(_timeout);
		Assert.True(consumer.IsDisposed);
		Assert.Empty(queue.Subscribers);
		Assert.False(transport.IsDisposed);
	}

	[Fact]
	public async Task UnsubscribeCancelsReceiveAndAllowsResubscription()
	{
		var first = Signal();
		var second = Signal();
		var calls = 0;
		var cancelled = 0;
		var transport = new Transport(async (_, cancellation) =>
		{
			(Interlocked.Increment(ref calls) == 1 ? first : second).TrySetResult();

			try { return await BlockAsync(cancellation); }
			finally { Interlocked.Increment(ref cancelled); }
		});
		await using var queue = fixture.CreateQueue(transport);
		var handler = new Handler((_, _) => ValueTask.CompletedTask);
		await using var consumer = await queue.SubscribeAsync("alarm", handler);
		await first.Task.WaitAsync(_timeout);
		await consumer.UnsubscribeAsync().AsTask().WaitAsync(_timeout);
		Assert.Equal(1, cancelled);
		Assert.False(transport.IsDisposed);

		await using var replacement = await queue.SubscribeAsync("alarm", handler);
		await second.Task.WaitAsync(_timeout);
		Assert.NotSame(consumer, replacement);
		await queue.DisposeAsync().AsTask().WaitAsync(_timeout);
		Assert.Equal(2, cancelled);
		Assert.True(replacement.IsDisposed);
		Assert.True(transport.IsDisposed);
	}

	[Fact]
	public async Task QueueDisposalWaitsForActiveHandlerBeforeDisposingTransport()
	{
		var handling = Signal();
		var cancelled = Signal();
		var release = Signal();
		var transport = new Transport((_, _) => Task.FromResult(Envelope()));
		await using var queue = fixture.CreateQueue(transport);
		await using var consumer = await queue.SubscribeAsync(new Handler(async (_, cancellation) =>
		{
			using var registration = cancellation.Register(() => cancelled.TrySetResult());
			handling.TrySetResult();
			await release.Task;
		}));
		await handling.Task.WaitAsync(_timeout);
		var disposal = queue.DisposeAsync().AsTask();

		try
		{
			await cancelled.Task.WaitAsync(_timeout);
			Assert.False(disposal.IsCompleted);
			Assert.False(transport.IsDisposed);
		}
		finally { release.TrySetResult(); }

		await disposal.WaitAsync(_timeout);
		Assert.True(transport.IsDisposed);
		Assert.True(consumer.IsDisposed);
	}

	[Theory]
	[InlineData("empty")]
	[InlineData("receive")]
	[InlineData("handler")]
	[InlineData("acknowledge")]
	public async Task PollingContinuesAfterEmptyQueueOrFailure(string failure)
	{
		var completed = Signal();
		var gets = 0;
		var deletes = 0;
		var handled = 0;
		var transport = new Transport(async (request, cancellation) =>
		{
			if(request.Method == HttpMethod.Delete)
			{
				if(Interlocked.Increment(ref deletes) == 1 && failure == "acknowledge")
					return Error("ReceiptHandleError");

				return Response(HttpStatusCode.NoContent);
			}

			var count = Interlocked.Increment(ref gets);
			if(count > 2)
				return await BlockAsync(cancellation);

			if(count == 1 && failure == "empty")
				return Error("MessageNotExist", HttpStatusCode.NotFound);
			if(count == 1 && failure == "receive")
				return Error("ServiceUnavailable", HttpStatusCode.ServiceUnavailable);

			return Envelope();
		});
		await using var queue = fixture.CreateQueue(transport);
		await using var consumer = await queue.SubscribeAsync(new Handler(async (message, cancellation) =>
		{
			if(Interlocked.Increment(ref handled) == 1 && failure == "handler")
				throw new InvalidOperationException("Injected handler failure.");

			await message.AcknowledgeAsync(cancellation);
			completed.TrySetResult();
		}), new MessageSubscribeOptions(MessageReliability.LeastOnce));
		await completed.Task.WaitAsync(_timeout);
		await consumer.UnsubscribeAsync();
		Assert.Equal(failure == "acknowledge" ? 2 : 1, deletes);
		Assert.Equal(failure is "handler" or "acknowledge" ? 2 : 1, handled);
		Assert.All(transport.Contents, content => Assert.True(content.IsDisposed));
	}

	[Theory]
	[InlineData("produce", true)]
	[InlineData("produce", false)]
	[InlineData("receive", true)]
	[InlineData("receive", false)]
	[InlineData("acknowledge", true)]
	[InlineData("acknowledge", false)]
	[InlineData("delay", true)]
	[InlineData("delay", false)]
	public async Task HttpFailuresPropagateAndReleaseContent(string operation, bool structured)
	{
		var transport = new Transport((_, _) => Task.FromResult(structured ? Error("AccessDenied") : Response(HttpStatusCode.BadGateway, "upstream unavailable")));
		await using var queue = fixture.CreateQueue(transport);

		if(structured)
			Assert.Equal("AccessDenied", (await Assert.ThrowsAsync<AliyunException>(ExecuteAsync)).Code);
		else
			Assert.Equal(HttpStatusCode.BadGateway, (await Assert.ThrowsAsync<HttpRequestException>(ExecuteAsync)).StatusCode);

		Assert.True(Assert.Single(transport.Contents).IsDisposed);

		async Task ExecuteAsync()
		{
			switch(operation)
			{
				case "produce":
					await queue.ProduceAsync("alarm", new byte[] { 1 }, new MessageEnqueueOptions(TimeSpan.FromMinutes(1)));
					break;
				case "receive":
					await queue.DequeueAsync(null);
					break;
				case "acknowledge":
					await queue.AcknowledgeAsync("receipt", TimeSpan.Zero);
					break;
				case "delay":
					await queue.DelayAsync("receipt", TimeSpan.FromMinutes(1));
					break;
			}
		}
	}

	[Theory]
	[InlineData(0, "?waitseconds=0")]
	[InlineData(1.1, "?waitseconds=2")]
	[InlineData(60, "?waitseconds=30")]
	[InlineData(-1, "")]
	public async Task ReceiveUsesBoundedPollingTimeout(double seconds, string query)
	{
		var transport = new Transport((request, _) =>
		{
			Assert.Equal(query, request.RequestUri.Query);
			return Task.FromResult(Error("MessageNotExist", HttpStatusCode.NotFound));
		});
		await using var queue = fixture.CreateQueue(transport);
		Assert.True((await queue.DequeueAsync(new(TimeSpan.FromSeconds(seconds)))).IsEmpty);
	}

	[Fact]
	public async Task ProducePreservesDelayAndPayload()
	{
		var transport = new Transport(async (request, cancellation) =>
		{
			Assert.Equal(HttpMethod.Post, request.Method);
			var xml = System.Xml.Linq.XDocument.Parse(await request.Content.ReadAsStringAsync(cancellation));
			var ns = xml.Root.Name.Namespace;
			Assert.Equal("alarm", Encoding.UTF8.GetString(Convert.FromBase64String(xml.Root.Element(ns + "MessageBody").Value)));
			Assert.Equal("1800", xml.Root.Element(ns + "DelaySeconds").Value);
			return Response(HttpStatusCode.Created, "<Message><MessageId>queued-1</MessageId></Message>");
		});
		await using var queue = fixture.CreateQueue(transport);
		Assert.Equal("queued-1", await queue.ProduceAsync("alarm", Encoding.UTF8.GetBytes("alarm"), new MessageEnqueueOptions(TimeSpan.FromMinutes(30))));
		Assert.True(Assert.Single(transport.Contents).IsDisposed);
	}

	private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
	private static async Task<HttpResponseMessage> BlockAsync(CancellationToken cancellation)
	{
		await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, cancellation);
		throw new InvalidOperationException();
	}
	private static HttpResponseMessage Envelope() => Response(HttpStatusCode.OK, "<Message><MessageId>alarm-1</MessageId><ReceiptHandle>receipt+/=</ReceiptHandle><MessageBody>YWxhcm0=</MessageBody></Message>");
	private static HttpResponseMessage Error(string code, HttpStatusCode status = HttpStatusCode.Forbidden) => Response(status, $"<Error><Code>{code}</Code><Message>Injected failure.</Message></Error>");
	private static HttpResponseMessage Response(HttpStatusCode status, string content = "") => new(status) { Content = new Content(content) };

	private sealed class Handler(Func<Message, CancellationToken, ValueTask> handle) : HandlerBase<Message>
	{
		protected override ValueTask OnHandleAsync(Message argument, Parameters parameters, CancellationToken cancellation) => handle(argument, cancellation);
	}

	private sealed class Transport(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
	{
		public bool IsDisposed { get; private set; }
		public ConcurrentQueue<Content> Contents { get; } = new();

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
		{
			var response = await send(request, cancellation);
			this.Contents.Enqueue((Content)response.Content);
			return response;
		}

		protected override void Dispose(bool disposing)
		{
			this.IsDisposed = true;
			base.Dispose(disposing);
		}
	}

	private sealed class Content(string value) : StringContent(value)
	{
		public bool IsDisposed { get; private set; }
		protected override void Dispose(bool disposing)
		{
			this.IsDisposed = true;
			base.Dispose(disposing);
		}
	}
}
