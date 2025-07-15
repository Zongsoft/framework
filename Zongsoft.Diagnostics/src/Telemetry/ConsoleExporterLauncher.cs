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

using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Diagnostics.Telemetry;

[Service<IExporterLauncher<MeterProviderBuilder>>(Members = nameof(Meters))]
[Service<IExporterLauncher<TracerProviderBuilder>>(Members = nameof(Tracers))]
public class ConsoleExporterLauncher
{
	public static readonly IExporterLauncher<MeterProviderBuilder> Meters = new MeterExporterLauncher();
	public static readonly IExporterLauncher<TracerProviderBuilder> Tracers = new TracerExporterLauncher();

	private sealed class MeterExporterLauncher() : ExporterLauncherBase<MeterProviderBuilder>("Console")
	{
		public override void Launch(MeterProviderBuilder builder, string settings) => builder.AddConsoleExporter((options, reader) =>
		{
			if(string.IsNullOrEmpty(settings))
				return;

			var connectionSettings = new ConnectionSettings(this.Name, settings);
			options.Targets = connectionSettings.GetValue("target", ConsoleExporterOutputTargets.Console);

			var timeout = (int)connectionSettings.GetValue("timeout", TimeSpan.FromSeconds(30)).TotalMilliseconds;
			var interval = (int)connectionSettings.GetValue("Interval", TimeSpan.FromSeconds(10)).TotalMilliseconds;

			reader.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds = timeout;
			reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = interval;
		});
	}

	private sealed class TracerExporterLauncher() : ExporterLauncherBase<TracerProviderBuilder>("Console")
	{
		public override void Launch(TracerProviderBuilder builder, string settings) => builder.AddConsoleExporter(options =>
		{
			if(string.IsNullOrEmpty(settings))
				return;

			var connectionSettings = new ConnectionSettings(this.Name, settings);
			options.Targets = connectionSettings.GetValue("target", ConsoleExporterOutputTargets.Console);
		});
	}
}
