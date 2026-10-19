using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace Zongsoft.Net.Samples;

/// <summary>由应用显式启用的有界故障输出；网络回调只入队，不等待输出。</summary>
internal sealed class SampleDiagnostics : IAsyncDisposable
{
	private long _dropped;
	private readonly Task _completion;
	private readonly Channel<TcpDiagnosticEventArgs> _queue = Channel.CreateBounded<TcpDiagnosticEventArgs>(new BoundedChannelOptions(64)
	{
		SingleReader = true,
		AllowSynchronousContinuations = false,
		FullMode = BoundedChannelFullMode.Wait,
	});

	public SampleDiagnostics()
	{
		_completion = this.RunAsync();
		TcpDiagnostics.Error += this.OnError;
	}

	public async ValueTask DisposeAsync()
	{
		TcpDiagnostics.Error -= this.OnError;
		_queue.Writer.TryComplete();
		await _completion.ConfigureAwait(false);

		if(Interlocked.Read(ref _dropped) > 0)
			await Console.Error.WriteLineAsync($"DIAGNOSTICS dropped={_dropped}");
	}

	private void OnError(object sender, TcpDiagnosticEventArgs args)
	{
		if(!_queue.Writer.TryWrite(args))
			Interlocked.Increment(ref _dropped);
	}

	private async Task RunAsync()
	{
		await foreach(var args in _queue.Reader.ReadAllAsync().ConfigureAwait(false))
			await Console.Error.WriteLineAsync($"DIAGNOSTICS operation={args.Operation} error={args.Exception.GetType().Name} trace={args.ActivityContext.TraceId}");
	}
}
