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

using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

using Zongsoft.Services;
using Zongsoft.Diagnostics.Configuration;

namespace Zongsoft.Diagnostics.Telemetry;

partial class ExporterLauncher
{
	[Service<IExporterLauncher<MeterProviderBuilder>>(Members = nameof(Meters))]
	[Service<IExporterLauncher<TracerProviderBuilder>>(Members = nameof(Tracers))]
	[Service<IExporterLauncher<OpenTelemetryLoggerOptions>>(Members = nameof(Logs))]
	public static class Default
	{
		#region 静态字段
		public static readonly IExporterLauncher<MeterProviderBuilder> Meters = new MeterExporterLauncher();
		public static readonly IExporterLauncher<TracerProviderBuilder> Tracers = new TracerExporterLauncher();
		public static readonly IExporterLauncher<OpenTelemetryLoggerOptions> Logs = new LogExporterLauncher();
		#endregion

		#region 嵌套子类
		private sealed class LogExporterLauncher() : ExporterLauncherBase<OpenTelemetryLoggerOptions>(ExporterLauncherSettingsDriver.Default.Name)
		{
			public override void Launch(OpenTelemetryLoggerOptions options, string settings) => options.AddOtlpExporter((exporter, processor) => Configure(settings, exporter, processor));
			protected override bool OnMatch(string name) => string.IsNullOrEmpty(name) || string.Equals(name, "telemetry", StringComparison.OrdinalIgnoreCase) || base.OnMatch(name);
		}

		private sealed class MeterExporterLauncher() : ExporterLauncherBase<MeterProviderBuilder>(ExporterLauncherSettingsDriver.Default.Name)
		{
			public override void Launch(MeterProviderBuilder builder, string settings) => builder.AddOtlpExporter((options, reader) => Configure(settings, options, reader));
			protected override bool OnMatch(string name) => string.IsNullOrEmpty(name) || string.Equals(name, "telemetry", StringComparison.OrdinalIgnoreCase) || base.OnMatch(name);
		}

		private sealed class TracerExporterLauncher() : ExporterLauncherBase<TracerProviderBuilder>(ExporterLauncherSettingsDriver.Default.Name)
		{
			public override void Launch(TracerProviderBuilder builder, string settings) => builder.AddOtlpExporter(options => Configure(settings, options));
			protected override bool OnMatch(string name) => string.IsNullOrEmpty(name) || string.Equals(name, "telemetry", StringComparison.OrdinalIgnoreCase) || base.OnMatch(name);
		}
		#endregion
	}
}
