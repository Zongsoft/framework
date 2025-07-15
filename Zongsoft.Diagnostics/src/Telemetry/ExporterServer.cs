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

public static partial class ExporterServer
{
	private static Task HandleAsync(ICollection<IHandler> handlers, object argument, Parameters parameters, CancellationToken cancellation = default)
	{
		if(handlers == null || handlers.Count == 0)
			return Task.CompletedTask;

		return Task.Run(() =>
		{
			foreach(var handler in handlers)
			{
				try
				{
					handler?.HandleAsync(argument, parameters, cancellation).AsTask();
				}
				catch(Exception ex)
				{
					Logger.GetLogger(handler).Error(ex);
				}
			}
		}, cancellation);
	}

	[System.Reflection.DefaultMember(nameof(Handlers))]
	public class Metrics : MetricsService.MetricsServiceBase
	{
		public ICollection<IHandler> Handlers { get; }
		public override async Task<ExportMetricsServiceResponse> Export(ExportMetricsServiceRequest request, ServerCallContext context)
		{
			await HandleAsync(this.Handlers, null, Parameters.Parameter(request).Parameter(context));
			return new ExportMetricsServiceResponse();
		}
	}

	[System.Reflection.DefaultMember(nameof(Handlers))]
	public class Traces : TraceService.TraceServiceBase
	{
		public ICollection<IHandler> Handlers { get; }
		public override async Task<ExportTraceServiceResponse> Export(ExportTraceServiceRequest request, ServerCallContext context)
		{
			await HandleAsync(this.Handlers, null, Parameters.Parameter(request).Parameter(context));
			return new ExportTraceServiceResponse();
		}
	}

	[System.Reflection.DefaultMember(nameof(Handlers))]
	public class Logs : LogsService.LogsServiceBase
	{
		public ICollection<IHandler> Handlers { get; }
		public override async Task<ExportLogsServiceResponse> Export(ExportLogsServiceRequest request, ServerCallContext context)
		{
			await HandleAsync(this.Handlers, null, Parameters.Parameter(request).Parameter(context));
			return new ExportLogsServiceResponse();
		}
	}
}
