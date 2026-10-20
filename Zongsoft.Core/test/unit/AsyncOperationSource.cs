using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Zongsoft.Tests;

internal sealed class AsyncOperationSource : IValueTaskSource
{
	private ManualResetValueTaskSourceCore<bool> _source = new() { RunContinuationsAsynchronously = true };
	private int _consumptionCount;

	public int ConsumptionCount => Volatile.Read(ref _consumptionCount);
	public ValueTask Operation => new(this, _source.Version);
	public void Complete() => _source.SetResult(true);
	public void Fail(Exception exception) => _source.SetException(exception);
	public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);
	public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) =>
		_source.OnCompleted(continuation, state, token, flags);

	public void GetResult(short token)
	{
		Interlocked.Increment(ref _consumptionCount);
		_source.GetResult(token);
	}
}
