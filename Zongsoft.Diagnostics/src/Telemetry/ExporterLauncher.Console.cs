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
	public static class Console
	{
		#region 静态字段
		public static readonly IExporterLauncher<MeterProviderBuilder> Meters = new MeterExporterLauncher();
		public static readonly IExporterLauncher<TracerProviderBuilder> Tracers = new TracerExporterLauncher();
		public static readonly IExporterLauncher<OpenTelemetryLoggerOptions> Logs = new LogExporterLauncher();
		#endregion

		#region 嵌套子类
		private sealed class LogExporterLauncher() : ExporterLauncherBase<OpenTelemetryLoggerOptions>(ExporterLauncherSettingsDriver.Console.Name)
		{
			public override void Launch(OpenTelemetryLoggerOptions options, string settings) => options.AddConsoleExporter(exporter => Configure(settings, exporter));
			protected override bool OnMatch(string name) => string.Equals(name, "Console", StringComparison.OrdinalIgnoreCase) || base.OnMatch(name);
		}

		private sealed class MeterExporterLauncher() : ExporterLauncherBase<MeterProviderBuilder>(ExporterLauncherSettingsDriver.Console.Name)
		{
			public override void Launch(MeterProviderBuilder builder, string settings) => builder.AddConsoleExporter((options, reader) => Configure(settings, options, reader));
			protected override bool OnMatch(string name) => string.Equals(name, "Console", StringComparison.OrdinalIgnoreCase) || base.OnMatch(name);
		}

		private sealed class TracerExporterLauncher() : ExporterLauncherBase<TracerProviderBuilder>(ExporterLauncherSettingsDriver.Console.Name)
		{
			public override void Launch(TracerProviderBuilder builder, string settings) => builder.AddConsoleExporter(options => Configure(settings, options));
			protected override bool OnMatch(string name) => string.Equals(name, "Console", StringComparison.OrdinalIgnoreCase) || base.OnMatch(name);
		}
		#endregion
	}
}
