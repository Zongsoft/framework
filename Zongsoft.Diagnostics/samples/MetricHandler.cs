using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Terminals;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Diagnostics.Samples;

public class MetricHandler : HandlerBase<IEnumerable<Zongsoft.Diagnostics.Telemetry.Metrics.Meter>>
{
	protected override ValueTask OnHandleAsync(IEnumerable<Telemetry.Metrics.Meter> meters, Parameters parameters, CancellationToken cancellation)
	{
		foreach(var meter in meters)
			Terminal.WriteLine(CommandOutletDumper.Dump(meter));

		return ValueTask.CompletedTask;
	}
}
