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
 * This file is part of Zongsoft.Upgrading library.
 *
 * The Zongsoft.Upgrading is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Upgrading is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Upgrading library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Upgrading;

partial class Fetcher
{
	internal sealed class WebFetcher : Fetcher
	{
		#region 常量定义
		private const string URL_SETTING = "url";
		private const string TIMEOUT_SETTING = "timeout";
		#endregion

		#region 构造函数
		public WebFetcher() : base("Web") => this.Downloader = new Downloader.WebDownloader(this);
		#endregion

		#region 公共属性
		public HttpClient Client
		{
			get
			{
				if(field == null)
				{
					var settings = this.Settings;

					if(settings != null)
					{
						var timeout = settings.TryGetValue(TIMEOUT_SETTING, out var value) && Common.TimeSpanUtility.TryParse(value, out var timespan) ? timespan : TimeSpan.Zero;

						if(settings.TryGetValue(URL_SETTING, out var url) && !string.IsNullOrEmpty(url))
							return field = HttpUtility.CreateClient(url, timeout);
					}
				}

				return field;
			}
		}
		#endregion

		#region 重写方法
		protected override async IAsyncEnumerable<Release> OnFetchAsync(string edition, Version version, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation)
		{
			var client = this.Client;
			if(client == null)
				yield break;

			using var request = new HttpRequestMessage(HttpMethod.Get, $"{Application.ApplicationName}/{edition}?{GetParameters(version, IO.Hardwares.HardwareProfile.Current)}");
			SetProfileHeader(request, IO.Hardwares.HardwareProfile.Current);

			using var response = await client.SendAsync(request, cancellation);
			if(!response.IsSuccessStatusCode)
				yield break;

			//尝试创建一个跟踪器实例，并将其赋值给当前获取器的跟踪器属性，以便在后续的升级过程中使用
			this.Tracer ??= new WebTracer(this);

			var releases = Release.LoadAsync(await response.Content.ReadAsStreamAsync(cancellation), cancellation);
			await foreach(var release in releases)
				yield return release;

			static string GetParameters(Version version, IO.Hardwares.HardwareProfile profile)
			{
				var text = new System.Text.StringBuilder();

				text.Append($"{nameof(Release.Name)}={Application.ApplicationName}&");
				text.Append($"{nameof(Release.Edition)}={Application.ApplicationEdition}&");
				text.Append($"{nameof(Release.Platform)}={Application.Platform.ToString()}&");
				text.Append($"{nameof(Release.Architecture)}={Application.Architecture.ToString()}&");
				text.Append($"{nameof(Environment.MachineName)}={Environment.MachineName}&");
				text.Append($"CurrentlyVersion={Application.ApplicationVersion.ToString()}&");

				if(version != null)
					text.Append($"UpgradingVersion={version.ToString()}&");

				if(!string.IsNullOrEmpty(profile?.Identifier))
					text.Append($"Fingerprint={profile.Identifier}&");

				return text.ToString().TrimEnd('&');
			}
		}
		#endregion

