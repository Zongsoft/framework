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
using OpenTelemetry.Metrics;

using Zongsoft.Services;
using Zongsoft.Communication;
using Zongsoft.Configuration;

namespace Zongsoft.Diagnostics.Telemetry;

[Service<IExporterLauncher<MeterProviderBuilder>>]
public class PrometheusExporterLauncher() : ExporterLauncherBase<MeterProviderBuilder>("Prometheus")
{
	/// <summary>根据完整监听前缀注册 Prometheus 指标导出器。</summary>
	/// <param name="builder">指标提供程序构建器。</param>
	/// <param name="settings">必须包含 urls 的连接设置；protocol 默认为 http1，多个地址须采用相同的字面路径。</param>
	/// <exception cref="ArgumentNullException"><paramref name="builder"/> 为空。</exception>
	/// <exception cref="ArgumentException">监听地址、协议或抓取路径无效，或仍配置独立的 path 设置。</exception>
	public override void Launch(MeterProviderBuilder builder, string settings)
	{
		ArgumentNullException.ThrowIfNull(builder);

		var connectionSettings = new ConnectionSettings(settings);

		if(connectionSettings.Contains("path"))
			throw new ArgumentException(Properties.Resources.Prometheus_PathObsolete_Message, nameof(settings));

		if(connectionSettings.Contains("protocols") ||
		   !NetworkProtocol.TryParse(connectionSettings.GetValue("protocol", "http1"), out var protocol) ||
		   protocol.Name != null || protocol.Carrier != "http1")
			throw new ArgumentException(Properties.Resources.Prometheus_ProtocolInvalid_Message, nameof(settings));

		var urls = connectionSettings.GetValue<string>("urls");

		if(string.IsNullOrWhiteSpace(urls))
			throw new ArgumentException(Properties.Resources.Prometheus_UrlsInvalid_Message, nameof(settings));

		string path = null;
		var prefixes = urls.Split([',', ';', '|'], StringSplitOptions.TrimEntries);

		foreach(var prefix in prefixes)
		{
			if(!NetworkAddress.TryParse(prefix, out var address) ||
			   address.Scheme is not ("http" or "https") || !IsPath(address.Path))
				throw new ArgumentException(Properties.Resources.Prometheus_UrlsInvalid_Message, nameof(settings));

			if(path != null && !string.Equals(path, address.Path, StringComparison.Ordinal))
				throw new ArgumentException(Properties.Resources.Prometheus_PathsDifferent_Message, nameof(settings));

			path = address.Path;
		}

		var disableTotalSuffix = !connectionSettings.GetValue("totalSuffix", true);
		var cacheDuration = (int)Math.Clamp(connectionSettings.GetValue("cacheDuration", TimeSpan.FromMilliseconds(300)).TotalMilliseconds, 0, int.MaxValue);

		builder.AddPrometheusExporter(options =>
		{
			options.ScrapeEndpointPath = path;
			options.DisableTotalNameSuffixForCounters = disableTotalSuffix;
			options.ScrapeResponseCacheDurationMilliseconds = cacheDuration;
		});

		builder.AddPrometheusHttpListener(options =>
		{
			options.ScrapeEndpointPath = path;
			options.DisableTotalNameSuffixForCounters = disableTotalSuffix;
			options.ScrapeResponseCacheDurationMilliseconds = cacheDuration;

			options.ConfigureHttpListener = (_, listener) =>
			{
				listener.Prefixes.Clear();

				//HttpListener 要求前缀以斜杠结尾；只适配监听前缀，保留配置及抓取路径
				foreach(var prefix in prefixes)
					listener.Prefixes.Add(prefix.EndsWith('/') ? prefix : prefix + "/");
			};
		});
	}

	private static bool IsPath(string path)
	{
		foreach(var character in path)
		{
			if(!char.IsAsciiLetterOrDigit(character) && character is not ('/' or '-' or '.' or '_' or '~'))
				return false;
		}

		if(path == "/")
			return true;

		var segments = path.EndsWith('/') ? path[1..^1] : path[1..];

		foreach(var segment in segments.Split('/'))
		{
			if(segment is "" or "." or "..")
				return false;
		}

		return true;
	}
}
