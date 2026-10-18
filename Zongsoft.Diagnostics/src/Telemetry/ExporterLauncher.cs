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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;

using Zongsoft.Services;
using Zongsoft.Configuration;
using Zongsoft.Diagnostics.Configuration;

namespace Zongsoft.Diagnostics.Telemetry;

public static partial class ExporterLauncher
{
	#region 私有方法
	private static void Configure(string settings, OtlpExporterOptions options, LogRecordExportProcessorOptions processor)
	{
		if(string.IsNullOrEmpty(settings))
			return;

		var connectionSettings = ExporterLauncherSettingsDriver.Default.GetSettings(settings);
		Configure(connectionSettings, options);

		processor.ExportProcessorType = GetProcessorType(connectionSettings);
		Configure(connectionSettings, processor.BatchExportProcessorOptions);
	}

	private static void Configure(string settings, OtlpExporterOptions options, MetricReaderOptions reader = null)
	{
		if(string.IsNullOrEmpty(settings))
			return;

		var connectionSettings = ExporterLauncherSettingsDriver.Default.GetSettings(settings);
		Configure(connectionSettings, options);

		options.ExportProcessorType = GetProcessorType(connectionSettings);
		Configure(connectionSettings, options.BatchExportProcessorOptions);

		if(reader != null)
			reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = GetMilliseconds(connectionSettings.Interval, nameof(connectionSettings.Interval));
	}

	private static void Configure(ExporterLauncherSettings settings, OtlpExporterOptions options)
	{
		var protocol = settings.Protocol;
		if(!Enum.IsDefined(protocol))
			throw new ArgumentOutOfRangeException(nameof(settings.Protocol));

		var server = settings.ContainsKey(nameof(settings.Server)) ? settings.Server :
			(protocol == OtlpExportProtocol.Grpc ? "http://localhost:4317" : "http://localhost:4318");
		if(!Uri.TryCreate(server, UriKind.Absolute, out var endpoint) || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
			throw new ArgumentException(null, nameof(settings.Server));

		options.Protocol = protocol;
		options.TimeoutMilliseconds = GetMilliseconds(settings.Timeout, nameof(settings.Timeout));
		options.Endpoint = endpoint;
	}

	private static void Configure<T>(ExporterLauncherSettings settings, BatchExportProcessorOptions<T> options) where T : class
	{
		var queueSize = settings.MaxQueueSize;
		var batchSize = settings.MaxBatchSize;
		if(queueSize <= 0)
			throw new ArgumentOutOfRangeException(nameof(settings.MaxQueueSize));
		if(batchSize <= 0 || batchSize > queueSize)
			throw new ArgumentOutOfRangeException(nameof(settings.MaxBatchSize));

		options.MaxExportBatchSize = batchSize;
		options.MaxQueueSize = queueSize;
		options.ExporterTimeoutMilliseconds = GetMilliseconds(settings.Timeout, nameof(settings.Timeout));
		options.ScheduledDelayMilliseconds = GetMilliseconds(settings.Interval, nameof(settings.Interval));
	}

	private static void Configure(string settings, ConsoleExporterOptions options, MetricReaderOptions reader = null)
	{
		if(string.IsNullOrEmpty(settings))
			return;

		var connectionSettings = ExporterLauncherSettingsDriver.Console.GetSettings(settings);
		var target = connectionSettings.Target;
		if(target == 0 || (target & ~(ConsoleExporterOutputTargets.Console | ConsoleExporterOutputTargets.Debug)) != 0)
			throw new ArgumentOutOfRangeException(nameof(connectionSettings.Target));

		options.Targets = target;

		if(reader != null)
		{
			reader.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds = GetMilliseconds(connectionSettings.Timeout, nameof(connectionSettings.Timeout));
			reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = GetMilliseconds(connectionSettings.Interval, nameof(connectionSettings.Interval));
		}
	}

	private static int GetMilliseconds(TimeSpan duration, string name)
	{
		var milliseconds = duration.TotalMilliseconds;
		if(milliseconds < 1 || milliseconds > int.MaxValue)
			throw new ArgumentOutOfRangeException(name);

		return (int)milliseconds;
	}

	private static ExportProcessorType GetProcessorType(ExporterLauncherSettings settings)
	{
		var type = settings.ProcessorType;
		if(!Enum.IsDefined(type))
			throw new ArgumentOutOfRangeException(nameof(settings.ProcessorType));

		return type;
	}
	#endregion

	#region 服务注册
	private sealed class Registration : IServiceRegistration
	{
		public void Register(IServiceCollection services, IConfiguration configuration)
		{
			if(services.Any(service => service.ServiceType == typeof(Registration)))
				return;

			var section = configuration?.GetSection("Diagnostics:Diagnostor:Logs");
			if(section == null || !section.GetSection("Exporters").GetChildren().Any(exporter => exporter.GetChildren().Any()))
				return;

			var options = section.GetOption<Configuration.DiagnostorOptions.Logging>();
			var exporters = options?.Exporters?.Where(exporter => exporter.Enabled).ToArray();

			if(exporters == null || exporters.Length == 0)
				return;

			services.AddSingleton(this);
			services.AddLogging(logging => logging.AddOpenTelemetry());
			services.AddOptions<OpenTelemetryLoggerOptions>().Configure<IServiceProvider>((logging, provider) =>
			{
				//先解析全部驱动，避免配置错误时创建部分导出器
				var launchers = exporters.Select(exporter =>
				{
					var launcher = provider.Find<IExporterLauncher<OpenTelemetryLoggerOptions>>(exporter.Driver);

					if(launcher == null)
					{
						var exception = new InvalidOperationException(string.Format(Properties.Resources.Exporter_Missing_Message, exporter.Driver, "logs"));
						exception.Data["ExporterDriver"] = exporter.Driver;
						exception.Data["Signal"] = "logs";
						throw exception;
					}

					return launcher;
				}).ToArray();

				var name = provider.GetService<IHostEnvironment>()?.ApplicationName;
				if(string.IsNullOrWhiteSpace(name))
					name = AppDomain.CurrentDomain.FriendlyName;

				logging.IncludeScopes = options.IncludeScopes;
				logging.IncludeFormattedMessage = options.IncludeFormattedMessage;
				logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(name).AddEnvironmentVariableDetector());

				for(int i = 0; i < exporters.Length; i++)
					launchers[i].Launch(logging, exporters[i].Settings);
			});
		}
	}
	#endregion
}
