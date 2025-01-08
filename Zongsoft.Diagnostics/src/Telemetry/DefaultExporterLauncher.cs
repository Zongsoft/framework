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

using Zongsoft.Services;

namespace Zongsoft.Diagnostics;

[Service<Telemetry.IExporterLauncher<MeterProviderBuilder>>(Members = nameof(Meters))]
[Service<Telemetry.IExporterLauncher<TracerProviderBuilder>>(Members = nameof(Tracers))]
public class DefaultExporterLauncher
{
	public static readonly Telemetry.IExporterLauncher<MeterProviderBuilder> Meters = new MeterExporterLauncher();
	public static readonly Telemetry.IExporterLauncher<TracerProviderBuilder> Tracers = new TracerExporterLauncher();

	private sealed class MeterExporterLauncher() : Telemetry.ExporterLauncherBase<MeterProviderBuilder>("OpenTelemetry")
	{
		public override void Launch(MeterProviderBuilder builder, string settings) => builder.AddOtlpExporter();
		protected override bool OnMatch(string name) => string.IsNullOrEmpty(name) || string.Equals(name, "telemetry", StringComparison.OrdinalIgnoreCase) || base.OnMatch(name);
	}

	private sealed class TracerExporterLauncher() : Telemetry.ExporterLauncherBase<TracerProviderBuilder>("OpenTelemetry")
	{
		public override void Launch(TracerProviderBuilder builder, string settings) => builder.AddOtlpExporter();
		protected override bool OnMatch(string name) => string.IsNullOrEmpty(name) || string.Equals(name, "telemetry", StringComparison.OrdinalIgnoreCase) || base.OnMatch(name);
	}
}
