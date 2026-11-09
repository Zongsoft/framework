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

using OpenTelemetry.Metrics;

using Zongsoft.Services;
using Zongsoft.Communication;
using Zongsoft.Configuration;

using WebResources = Zongsoft.Diagnostics.Web.Properties.Resources;

namespace Zongsoft.Diagnostics.Telemetry;

[Service<IExporterLauncher<MeterProviderBuilder>>]
public partial class PrometheusExporterLauncher() : ExporterLauncherBase<MeterProviderBuilder>("Prometheus")
{
	/// <summary>根据完整监听前缀注册 Prometheus 指标导出器。</summary>
	/// <param name="builder">指标提供程序构建器。</param>
	/// <param name="settings">必须包含 urls 的连接设置；protocol 默认为 http1，多个地址须采用等效的基路径。</param>
	/// <exception cref="ArgumentNullException"><paramref name="builder"/> 为空。</exception>
	/// <exception cref="ArgumentException">监听地址、协议或抓取路径无效，或仍配置独立的 path 设置。</exception>
	public override void Launch(MeterProviderBuilder builder, string settings)
	{
		ArgumentNullException.ThrowIfNull(builder);

		var connectionSettings = new ConnectionSettings(settings);

		if(connectionSettings.Contains("path"))
			throw new ArgumentException(WebResources.Prometheus_PathObsolete_Message, nameof(settings));

		if(connectionSettings.Contains("protocols") ||
		   !NetworkProtocol.TryParse(connectionSettings.GetValue("protocol", "http1"), out var protocol) ||
		   protocol.Name != null || protocol.Carrier != "http1")
			throw new ArgumentException(WebResources.Prometheus_ProtocolInvalid_Message, nameof(settings));

		var urls = connectionSettings.GetValue<string>("urls");

		if(string.IsNullOrWhiteSpace(urls))
			throw new ArgumentException(WebResources.Prometheus_UrlsInvalid_Message, nameof(settings));

		string path = null;
		var prefixes = urls.Split([',', ';', '|'], StringSplitOptions.TrimEntries);
		var addresses = new NetworkAddress[prefixes.Length];

		for(var index = 0; index < prefixes.Length; index++)
		{
			if(!NetworkAddress.TryParse(prefixes[index], out var address) ||
			   address.Scheme is not ("http" or "https") || !IsPath(address.Path))
				throw new ArgumentException(WebResources.Prometheus_UrlsInvalid_Message, nameof(settings));

			var basePath = address.Path.Length > 1 ? address.Path.TrimEnd('/') : address.Path;
			if(path != null && !string.Equals(path, basePath, StringComparison.OrdinalIgnoreCase))
				throw new ArgumentException(WebResources.Prometheus_PathsDifferent_Message, nameof(settings));

			path ??= basePath;
			addresses[index] = address;
		}

		var disableTotalSuffix = !connectionSettings.GetValue("totalSuffix", true);
		var cacheDuration = (int)Math.Clamp(connectionSettings.GetValue("cacheDuration", TimeSpan.FromMilliseconds(300)).TotalMilliseconds, 0, int.MaxValue);

		builder.AddPrometheusExporter(options =>
		{
			options.ScrapeEndpointPath = path;
			options.DisableTotalNameSuffixForCounters = disableTotalSuffix;
			options.ScrapeResponseCacheDurationMilliseconds = cacheDuration;
		});

		builder.AddInstrumentation((services, provider) => new ScrapeServer(services, provider, addresses, path));
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
