using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Upgrading.Tests;

public class ReleaseReaderTest
{
	[Fact]
	public async Task ReaderRemainsAliveUntilAsynchronousReadCompletes()
	{
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		using var stream = new DelayedStream();
		var method = typeof(Release).GetMethod("ReadAsync", BindingFlags.Static | BindingFlags.NonPublic, [typeof(Stream), typeof(CancellationToken)]);
		Assert.NotNull(method);

		try
		{
			var reading = (ValueTask<Release>)method.Invoke(null, [stream, cancellation.Token]);
			await stream.Started.Task.WaitAsync(cancellation.Token);
			Assert.False(reading.IsCompleted);

			stream.Resume.TrySetResult();
			var release = await reading.AsTask().WaitAsync(cancellation.Token);
			Assert.Equal("ReaderTest", release.Name);
			Assert.Equal(new Version(1, 2, 3), release.Version);
		}
		finally { stream.Resume.TrySetResult(); }
	}

	private sealed class DelayedStream() : MemoryStream("<release name=\"ReaderTest\" version=\"1.2.3\" />"u8.ToArray())
	{
		public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Resume { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellation) =>
			this.ReadAsync(buffer.AsMemory(offset, count), cancellation).AsTask();

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default)
		{
			this.Started.TrySetResult();
			await this.Resume.Task.WaitAsync(cancellation);
			return base.Read(buffer.Span);
		}
	}
}
