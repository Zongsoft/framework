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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Grpc.Core;

using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;

using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Diagnostics.Telemetry;

[Service(Tags = "gRPC", Members = nameof(Metrics))]
[Service(Tags = "gRPC", Members = nameof(Traces))]
[Service(Tags = "gRPC", Members = nameof(Logs))]
public static partial class Listener
{
	public static readonly MetricsProcessor Metrics = new();
	public static readonly TracesProcessor Traces = new();
	public static readonly LogsProcessor Logs = new();

	private static Task HandleAsync(ICollection<IHandler> handlers, object argument, Parameters parameters, CancellationToken cancellation = default)
	{
		if(handlers == null || handlers.Count == 0)
			return Task.CompletedTask;

		return Parallel.ForEachAsync(handlers, cancellation, async (handler, cancellation) =>
		{
			if(handler == null)
				return;

			try
			{
				await handler.HandleAsync(argument, parameters, cancellation);
			}
			catch(Exception ex)
			{
				Logger.GetLogger(handler).Error(ex);
			}
		});
	}

	[System.Reflection.DefaultMember(nameof(Handlers))]
	public class MetricsProcessor : MetricsService.MetricsServiceBase
	{
		internal MetricsProcessor() { }
		public ICollection<IHandler> Handlers { get; } = new List<IHandler>();
		public override async Task<ExportMetricsServiceResponse> Export(ExportMetricsServiceRequest request, ServerCallContext context)
		{
			await HandleAsync(this.Handlers, null, Parameters.Parameter(request).Parameter(context), context.CancellationToken);
			return new ExportMetricsServiceResponse();
		}
	}

	[System.Reflection.DefaultMember(nameof(Handlers))]
	public class TracesProcessor : TraceService.TraceServiceBase
	{
		internal TracesProcessor() { }
		public ICollection<IHandler> Handlers { get; } = new List<IHandler>();
		public override async Task<ExportTraceServiceResponse> Export(ExportTraceServiceRequest request, ServerCallContext context)
		{
			await HandleAsync(this.Handlers, null, Parameters.Parameter(request).Parameter(context), context.CancellationToken);
			return new ExportTraceServiceResponse();
		}
	}

	[System.Reflection.DefaultMember(nameof(Handlers))]
	public class LogsProcessor : LogsService.LogsServiceBase
	{
		internal LogsProcessor() { }
		public ICollection<IHandler> Handlers { get; } = new List<IHandler>();
		public override async Task<ExportLogsServiceResponse> Export(ExportLogsServiceRequest request, ServerCallContext context)
		{
			await HandleAsync(this.Handlers, null, Parameters.Parameter(request).Parameter(context), context.CancellationToken);
			return new ExportLogsServiceResponse();
		}
	}
}
