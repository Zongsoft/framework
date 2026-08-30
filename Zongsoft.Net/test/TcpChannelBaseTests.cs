using System;
using System.Net;
using System.Text;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Net.Tests;

public class TcpChannelBaseTests
{
	[Fact]
	public async Task SendAsyncPackagePacksAndFlushesOutput()
	{
		var transport = new TestDuplexPipe();
		await using var channel = new TestChannel(transport);

		await channel.SendAsync("hello");

		var result = await transport.ReadOutputAsync();
		Assert.Equal([5, .. Encoding.UTF8.GetBytes("hello")], result);
		Assert.Equal(0, channel.TotalBytesSent);
		Assert.Equal(0, channel.TotalBytesReceived);
	}

	[Fact]
	public async Task SendAsyncMemoryWritesBytesUnchanged()
	{
		var transport = new TestDuplexPipe();
		await using var channel = new TestChannel(transport);

		await channel.SendMemoryAsync(new byte[] { 3, 1, 4 });

		Assert.Equal([3, 1, 4], await transport.ReadOutputAsync());
	}

	[Fact]
	public async Task ConcurrentTypedSendFlushesSlowPath()
	{
		var writer = new BlockingPipeWriter();
		var transport = new TestDuplexPipe(writer);
		await using var channel = new TestChannel(transport);

		var first = channel.SendAsync("one");
		await writer.FirstFlushStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var second = channel.SendAsync("two");
		writer.ReleaseFirstFlush();

		await first;
		await second;
		Assert.Equal(2, writer.FlushCount);
	}

	[Fact]
	public async Task RawMemorySendDelegatesToOnSendAsync()
	{
		var transport = new TestDuplexPipe();
		await using var channel = new DispatchChannel(transport);

		await channel.SendMemoryAsync(new byte[] { 1, 2, 3 });

		Assert.Equal(1, channel.DispatchCount);
		Assert.Equal([255, 1, 2, 3], await transport.ReadOutputAsync());
	}

	[Fact]
	public async Task CloseAsyncCompletesTransportAndRejectsLaterSend()
	{
		var transport = new TestDuplexPipe();
		await using var channel = new TestChannel(transport);

		await channel.CloseAsync();

		Assert.True(channel.IsClosed);
		await Assert.ThrowsAsync<ObjectDisposedException>(() => channel.SendAsync("closed").AsTask());
	}

	[Fact]
	public async Task ReceiveAsyncInitializesOnceAndDeliversEveryFrame()
	{
		var transport = new TestDuplexPipe();
		await using var channel = new TestChannel(transport);
		var receive = channel.RunReceiveAsync();

		await transport.WriteInputAsync(new byte[] { 3, (byte)'o', (byte)'n', (byte)'e', 3, (byte)'t', (byte)'w', (byte)'o' });
		await transport.CompleteInputAsync();
		await receive;

		Assert.Equal(1, channel.InitializeCount);
		Assert.Equal(["one", "two"], channel.Received);
		Assert.True(channel.IsClosed);
	}

	private sealed class DispatchChannel(TestDuplexPipe transport) : TestChannel(transport)
	{
		public int DispatchCount { get; private set; }

		protected override ValueTask<FlushResult> OnSendAsync(PipeWriter writer, ReadOnlyMemory<byte> data, CancellationToken cancellation)
		{
			this.DispatchCount++;
			byte[] bytes = [255, .. data.Span];
			return writer.WriteAsync(bytes, cancellation);
		}
	}

	private class TestChannel(TestDuplexPipe transport) : TcpChannelBase<string>(transport, new IPEndPoint(IPAddress.Loopback, 7969))
	{
		public int InitializeCount { get; private set; }
		public List<string> Received { get; } = [];

		public Task RunReceiveAsync(CancellationToken cancellation = default) => base.ReceiveAsync(cancellation);
		public ValueTask SendMemoryAsync(ReadOnlyMemory<byte> data, CancellationToken cancellation = default) => base.SendAsync(data, cancellation);

		protected override bool OnInitialize(in string package)
		{
			this.InitializeCount++;
			return true;
		}

		protected override void Pack(PipeWriter writer, in string package)
		{
			var bytes = Encoding.UTF8.GetBytes(package);
			writer.Write([(byte)bytes.Length, .. bytes]);
		}

		protected override bool Unpack(ref ReadOnlySequence<byte> data, out string package)
		{
			if(data.IsEmpty || data.Length < data.FirstSpan[0] + 1)
			{
				package = null;
				return false;
			}

			var length = data.FirstSpan[0];
			package = Encoding.UTF8.GetString(data.Slice(1, length));
			data = data.Slice(length + 1);
			return true;
		}

		protected override ValueTask OnReceiveAsync(in string package)
		{
			this.Received.Add(package);
			return ValueTask.CompletedTask;
		}
	}

	private sealed class TestDuplexPipe : IDuplexPipe
	{
		private readonly Pipe _input = new();
		private readonly Pipe _output;
		private readonly PipeWriter _writer;

		public TestDuplexPipe()
		{
			_output = new Pipe();
			_writer = _output.Writer;
		}

		public TestDuplexPipe(PipeWriter writer)
		{
			_writer = writer;
		}

		public PipeReader Input => _input.Reader;
		public PipeWriter Output => _writer;

		public async ValueTask WriteInputAsync(ReadOnlyMemory<byte> data) => await _input.Writer.WriteAsync(data);
		public ValueTask<FlushResult> CompleteInputAsync() => CompleteAsync(_input.Writer);

		public async ValueTask<byte[]> ReadOutputAsync()
		{
			if(_output == null)
				return [];

			var result = await _output.Reader.ReadAsync();
			var bytes = result.Buffer.ToArray();
			_output.Reader.AdvanceTo(result.Buffer.End);
			return bytes;
		}

		private static async ValueTask<FlushResult> CompleteAsync(PipeWriter writer)
		{
			await writer.CompleteAsync();
			return default;
		}
	}

	private sealed class BlockingPipeWriter : PipeWriter
	{
		private readonly ArrayBufferWriter<byte> _buffer = new();
		private readonly TaskCompletionSource<FlushResult> _firstFlush = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public int FlushCount { get; private set; }
		public TaskCompletionSource FirstFlushStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public override void Advance(int bytes) => _buffer.Advance(bytes);
		public override void CancelPendingFlush() => _firstFlush.TrySetResult(new FlushResult(true, false));
		public override void Complete(Exception exception = null) { }
		public override ValueTask CompleteAsync(Exception exception = null) => ValueTask.CompletedTask;
		public override Memory<byte> GetMemory(int sizeHint = 0) => _buffer.GetMemory(sizeHint);
		public override Span<byte> GetSpan(int sizeHint = 0) => _buffer.GetSpan(sizeHint);

		public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
		{
			this.FlushCount++;

			if(this.FlushCount == 1)
			{
				this.FirstFlushStarted.TrySetResult();
				return new ValueTask<FlushResult>(_firstFlush.Task);
			}

			return new ValueTask<FlushResult>(default(FlushResult));
		}

		public void ReleaseFirstFlush() => _firstFlush.TrySetResult(default);
	}
}
