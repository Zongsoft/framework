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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Grpc.Core;

using OpenTelemetry.Proto.Collector.Trace.V1;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Diagnostics.Protocols.Server;

[Service(Tags = "gRPC", Members = nameof(Traces))]
partial class Listener
{
	#region 单例字段
	public static readonly TracesProcessor Traces = new();
	#endregion

	[Handler("/opentelemetry")]
	[System.Reflection.DefaultMember(nameof(Handlers))]
	public class TracesProcessor : TraceService.TraceServiceBase
	{
		internal TracesProcessor() { }
		public ICollection<IHandler> Handlers { get; } = new List<IHandler>();
		public override async Task<ExportTraceServiceResponse> Export(ExportTraceServiceRequest request, ServerCallContext context)
		{
			if(this.Handlers.Count > 0 && !context.CancellationToken.IsCancellationRequested)
				await HandleAsync(this.Handlers, null, Parameters.Parameter(request).Parameter(context), context.CancellationToken);

			return new ExportTraceServiceResponse();
		}
	}
}
