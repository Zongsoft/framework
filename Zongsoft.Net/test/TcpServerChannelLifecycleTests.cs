using System;
using System.Net;
using System.Buffers;
using System.Reflection;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Communication;
using Zongsoft.Components;

namespace Zongsoft.Net.Tests;

public class TcpServerChannelLifecycleTests
{
	[Fact]
	public async Task ClientAsyncHandlerOwnsDisposablePayloadUntilCompletionAndDisposesOnce()
	{
		var handler = new DelayedHandler();
		var client = new TcpClient<DisposablePayload>(new PayloadPacketizer()) { Handler = handler };
		var channel = RuntimeHelpers.GetUninitializedObject(typeof(TcpClientChannel<DisposablePayload>));
		var clientField = channel.GetType().GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic);
		var receive = channel.GetType().GetMethod("OnReceiveAsync", BindingFlags.Instance | BindingFlags.NonPublic);
		var payload = new DisposablePayload();
		clientField.SetValue(channel, client);

		receive.Invoke(channel, [payload]);
		await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var disposedBeforeCompletion = payload.DisposeCount;

		handler.Release();
		await handler.Processing.WaitAsync(TimeSpan.FromSeconds(5));
		await payload.FirstDisposed.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(0, disposedBeforeCompletion);
		Assert.Equal(1, payload.DisposeCount);
		Assert.False(payload.SecondDisposed.Task.IsCompleted);
	}

	[Fact]
	public async Task AsyncHandlerOwnsDisposablePayloadUntilCompletionAndDisposesOnce()
	{
		using var server = new TcpServer<DisposablePayload>(new PayloadPacketizer());
		var handler = new DelayedHandler();
		var transport = new TestDuplexPipe();
		server.Handler = handler;
		var accepting = server.AcceptAsync(transport, new IPEndPoint(IPAddress.Loopback, 32123));

		await transport.WriteAndCompleteAsync(new byte[] { 1 });
		await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var payload = handler.Payload;
		var disposedBeforeCompletion = payload.DisposeCount;

		handler.Release();
		await handler.Processing.WaitAsync(TimeSpan.FromSeconds(5));
		await accepting.WaitAsync(TimeSpan.FromSeconds(5));
		await payload.FirstDisposed.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(0, disposedBeforeCompletion);
		Assert.Equal(1, payload.DisposeCount);
		Assert.False(payload.SecondDisposed.Task.IsCompleted);
	}

	private sealed class DisposablePayload : IDisposable
	{
		private int _disposeCount;

		public int DisposeCount => Volatile.Read(ref _disposeCount);
		public TaskCompletionSource FirstDisposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource SecondDisposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public void Dispose()
		{
			var count = Interlocked.Increment(ref _disposeCount);
			if(count == 1)
				this.FirstDisposed.TrySetResult();
			else if(count == 2)
				this.SecondDisposed.TrySetResult();
		}
	}

	private sealed class PayloadPacketizer : IPacketizer<DisposablePayload>
	{
		public void Pack(IBufferWriter<byte> writer, in DisposablePayload package) { }

		public bool Unpack(ref ReadOnlySequence<byte> data, out DisposablePayload package)
		{
			if(data.IsEmpty)
			{
				package = null;
				return false;
			}

			package = new DisposablePayload();
			data = data.Slice(1);
			return true;
		}
	}

	private sealed class DelayedHandler : IHandler<DisposablePayload>
	{
		private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private Task _processing = Task.CompletedTask;

		public DisposablePayload Payload { get; private set; }
		public Task Processing => _processing;
		public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public ValueTask HandleAsync(DisposablePayload argument, CancellationToken cancellation = default)
		{
			_processing = this.HandleCoreAsync(argument);
			return new ValueTask(_processing);
		}

		public ValueTask HandleAsync(DisposablePayload argument, Parameters parameters, CancellationToken cancellation = default) => this.HandleAsync(argument, cancellation);
		public ValueTask HandleAsync(object argument, CancellationToken cancellation = default) => this.HandleAsync((DisposablePayload)argument, cancellation);
		public ValueTask HandleAsync(object argument, Parameters parameters, CancellationToken cancellation = default) => this.HandleAsync((DisposablePayload)argument, cancellation);

		public void Release() => _release.TrySetResult();

		private async Task HandleCoreAsync(DisposablePayload argument)
		{
			this.Payload = argument;
			this.Started.TrySetResult();
			await _release.Task;
		}
	}

	private sealed class TestDuplexPipe : IDuplexPipe
	{
		private readonly Pipe _input = new();
		private readonly Pipe _output = new();

		public PipeReader Input => _input.Reader;
		public PipeWriter Output => _output.Writer;

		public async Task WriteAndCompleteAsync(ReadOnlyMemory<byte> data)
		{
			await _input.Writer.WriteAsync(data);
			await _input.Writer.CompleteAsync();
		}
	}
}
