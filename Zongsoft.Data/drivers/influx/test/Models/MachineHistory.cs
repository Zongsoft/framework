using System;
using System.Collections.Generic;

namespace Zongsoft.Data.Influx.Tests.Models;

public struct MachineHistory
{
	public uint MachineId { get; set; }
	public ulong MetricId { get; set; }
	public double Value { get; set; }
	public string Text { get; set; }
	public int FailureCode { get; set; }
	public string FailureMessage { get; set; }
}
