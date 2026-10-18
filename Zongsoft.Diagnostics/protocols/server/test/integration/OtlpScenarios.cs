extern alias client;

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Grpc.Core;
using Grpc.Reflection.V1Alpha;
using Google.Protobuf;

using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;

using M = OpenTelemetry.Proto.Collector.Metrics.V1;
using T = OpenTelemetry.Proto.Collector.Trace.V1;
using L = OpenTelemetry.Proto.Collector.Logs.V1;
using CM = client::OpenTelemetry.Proto.Collector.Metrics.V1;
using CT = client::OpenTelemetry.Proto.Collector.Trace.V1;
using CL = client::OpenTelemetry.Proto.Collector.Logs.V1;

using Xunit;

namespace Zongsoft.Diagnostics.Protocols.Server.IntegrationTests;

internal static class OtlpScenarios
{
	public static Task RunAsync(string name, string directory) => name switch
	{
		nameof(OtlpIntegrationTest.OfficialExportersReachHandlersAndSample) => OfficialExportersReachHandlersAndSample(directory),
		nameof(OtlpIntegrationTest.GeneratedClientsReachAllSignals) => GeneratedClientsReachAllSignals(directory),
		nameof(OtlpIntegrationTest.RegionReflectionAndUnknownCallsStayIsolated) => RegionReflectionAndUnknownCallsStayIsolated(directory),
		nameof(OtlpIntegrationTest.CancellationReachesHandlerAndReleasesListener) => CancellationReachesHandlerAndReleasesListener(directory),
		_ => throw new ArgumentOutOfRangeException(nameof(name)),
	};

	private static async Task OfficialExportersReachHandlersAndSample(string directory)
	{
		await using var host = await OtlpTestHost.StartAsync(directory);
		var endpoint = new Uri(host.Address + "/opentelemetry");
		using var meter = new System.Diagnostics.Metrics.Meter("sdk-meter", "1.0");
		using var metrics = Sdk.CreateMeterProviderBuilder()
			.SetResourceBuilder(ResourceBuilder.CreateEmpty().AddService("sdk-fixture"))
			.AddMeter(meter.Name)
			.AddOtlpExporter((options, reader) =>
			{
				Configure(options, endpoint);
				reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = int.MaxValue;
			}).Build();
		meter.CreateCounter<long>("sdk.counter", "items").Add(37, new KeyValuePair<string, object>("fixture", "sdk"));
		Assert.True(metrics.ForceFlush(10000));
		var metric = await host.Metrics.ReadAsync();
		AssertMetric(metric, "sdk-meter", "sdk.counter", 37);
		var rawMetric = metric.Parameters.GetValue<M.ExportMetricsServiceRequest>();
		Assert.Equal("sdk-fixture", Assert.Single(rawMetric.ResourceMetrics).Resource.Attributes.Single(item => item.Key == "service.name").Value.StringValue);
		Assert.Equal("sdk", Assert.Single(rawMetric.ResourceMetrics[0].ScopeMetrics[0].Metrics[0].Sum.DataPoints[0].Attributes).Value.StringValue);

		using var source = new ActivitySource("sdk-traces");
		using var traces = Sdk.CreateTracerProviderBuilder()
			.SetResourceBuilder(ResourceBuilder.CreateEmpty().AddService("sdk-fixture"))
			.AddSource(source.Name)
			.AddOtlpExporter(options => Configure(options, endpoint)).Build();
		using(var activity = source.StartActivity("sdk-span", ActivityKind.Client))
		{
			Assert.NotNull(activity);
			activity.SetTag("fixture", "sdk-trace");
		}

		Assert.True(traces.ForceFlush(10000));
		var trace = await host.Traces.ReadAsync();
		Assert.Null(trace.Argument);
		var rawTrace = trace.Parameters.GetValue<T.ExportTraceServiceRequest>();
		var span = Assert.Single(Assert.Single(Assert.Single(rawTrace.ResourceSpans).ScopeSpans).Spans);
		Assert.Equal("sdk-span", span.Name);
		Assert.Equal("sdk-trace", Assert.Single(span.Attributes).Value.StringValue);
		Assert.Equal(16, span.TraceId.Length);
		Assert.Equal(8, span.SpanId.Length);

		using(var factory = LoggerFactory.Create(builder => builder.AddOpenTelemetry(options =>
		{
			options.IncludeFormattedMessage = true;
			options.SetResourceBuilder(ResourceBuilder.CreateEmpty().AddService("sdk-fixture"));
			options.AddOtlpExporter(exporter => Configure(exporter, endpoint));
		})))
		{
			factory.CreateLogger("sdk-logs").LogInformation("otlp fixture {Value}", 73);
		}

		var log = await host.Logs.ReadAsync();
		Assert.Null(log.Argument);
		var rawLog = log.Parameters.GetValue<L.ExportLogsServiceRequest>();
		var record = Assert.Single(Assert.Single(Assert.Single(rawLog.ResourceLogs).ScopeLogs).LogRecords);
		Assert.Equal("otlp fixture 73", record.Body.StringValue);
		Assert.Equal(73, record.Attributes.Single(item => item.Key == "Value").Value.IntValue);
		Assert.Equal(OpenTelemetry.Proto.Logs.V1.SeverityNumber.Info, record.SeverityNumber);

		static void Configure(OtlpExporterOptions options, Uri address)
		{
			options.Protocol = OtlpExportProtocol.Grpc;
			options.Endpoint = address;
			options.TimeoutMilliseconds = 10000;
		}
	}

