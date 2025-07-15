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

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Diagnostics.Telemetry;

[Service<IExporterLauncher<TracerProviderBuilder>>]
public class ZipkinExporterLauncher() : ExporterLauncherBase<TracerProviderBuilder>("Zipkin")
{
	public override void Launch(TracerProviderBuilder builder, string settings) => builder.AddZipkinExporter(options =>
	{
		if(string.IsNullOrEmpty(settings))
			return;

		var connectionSettings = new ConnectionSettings(this.Name, settings);

		if(!string.IsNullOrEmpty(connectionSettings.Server))
			options.Endpoint = new Uri(connectionSettings.Server);

		var timeout = (int)connectionSettings.GetValue("timeout", TimeSpan.FromSeconds(30)).TotalMilliseconds;
		var interval = (int)connectionSettings.GetValue("Interval", TimeSpan.FromSeconds(10)).TotalMilliseconds;

		options.ExportProcessorType = connectionSettings.GetValue("processorType", ExportProcessorType.Batch);
		options.MaxPayloadSizeInBytes = connectionSettings.GetValue("maxPayloadSize", 4096);
		options.UseShortTraceIds = connectionSettings.GetValue("useShortTraceIds", true);

		options.BatchExportProcessorOptions.MaxExportBatchSize = connectionSettings.GetValue("maxBatchSize", 512);
		options.BatchExportProcessorOptions.MaxQueueSize = connectionSettings.GetValue("maxQueueSize", 2048);
		options.BatchExportProcessorOptions.ExporterTimeoutMilliseconds = timeout;
		options.BatchExportProcessorOptions.ScheduledDelayMilliseconds = interval;
	});
}
