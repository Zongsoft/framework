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
 * This file is part of Zongsoft.Diagnostics.Protocols.Server library.
 *
 * The Zongsoft.Diagnostics.Protocols.Server is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Diagnostics.Protocols.Server is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Diagnostics.Protocols.Server library. If not, see <http://www.gnu.org/licenses/>.
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