	private static async Task GeneratedClientsReachAllSignals(string directory)
	{
		await using var host = await OtlpTestHost.StartAsync(directory);
		var channel = host.Channel();
		var metricRequest = MetricsRequest();

		using(var call = new CM.MetricsService.MetricsServiceClient(channel).ExportAsync(CM.ExportMetricsServiceRequest.Parser.ParseFrom(metricRequest.ToByteArray()), deadline: Deadline()))
			await call.ResponseAsync;

		var metric = await host.Metrics.ReadAsync();
		AssertMetric(metric, "direct-meter", "direct.counter", 42);
		Assert.Equal(metricRequest, metric.Parameters.GetValue<M.ExportMetricsServiceRequest>());

		var traceRequest = new T.ExportTraceServiceRequest();
		var traces = new OpenTelemetry.Proto.Trace.V1.ScopeSpans { Scope = new() { Name = "direct-traces" } };
		traces.Spans.Add(new OpenTelemetry.Proto.Trace.V1.Span
		{
			Name = "direct-span",
			TraceId = ByteString.CopyFrom(Enumerable.Range(1, 16).Select(value => (byte)value).ToArray()),
			SpanId = ByteString.CopyFrom(Enumerable.Range(1, 8).Select(value => (byte)value).ToArray()),
		});
		traceRequest.ResourceSpans.Add(new OpenTelemetry.Proto.Trace.V1.ResourceSpans { ScopeSpans = { traces } });

		using(var call = new CT.TraceService.TraceServiceClient(channel).ExportAsync(CT.ExportTraceServiceRequest.Parser.ParseFrom(traceRequest.ToByteArray()), deadline: Deadline()))
			await call.ResponseAsync;

		var trace = await host.Traces.ReadAsync();
		Assert.Null(trace.Argument);
		Assert.Equal(traceRequest, trace.Parameters.GetValue<T.ExportTraceServiceRequest>());

		var logRequest = new L.ExportLogsServiceRequest();
		var logs = new OpenTelemetry.Proto.Logs.V1.ScopeLogs { Scope = new() { Name = "direct-logs" } };
		logs.LogRecords.Add(new OpenTelemetry.Proto.Logs.V1.LogRecord
		{
			Body = new() { StringValue = "direct log payload" },
			SeverityNumber = OpenTelemetry.Proto.Logs.V1.SeverityNumber.Warn,
			TimeUnixNano = 1700000000000000000,
		});
		logRequest.ResourceLogs.Add(new OpenTelemetry.Proto.Logs.V1.ResourceLogs { ScopeLogs = { logs } });

		using(var call = new CL.LogsService.LogsServiceClient(channel).ExportAsync(CL.ExportLogsServiceRequest.Parser.ParseFrom(logRequest.ToByteArray()), deadline: Deadline()))
			await call.ResponseAsync;

		var log = await host.Logs.ReadAsync();
		Assert.Null(log.Argument);
		Assert.Equal(logRequest, log.Parameters.GetValue<L.ExportLogsServiceRequest>());
		Assert.Equal(1, host.Metrics.Count);
		Assert.Equal(1, host.Traces.Count);
		Assert.Equal(1, host.Logs.Count);
	}

