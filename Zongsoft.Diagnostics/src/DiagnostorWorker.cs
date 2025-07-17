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

using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Diagnostics;

public class DiagnostorWorker(string name, Diagnostor.Configurator configurator) : WorkerBase(name)
{
	#region 成员字段
	private MeterProvider _meters;
	private TracerProvider _tracers;
	private Diagnostor _diagnostor = new(name, configurator);
	#endregion

	#region 重写方法
	protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		var diagnostor = _diagnostor ?? throw new ObjectDisposedException(nameof(DiagnostorWorker));

		var meters = diagnostor.Meters;

		if(meters != null && Validate(meters))
		{
			var builder = Sdk.CreateMeterProviderBuilder();

			//填充计量器（支持星号，默认计量器）
			FillMeters(builder, meters.Filters);

			if(meters.Exporters != null)
			{
				foreach(var exporter in meters.Exporters)
				{
					var launcher = ApplicationContext.Current?.Services.Resolve<Telemetry.IExporterLauncher<MeterProviderBuilder>>(exporter.Key);
					if(launcher != null)
						launcher.Launch(builder, exporter.Value);
				}
			}

			_meters = builder.Build();
		}

		var traces = diagnostor.Traces;

		if(traces != null && Validate(traces))
		{
			var builder = Sdk.CreateTracerProviderBuilder();

			//填充追踪器（支持星号，默认追踪器）
			FillTracers(builder, traces.Filters);

			if(traces.Exporters != null)
			{
				foreach(var exporter in traces.Exporters)
				{
					var launcher = ApplicationContext.Current?.Services.Resolve<Telemetry.IExporterLauncher<TracerProviderBuilder>>(exporter.Key);
					if(launcher != null)
						launcher.Launch(builder, exporter.Value);
				}
			}

			_tracers = builder.Build();
		}

		return Task.CompletedTask;
	}

	protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		var meters = Interlocked.Exchange(ref _meters, null);
		if(meters != null)
		{
			meters.Shutdown();
			meters.Dispose();
		}

		var tracers = Interlocked.Exchange(ref _tracers, null);
		if(tracers != null)
		{
			tracers.Shutdown();
			tracers.Dispose();
		}

		return Task.CompletedTask;
	}

	protected override void Dispose(bool disposing) => _diagnostor = null;
	#endregion

	#region 私有方法
	private static readonly string[] DefaultMeters =
	[
		"System.Runtime",
		"System.Net.Http",
		"System.Net.NameResolution",
		"Microsoft.Extensions.Diagnostics.HealthChecks",
		"Microsoft.AspNetCore.Hosting",
		"Microsoft.AspNetCore.Routing",
		"Microsoft.AspNetCore.Diagnostics",
		"Microsoft.AspNetCore.RateLimiting",
		"Microsoft.AspNetCore.HeaderParsing",
		"Microsoft.AspNetCore.Server.Kestrel",
		"Microsoft.AspNetCore.Http.Connections",
	];

	private static readonly string[] DefaultTracers =
	[
		"System.Net",
		"System.Net.Http",
		"System.Net.Http.Connections",
		"System.Net.NameResolution",
		"System.Net.Sockets",
		"System.Net.Security",
		"Experimental.System.Net.Http.Connections",
		"Experimental.System.Net.NameResolution",
		"Experimental.System.Net.Sockets",
		"Experimental.System.Net.Security",
	];

	private static void FillMeters(MeterProviderBuilder builder, ICollection<string> meters)
	{
		if(builder == null || meters == null || meters.Count == 0)
			return;

		var hashset = new HashSet<string>();

		foreach(var meter in meters)
		{
			if(string.IsNullOrEmpty(meter))
				continue;

			if(meter == "*")
			{
				for(int i = 0; i < DefaultMeters.Length; i++)
					hashset.Add(DefaultMeters[i]);

				if(ApplicationContext.Current != null)
				{
					foreach(var module in ApplicationContext.Current.Modules)
						hashset.Add(string.IsNullOrEmpty(module.Name) ? "Common" : module.Name);
				}
			}
			else
			{
				hashset.Add(meter);
			}
		}

		builder.AddMeter([.. hashset]);
	}

	private static void FillTracers(TracerProviderBuilder builder, ICollection<string> tracers)
	{
		if(builder == null || tracers == null || tracers.Count == 0)
			return;

		var hashset = new HashSet<string>();

		foreach(var tracer in tracers)
		{
			if(string.IsNullOrEmpty(tracer))
				continue;

			if(tracer == "*")
			{
				for(int i = 0; i < DefaultTracers.Length; i++)
					hashset.Add(DefaultTracers[i]);

				if(ApplicationContext.Current != null)
				{
					foreach(var module in ApplicationContext.Current.Modules)
						hashset.Add(string.IsNullOrEmpty(module.Name) ? "Common" : module.Name);
				}
			}
			else
			{
				hashset.Add(tracer);
			}
		}

		builder.AddSource([.. hashset]);
	}

	private static bool Validate(Diagnostor.Filtering filtering) => filtering != null &&
		filtering.Filters != null && filtering.Filters.Count > 0 &&
		filtering.Exporters != null && filtering.Exporters.Count > 0;
	#endregion
}