		#region 私有方法
		private static void SetProfileHeader(HttpRequestMessage request, IO.Hardwares.HardwareProfile profile)
		{
			if(request == null || profile == null)
				return;

			var text = new System.Text.StringBuilder();

			AppendProfile(text, "Mainboard", GetHardwareText(profile.Mainboard));
			AppendProfile(text, "Processor", GetHardwareText(profile.Processors, null));
			AppendProfile(text, "Memory", GetHardwareText(profile.Memories, ["Capacity", "Size"]));
			AppendProfile(text, "Storage", GetHardwareText(profile.Storages, ["Size", "Capacity"]));
			AppendProfile(text, "Network", GetNetworkText(profile.Networks));

			if(text.Length > 0)
				request.Headers.TryAddWithoutValidation("X-Upgrading-Profile", EncodeHeaderValue(text.ToString()));

			static void AppendProfile(System.Text.StringBuilder text, string name, string value)
			{
				if(string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
					return;

				if(text.Length > 0)
					text.Append(';');

				text.Append(name);
				text.Append('=');
				text.Append(value);
			}
		}

		private static string GetHardwareText(IEnumerable<IO.Hardwares.IHardware> hardwares, string[] propertyNames)
		{
			if(hardwares == null)
				return null;

			var text = new System.Text.StringBuilder();

			foreach(var hardware in hardwares)
				Append(text, GetHardwareText(hardware, propertyNames));

			return text.Length > 0 ? text.ToString() : null;
		}

		private static string GetHardwareText(IO.Hardwares.IHardware hardware, string[] propertyNames = null)
		{
			if(hardware == null)
				return null;

			var text = new System.Text.StringBuilder();

			if(hardware.HasUnique(out var identifier))
				AppendPart(text, "Id", identifier);

			AppendPart(text, nameof(hardware.Name), hardware.Name);
			AppendPart(text, nameof(hardware.Model), hardware.Model);
			AppendPart(text, nameof(hardware.Manufacturer), hardware.Manufacturer);

			if(propertyNames != null)
			{
				for(int i = 0; i < propertyNames.Length; i++)
					AppendPart(text, propertyNames[i], GetPropertyValue(hardware.Properties, propertyNames[i]));
			}

			return text.Length > 0 ? text.ToString() : null;
		}

		private static string GetNetworkText(IEnumerable<IO.Hardwares.IHardware> hardwares)
		{
			if(hardwares == null)
				return null;

			var text = new System.Text.StringBuilder();

			foreach(var hardware in hardwares)
			{
				if(hardware == null)
					continue;

				var item = new System.Text.StringBuilder();

				if(hardware.HasUnique(out var identifier))
					AppendPart(item, "MacAddr", identifier);

				AppendPart(item, nameof(hardware.Name), hardware.Name);
				AppendPart(item, "IP", GetIPAddresses(hardware.Components));

				Append(text, item.Length > 0 ? item.ToString() : null);
			}

			return text.Length > 0 ? text.ToString() : null;
		}

		private static string GetIPAddresses(IEnumerable<IO.Hardwares.HardwareComponent> components)
		{
			if(components == null)
				return null;

			var text = new System.Text.StringBuilder();

			foreach(var component in components)
			{
				if(component == null)
					continue;

				if(string.Equals(component.Type, "network/ip/unicast", StringComparison.OrdinalIgnoreCase) && component.Properties.Contains("Address"))
					Append(text, Convert.ToString(component.Properties["Address"].Value), ",");

				var children = GetIPAddresses(component.Components);
				if(!string.IsNullOrEmpty(children))
					Append(text, children, ",");
			}

			return text.Length > 0 ? text.ToString() : null;
		}

		private static string GetPropertyValue(IO.Hardwares.HardwarePropertyCollection properties, string name)
		{
			if(properties == null || string.IsNullOrEmpty(name) || !properties.Contains(name))
				return null;

			return Convert.ToString(properties[name].Value);
		}

		private static void Append(System.Text.StringBuilder text, string value, string separator = "|")
		{
			if(string.IsNullOrWhiteSpace(value))
				return;

			if(text.Length > 0)
				text.Append(separator);

			text.Append(value);
		}

		private static void AppendPart(System.Text.StringBuilder text, string name, string value)
		{
			if(string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
				return;

			if(text.Length > 0)
				text.Append(',');

			text.Append(name);
			text.Append(':');
			text.Append(Sanitize(value));
		}

		private static string EncodeHeaderValue(string value) => string.IsNullOrEmpty(value) ? null : $"base64:{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value))}";
		private static string Sanitize(string value) => value?.Replace('\r', ' ').Replace('\n', ' ').Replace(';', ',').Replace('=', ':').Trim();
		#endregion

		#region 嵌套子类
		private sealed class WebTracer(WebFetcher fetcher) : ITracer
		{
			private readonly WebFetcher _fetcher = fetcher;
			private readonly string _fingerprint = IO.Hardwares.HardwareProfile.Current?.Identifier;

			public async ValueTask TraceAsync(string phase, string message, IEnumerable<KeyValuePair<string, string>> parameters, CancellationToken cancellation)
			{
				try
				{
					var content = new FormUrlEncodedContent(new Dictionary<string, string>(parameters) { ["message"] = message });
					var response = await _fetcher.Client.PostAsync($"Trace/{phase}?fingerprint={_fingerprint}", content, cancellation);

					if(response.IsSuccessStatusCode)
						await Diagnostics.Logging.GetLogging(typeof(WebTracer)).InfoAsync($"Traced the upgrading process with fingerprint '{_fingerprint}' and phase '{phase}'.", cancellation);
					else
						await Diagnostics.Logging.GetLogging(typeof(WebTracer)).WarnAsync($"Failed to trace the upgrading process with fingerprint '{_fingerprint}' and phase '{phase}'. The server responded with status code {response.StatusCode}.", cancellation);
				}
				catch(Exception ex)
				{
					await Diagnostics.Logging.GetLogging(typeof(WebTracer)).ErrorAsync($"Failed to trace the upgrading process with fingerprint '{_fingerprint}'.", ex, cancellation);
				}
			}
		}
		#endregion
	}
}
