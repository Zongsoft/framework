/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Diagnostics library.
 *
 * The Zongsoft.Diagnostics is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Diagnostics is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Diagnostics library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Grpc.Core;

using Google.Protobuf;
using Google.Protobuf.Collections;

using OpenTelemetry.Proto.Collector.Metrics.V1;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Diagnostics.Telemetry;

[Service(Tags = "gRPC", Members = nameof(Metrics))]
partial class Listener
{
	#region 单例字段
	public static readonly MetricsProcessor Metrics = new();
	#endregion

	[System.Reflection.DefaultMember(nameof(Handlers))]
	public class MetricsProcessor : MetricsService.MetricsServiceBase
	{
		#region 私有构造
		internal MetricsProcessor() { }
		#endregion

		#region 公共属性
		public ICollection<IHandler> Handlers { get; } = new List<IHandler>();
		#endregion

		#region 重写方法
		public override async Task<ExportMetricsServiceResponse> Export(ExportMetricsServiceRequest request, ServerCallContext context)
		{
			if(this.Handlers.Count > 0 && !context.CancellationToken.IsCancellationRequested)
			{
				List<Metrics.Meter> meters = null;

				foreach(var resource in request.ResourceMetrics)
				{
					meters = new(resource.ScopeMetrics.Count);

					foreach(var bundle in resource.ScopeMetrics)
					{
						var meter = new Metrics.Meter(bundle.Scope.Name, bundle.Scope.Version)
						{
							Tags = GetTags(bundle.Scope.Attributes)
						};

						foreach(var metric in bundle.Metrics)
						{
							var entry = GetMetric(metric);

							if(entry != null)
								meter.Metrics.Add(GetMetric(metric));
						}

						meters.Add(meter);
					}
				}

				if(meters != null && meters.Count > 0 && !context.CancellationToken.IsCancellationRequested)
					await HandleAsync(this.Handlers, meters, Parameters.Parameter(request).Parameter(context), context.CancellationToken);
			}

			return new ExportMetricsServiceResponse();
		}
		#endregion

		#region 私有方法
		static Metrics.Metric GetMetric(OpenTelemetry.Proto.Metrics.V1.Metric metric)
		{
			switch(metric.DataCase)
			{
				case OpenTelemetry.Proto.Metrics.V1.Metric.DataOneofCase.Sum:
					var counter = new Metrics.Metric.Counter(metric.Name, metric.Unit, metric.Sum.IsMonotonic, metric.Description)
					{
						Tags = GetTags(metric.Metadata),
					};

					var counterPoints = new List<Metrics.Metric.Counter.Point>(metric.Sum.DataPoints.Count);

					foreach(var entry in metric.Sum.DataPoints)
					{
						object value = null;

						if(entry.HasAsDouble)
							value = entry.AsDouble;
						else if(entry.HasAsInt)
							value = entry.AsInt;

						counterPoints.Add(new Metrics.Metric.Counter.Point(
							value,
							GetTimestamp(entry.StartTimeUnixNano),
							GetTimestamp(entry.TimeUnixNano),
							GetTags(entry.Attributes)));
					}

					counter.Points = [.. counterPoints];
					return counter;
				case OpenTelemetry.Proto.Metrics.V1.Metric.DataOneofCase.Gauge:
					var gauge = new Metrics.Metric.Counter(metric.Name, metric.Unit, true, metric.Description)
					{
						Tags = GetTags(metric.Metadata),
					};

					var gaugePoints = new List<Metrics.Metric.Counter.Point>(metric.Gauge.DataPoints.Count);

					foreach(var entry in metric.Gauge.DataPoints)
					{
						object value = null;

						if(entry.HasAsDouble)
							value = entry.AsDouble;
						else if(entry.HasAsInt)
							value = entry.AsInt;

						gaugePoints.Add(new Metrics.Metric.Counter.Point(
							value,
							GetTimestamp(entry.StartTimeUnixNano),
							GetTimestamp(entry.TimeUnixNano),
							GetTags(entry.Attributes)));
					}

					gauge.Points = [.. gaugePoints];
					return gauge;
				case OpenTelemetry.Proto.Metrics.V1.Metric.DataOneofCase.Summary:
					var summary = new Metrics.Metric.Summary(metric.Name, metric.Unit, metric.Description)
					{
						Tags = GetTags(metric.Metadata),
					};

					var summaryPoints = new List<Metrics.Metric.Summary.Point>(metric.Summary.DataPoints.Count);

					foreach(var entry in metric.Summary.DataPoints)
					{
						summaryPoints.Add(new Metrics.Metric.Summary.Point(
							entry.Sum,
							entry.Count,
							entry.Flags,
							GetTimestamp(entry.StartTimeUnixNano),
							GetTimestamp(entry.TimeUnixNano),
							[.. entry.QuantileValues.Select(q => new Metrics.Metric.Summary.Point.QuantileValue(q.Value, q.Quantile))],
							GetTags(entry.Attributes)));
					}

					summary.Points = [.. summaryPoints];
					return summary;
				case OpenTelemetry.Proto.Metrics.V1.Metric.DataOneofCase.Histogram:
					var histogram = new Metrics.Metric.Histogram(metric.Name, metric.Unit, metric.Description)
					{
						Tags = GetTags(metric.Metadata),
					};

					var histogramPoints = new List<Metrics.Metric.Histogram.Point>(metric.Histogram.DataPoints.Count);

					foreach(var entry in metric.Histogram.DataPoints)
					{
						histogramPoints.Add(new Telemetry.Metrics.Metric.Histogram.Point(
							entry.Count,
							entry.Sum,
							entry.Min,
							entry.Max,
							GetTimestamp(entry.StartTimeUnixNano),
							GetTimestamp(entry.TimeUnixNano),
							GetTags(entry.Attributes)));
					}

					histogram.Points = [.. histogramPoints];
					return histogram;
				case OpenTelemetry.Proto.Metrics.V1.Metric.DataOneofCase.ExponentialHistogram:
					var exponentialHistogram = new Metrics.Metric.Histogram(metric.Name, metric.Unit, metric.Description)
					{
						Tags = GetTags(metric.Metadata),
					};

					var exponentialHistogramPoints = new List<Metrics.Metric.Histogram.Point>(metric.ExponentialHistogram.DataPoints.Count);

					foreach(var entry in metric.ExponentialHistogram.DataPoints)
					{
						exponentialHistogramPoints.Add(new Telemetry.Metrics.Metric.Histogram.Point(
							entry.Count,
							entry.Sum,
							entry.Min,
							entry.Max,
							GetTimestamp(entry.StartTimeUnixNano),
							GetTimestamp(entry.TimeUnixNano),
							GetTags(entry.Attributes)));
					}

					exponentialHistogram.Points = [.. exponentialHistogramPoints];
					return exponentialHistogram;
			}

			return null;
		}

