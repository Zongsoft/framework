using System;
using System.Linq;
using System.Diagnostics.Metrics;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Diagnostics.Telemetry.Tests;

public class MeterTest
{
	[Fact]
	public void TestCreateCounter()
	{
		var meter = new Meter("TestMeter");

		var counter = meter.CreateCounter("Int32Counter", typeof(int), "units", "description");
		Assert.NotNull(counter);
		Assert.NotNull(counter.Meter);
		Assert.Same(meter, counter.Meter);
		Assert.IsType<Counter<int>>(counter);
		Assert.Equal("Int32Counter", counter.Name);
		Assert.Equal("units", counter.Unit);
		Assert.Equal("description", counter.Description);

		#if NET8_0_OR_GREATER
		counter = meter.CreateCounter("DoubleCounter", typeof(double), "units", "description", [new KeyValuePair<string, object>("Tag1", "Value1")]);
		Assert.NotNull(counter);
		Assert.NotNull(counter.Meter);
		Assert.Same(meter, counter.Meter);
		Assert.IsType<Counter<double>>(counter);
		Assert.Equal("DoubleCounter", counter.Name);
		Assert.Equal("units", counter.Unit);
		Assert.Equal("description", counter.Description);
		Assert.Single(counter.Tags);
		Assert.Equal("Tag1", counter.Tags.FirstOrDefault().Key);
		Assert.Equal("Value1", counter.Tags.FirstOrDefault().Value);
		#endif
	}

	[Fact]
	public void TestCreateHistogram()
	{
		var meter = new Meter("TestMeter");

		var histogram = meter.CreateHistogram("Int32Histogram", typeof(int), "units", "description");
		Assert.NotNull(histogram);
		Assert.NotNull(histogram.Meter);
		Assert.Same(meter, histogram.Meter);
		Assert.IsType<Histogram<int>>(histogram);
		Assert.Equal("Int32Histogram", histogram.Name);
		Assert.Equal("units", histogram.Unit);
		Assert.Equal("description", histogram.Description);

		#if NET8_0_OR_GREATER
		histogram = meter.CreateHistogram("DoubleHistogram", typeof(double), "units", "description", [new KeyValuePair<string, object>("Tag1", "Value1")]);
		Assert.NotNull(histogram);
		Assert.NotNull(histogram.Meter);
		Assert.Same(meter, histogram.Meter);
		Assert.IsType<Histogram<double>>(histogram);
		Assert.Equal("DoubleHistogram", histogram.Name);
		Assert.Equal("units", histogram.Unit);
		Assert.Equal("description", histogram.Description);
		Assert.Single(histogram.Tags);
		Assert.Equal("Tag1", histogram.Tags.FirstOrDefault().Key);
		Assert.Equal("Value1", histogram.Tags.FirstOrDefault().Value);
		#endif
	}

	#if NET8_0_OR_GREATER
	[Fact]
	public void TestCreateUpDownCounter()
	{
		var meter = new Meter("TestMeter");

		var counter = meter.CreateUpDownCounter("Int32Counter", typeof(int), "units", "description");
		Assert.NotNull(counter);
		Assert.NotNull(counter.Meter);
		Assert.Same(meter, counter.Meter);
		Assert.IsType<UpDownCounter<int>>(counter);
		Assert.Equal("Int32Counter", counter.Name);
		Assert.Equal("units", counter.Unit);
		Assert.Equal("description", counter.Description);

		counter = meter.CreateUpDownCounter("DoubleCounter", typeof(double), "units", "description", [new KeyValuePair<string, object>("Tag1", "Value1")]);
		Assert.NotNull(counter);
		Assert.NotNull(counter.Meter);
		Assert.Same(meter, counter.Meter);
		Assert.IsType<UpDownCounter<double>>(counter);
		Assert.Equal("DoubleCounter", counter.Name);
		Assert.Equal("units", counter.Unit);
		Assert.Equal("description", counter.Description);
		Assert.Single(counter.Tags);
		Assert.Equal("Tag1", counter.Tags.FirstOrDefault().Key);
		Assert.Equal("Value1", counter.Tags.FirstOrDefault().Value);
	}
	#endif

	#if NET9_0_OR_GREATER
	[Fact]
	public void TestCreateGauge()
	{
		var meter = new Meter("TestMeter");

		var gauge = meter.CreateGauge("Int32Gauge", typeof(int), "units", "description");
		Assert.NotNull(gauge);
		Assert.NotNull(gauge.Meter);
		Assert.Same(meter, gauge.Meter);
		Assert.IsType<Gauge<int>>(gauge);
		Assert.Equal("Int32Gauge", gauge.Name);
		Assert.Equal("units", gauge.Unit);
		Assert.Equal("description", gauge.Description);

		gauge = meter.CreateGauge("DoubleGauge", typeof(double), "units", "description", [new KeyValuePair<string, object>("Tag1", "Value1")]);
		Assert.NotNull(gauge);
		Assert.NotNull(gauge.Meter);
		Assert.Same(meter, gauge.Meter);
		Assert.IsType<Gauge<double>>(gauge);
		Assert.Equal("DoubleGauge", gauge.Name);
		Assert.Equal("units", gauge.Unit);
		Assert.Equal("description", gauge.Description);
		Assert.Single(gauge.Tags);
		Assert.Equal("Tag1", gauge.Tags.FirstOrDefault().Key);
		Assert.Equal("Value1", gauge.Tags.FirstOrDefault().Value);
	}
	#endif
}
