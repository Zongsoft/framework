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

using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Diagnostics;

public class DiagnostorWorker(string name, Diagnostor.Configurator configurator) : WorkerBase(name)
{
	#region 成员字段
	private MeterProvider _meters;
	private TracerProvider _tracers;
	private readonly Diagnostor _diagnostor = new(name, configurator);
	#endregion

	#region 重写方法
	protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		var diagnostor = _diagnostor;
		var meters = ResolveExporters<MeterProviderBuilder>(diagnostor.Meters, "meters");
		var traces = ResolveExporters<TracerProviderBuilder>(diagnostor.Traces, "traces");

		if(meters.Length > 0)
		{
			var builder = Sdk.CreateMeterProviderBuilder().ConfigureResource(ConfigureResource);
			FillMeters(builder, diagnostor.Meters.Filters);

			foreach(var exporter in meters)
				exporter.Launcher.Launch(builder, exporter.Settings);

			_meters = builder.Build();
		}

		if(traces.Length > 0)
		{
			var builder = Sdk.CreateTracerProviderBuilder().ConfigureResource(ConfigureResource);
			FillTracers(builder, diagnostor.Traces.Filters);

			foreach(var exporter in traces)
				exporter.Launcher.Launch(builder, exporter.Settings);

			_tracers = builder.Build();
		}

		return Task.CompletedTask;
	}

	protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		this.ReleaseProviders();
		return Task.CompletedTask;
	}
	#endregion

	#region 私有方法
	private static void ConfigureResource(ResourceBuilder resource)
	{
		var name = ApplicationContext.Current?.Name;

		if(string.IsNullOrWhiteSpace(name))
			name = AppDomain.CurrentDomain.FriendlyName;

		resource.AddService(serviceName: name).AddEnvironmentVariableDetector();
	}

	private static (Telemetry.IExporterLauncher<TBuilder> Launcher, string Settings)[] ResolveExporters<TBuilder>(Diagnostor.Filtering filtering, string signal)
	{
		if(!Validate(filtering))
			return [];

		var exporters = new List<(Telemetry.IExporterLauncher<TBuilder>, string)>(filtering.Exporters.Count);

		foreach(var exporter in filtering.Exporters)
		{
			var launcher = ApplicationContext.Current?.Services.Find<Telemetry.IExporterLauncher<TBuilder>>(exporter.Driver);

			if(launcher == null)
			{
				var exception = new InvalidOperationException(string.Format(Properties.Resources.Exporter_Missing_Message, exporter.Driver, signal));
				exception.Data["ExporterDriver"] = exporter.Driver;
				exception.Data["Signal"] = signal;
				throw exception;
			}

			exporters.Add((launcher, exporter.Settings));
		}

		return [.. exporters];
	}

	private void ReleaseProviders()
	{
		try
		{
			if(_meters != null)
			{
				try { _meters.Shutdown(); }
				finally { _meters.Dispose(); _meters = null; }
			}
		}
		finally
		{
			if(_tracers != null)
			{
				try { _tracers.Shutdown(); }
				finally { _tracers.Dispose(); _tracers = null; }
			}
		}
	}

	private static readonly string[] DEFAULT_METERS =
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

	private static readonly string[] DEFAULT_TRACERS =
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
				for(int i = 0; i < DEFAULT_METERS.Length; i++)
					hashset.Add(DEFAULT_METERS[i]);

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
				for(int i = 0; i < DEFAULT_TRACERS.Length; i++)
					hashset.Add(DEFAULT_TRACERS[i]);

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