		static IEnumerable<KeyValuePair<string, object>> GetTags(RepeatedField<OpenTelemetry.Proto.Common.V1.KeyValue> attributes)
		{
			if(attributes == null)
				yield break;

			foreach(var attribute in attributes)
				yield return new KeyValuePair<string, object>(attribute.Key, GetValue(attribute.Value));
		}

		static object GetValue(OpenTelemetry.Proto.Common.V1.AnyValue value)
		{
			if(value == null)
				return null;

			return value.ValueCase switch
			{
				OpenTelemetry.Proto.Common.V1.AnyValue.ValueOneofCase.None => null,
				OpenTelemetry.Proto.Common.V1.AnyValue.ValueOneofCase.BoolValue => value.BoolValue,
				OpenTelemetry.Proto.Common.V1.AnyValue.ValueOneofCase.IntValue => value.IntValue,
				OpenTelemetry.Proto.Common.V1.AnyValue.ValueOneofCase.DoubleValue => value.DoubleValue,
				OpenTelemetry.Proto.Common.V1.AnyValue.ValueOneofCase.StringValue => value.StringValue,
				OpenTelemetry.Proto.Common.V1.AnyValue.ValueOneofCase.BytesValue => value.BytesValue.ToByteArray(),
				OpenTelemetry.Proto.Common.V1.AnyValue.ValueOneofCase.ArrayValue => GetArray(value.ArrayValue.Values),
				OpenTelemetry.Proto.Common.V1.AnyValue.ValueOneofCase.KvlistValue => GetDictionary(value.KvlistValue.Values),
				_ => value,
			};

			static Array GetArray(RepeatedField<OpenTelemetry.Proto.Common.V1.AnyValue> values)
			{
				if(values == null || values.Count == 0)
					return null;

				var list = new List<object>(values.Count);
				list.AddRange(values.Select(GetValue));
				return list.ToArray();
			}

			static Dictionary<string, object> GetDictionary(RepeatedField<OpenTelemetry.Proto.Common.V1.KeyValue> entries)
			{
				if(entries == null || entries.Count == 0)
					return null;

				var dictionary = new Dictionary<string, object>(entries.Count);

				foreach(var entry in entries)
					dictionary[entry.Key] = GetValue(entry.Value);

				return dictionary;
			}
		}
		#endregion
	}
}