	private static async Task RegionReflectionAndUnknownCallsStayIsolated(string directory)
	{
		await using var host = await OtlpTestHost.StartAsync(directory);
		var channel = host.Channel();

		using(var call = new ServerReflection.ServerReflectionClient(channel).ServerReflectionInfo(deadline: Deadline()))
		{
			await call.RequestStream.WriteAsync(new ServerReflectionRequest { ListServices = "" });
			await call.RequestStream.CompleteAsync();
			Assert.True(await call.ResponseStream.MoveNext(CancellationToken.None));
			Assert.Equal(new[]
			{
				"grpc.reflection.v1alpha.ServerReflection",
				"opentelemetry.proto.collector.logs.v1.LogsService",
				"opentelemetry.proto.collector.metrics.v1.MetricsService",
				"opentelemetry.proto.collector.trace.v1.TraceService",
			}, call.ResponseStream.Current.ListServicesResponse.Service.Select(service => service.Name).Order());
			Assert.False(await call.ResponseStream.MoveNext(CancellationToken.None));
		}

		foreach(var path in new[] { "/", "/incorrect" })
		{
			using var call = new CM.MetricsService.MetricsServiceClient(host.Channel(path)).ExportAsync(CM.ExportMetricsServiceRequest.Parser.ParseFrom(MetricsRequest().ToByteArray()), deadline: Deadline());
			var error = await Assert.ThrowsAsync<RpcException>(async () => await call.ResponseAsync);
			Assert.Equal(Grpc.Core.StatusCode.Unimplemented, error.StatusCode);
		}

		var method = new Method<CM.ExportMetricsServiceRequest, CM.ExportMetricsServiceResponse>(MethodType.Unary,
			"opentelemetry.proto.collector.metrics.v1.MetricsService", "Unknown",
			Marshallers.Create(request => request.ToByteArray(), CM.ExportMetricsServiceRequest.Parser.ParseFrom),
			Marshallers.Create(response => response.ToByteArray(), CM.ExportMetricsServiceResponse.Parser.ParseFrom));

		using(var call = channel.CreateCallInvoker().AsyncUnaryCall(method, null, new CallOptions(deadline: Deadline()), new()))
		{
			var error = await Assert.ThrowsAsync<RpcException>(async () => await call.ResponseAsync);
			Assert.Equal(Grpc.Core.StatusCode.Unimplemented, error.StatusCode);
		}

		Assert.Equal(0, host.Metrics.Count);
		Assert.Equal(0, host.Traces.Count);
		Assert.Equal(0, host.Logs.Count);
	}

	private static async Task CancellationReachesHandlerAndReleasesListener(string directory)
	{
		var host = await OtlpTestHost.StartAsync(directory);
		var port = host.Port;

		try
		{
			host.Traces.Block = true;
			using var cancellation = new CancellationTokenSource();
			using var call = new CT.TraceService.TraceServiceClient(host.Channel()).ExportAsync(new(), deadline: Deadline(), cancellationToken: cancellation.Token);
			await host.Traces.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
			cancellation.Cancel();
			var error = await Assert.ThrowsAsync<RpcException>(async () => await call.ResponseAsync);
			Assert.Equal(Grpc.Core.StatusCode.Cancelled, error.StatusCode);
			await host.Traces.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.Equal(1, host.Traces.Count);
		}
		finally
		{
			await host.DisposeAsync();
		}

		Assert.DoesNotContain(host.Metrics, Listener.Metrics.Handlers);
		Assert.DoesNotContain(host.Traces, Listener.Traces.Handlers);
		Assert.DoesNotContain(host.Logs, Listener.Logs.Handlers);
		Assert.DoesNotContain(Listener.Metrics.Handlers, handler => handler is Samples.MetricHandler);
		using var listener = new TcpListener(IPAddress.Loopback, port);
		listener.Start();
	}

	private static M.ExportMetricsServiceRequest MetricsRequest()
	{
		var request = new M.ExportMetricsServiceRequest();
		var scope = new OpenTelemetry.Proto.Metrics.V1.ScopeMetrics { Scope = new() { Name = "direct-meter" } };
		var sum = new OpenTelemetry.Proto.Metrics.V1.Sum { IsMonotonic = true };
		sum.DataPoints.Add(new OpenTelemetry.Proto.Metrics.V1.NumberDataPoint
		{
			AsInt = 42,
			StartTimeUnixNano = 1700000000000000000,
			TimeUnixNano = 1700000001000000000,
		});
		scope.Metrics.Add(new OpenTelemetry.Proto.Metrics.V1.Metric { Name = "direct.counter", Unit = "items", Sum = sum });
		request.ResourceMetrics.Add(new OpenTelemetry.Proto.Metrics.V1.ResourceMetrics { ScopeMetrics = { scope } });
		return request;
	}

	private static void AssertMetric(OtlpTestHost.Received received, string meterName, string metricName, long value)
	{
		var meter = Assert.Single(Assert.IsAssignableFrom<IEnumerable<Telemetry.Metrics.Meter>>(received.Argument));
		Assert.Equal(meterName, meter.Name);
		var metric = Assert.IsType<Telemetry.Metrics.Metric.Counter>(Assert.Single(meter.Metrics));
		Assert.Equal(metricName, metric.Name);
		Assert.Equal("items", metric.Unit);
		Assert.True(metric.IsMonotonic);
		Assert.Equal(value, Assert.IsType<long>(Assert.Single(metric.Points).Value));
		Assert.NotNull(received.Parameters.GetValue<ServerCallContext>());
	}

	private static DateTime Deadline() => DateTime.UtcNow.AddSeconds(10);
}
