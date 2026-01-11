using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Diagnostics;

namespace Zongsoft.Externals.Hangfire.Samples;

public class MyHandler : HandlerBase<object>
{
	private long _count = 0;

	protected override ValueTask OnHandleAsync(object argument, Parameters parameters, CancellationToken cancellation) =>
		Logging.GetLogging(this).DebugAsync(
			"MyHandler handles the scheduling of the Hangfire.",
			new
			{
				Count = Interlocked.Increment(ref _count),
				Argument = argument,
				Parameters = parameters,
			},
			cancellation);
}
