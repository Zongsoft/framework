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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Extensions;

using Zongsoft.Services;

namespace Zongsoft.Diagnostics;
                            
[Service<IApplicationInitializer>(Members = nameof(Instance))]
public sealed partial class Diagnostor : IApplicationInitializer
{
	public static readonly Diagnostor Instance = new();

	private Diagnostor()
	{
		this.Meters = new HashSet<string>()
		{
			"System.Runtime",
			"System.Net.Http",
			"System.Net.NameResolution",
			"Microsoft.Extensions.Diagnostics.HealthChecks",
			"Microsoft.Extensions.Diagnostics.ResourceMonitoring",
			"Microsoft.AspNetCore.Hosting",
			"Microsoft.AspNetCore.Routing",
			"Microsoft.AspNetCore.Diagnostics",
			"Microsoft.AspNetCore.RateLimiting",
			"Microsoft.AspNetCore.HeaderParsing",
			"Microsoft.AspNetCore.Server.Kestrel",
			"Microsoft.AspNetCore.Http.Connections",
		};
		this.Tracers = new HashSet<string>();
		this.Exporters = new HashSet<string>();
	}

	public ICollection<string> Meters { get; }
	public ICollection<string> Tracers { get; }
	public ICollection<string> Exporters { get; }

	public void Initialize(IApplicationContext context)
	{
		//var builder = context.Services.Resolve<IDeferredMeterProviderBuilder>();
		//var telemetry = context.Services.Resolve<OpenTelemetryBuilder>();
		//var mpb = context.Services.Resolve<MeterProviderBuilder>();
		//var tpb = context.Services.Resolve<TracerProviderBuilder>();
		//var mp = context.Services.Resolve<MeterProvider>();
		//var tp = context.Services.Resolve<TracerProvider>();
		//var lp = context.Services.Resolve<LoggerProvider>();

		var meters = Sdk.CreateMeterProviderBuilder();
		var tracers = Sdk.CreateTracerProviderBuilder();

		this.Initialize(meters);
		this.Initialize(tracers);

		var a = meters.Build();
		var b = tracers.Build();

		a.Shutdown();
		b.Shutdown();
		a.Dispose();
		b.Dispose();
	}

	private void Initialize(MeterProviderBuilder meters)
	{
		if(this.Meters.Count > 0)
			meters.AddMeter(this.Meters.ToArray());

		//meters.AddRuntimeInstrumentation();
		//meters.AddHttpClientInstrumentation();
		//meters.AddAspNetCoreInstrumentation();

		meters.AddOtlpExporter();
		meters.AddPrometheusExporter();
		meters.AddPrometheusHttpListener(options => options.UriPrefixes =
		[
			"http://127.0.0.1:9464",
			"http://localhost:9464",
		]);
	}

	private void Initialize(TracerProviderBuilder tracers)
	{
		if(this.Tracers.Count > 0)
			tracers.AddSource(this.Tracers.ToArray());

		//tracers.AddHttpClientInstrumentation();
		//tracers.AddAspNetCoreInstrumentation();

		tracers.AddOtlpExporter();
		tracers.AddZipkinExporter();
	}

	private void Initialize(LoggerProviderBuilder loggers)
	{
	}
}

//partial class Diagnostor : IServiceRegistration
//{
//	void IServiceRegistration.Register(IServiceCollection services, IConfiguration configuration)
//	{
//		services.AddTransient<IApplicationInitializer>(_ => Instance);

//		services.AddOpenTelemetry()
//			.WithMetrics(this.Initialize)
//			.WithTracing(this.Initialize)
//			.WithLogging(this.Initialize);
//	}
//}