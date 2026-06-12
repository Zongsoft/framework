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

			var segment = string.IsNullOrWhiteSpace(edition) ? "_" : Uri.EscapeDataString(edition);
			using var response = await client.GetAsync($"{Application.ApplicationName}/{segment}?{GetParameters(version)}", cancellation);
			if(!response.IsSuccessStatusCode)
				yield break;

			//尝试创建一个跟踪器实例，并将其赋值给当前获取器的跟踪器属性，以便在后续的升级过程中使用
			this.Tracer ??= new WebTracer(this);

			var releases = Release.LoadAsync(await response.Content.ReadAsStreamAsync(cancellation), cancellation);
			await foreach(var release in releases)
				yield return release;

			static string GetParameters(Version version)
			{
				var text = new System.Text.StringBuilder();

				text.Append($"{nameof(Release.Name)}={Application.ApplicationName}&");
				text.Append($"{nameof(Release.Edition)}={Application.ApplicationEdition}&");
				text.Append($"{nameof(Release.Platform)}={Application.Platform.ToString()}&");
				text.Append($"{nameof(Release.Architecture)}={Application.Architecture.ToString()}&");
				text.Append($"CurrentlyVersion={Application.ApplicationVersion.ToString()}&");

				if(version != null)
					text.Append($"UpgradingVersion={version.ToString()}&");

				var profile = IO.Hardwares.HardwareProfile.Current;

				if(profile != null)
				{
					if(!string.IsNullOrEmpty(profile.Identifier))
						text.Append($"Fingerprint={profile.Identifier}&");

					if(profile.Mainboard != null && profile.Mainboard.HasUnique(out var identifier))
						text.Append($"Mainboard={identifier}&");

					foreach(var hardware in profile.Processors)
					{
						if(hardware.HasUnique(out var id))
							text.Append($"Processor={id}&");
					}

					foreach(var hardware in profile.Networks)
					{
						if(hardware.HasUnique(out var id))
							text.Append($"Network={id}&");
					}

					foreach(var hardware in profile.Memories)
					{
						if(hardware.HasUnique(out var id))
							text.Append($"Memory={id}&");
					}

					foreach(var hardware in profile.Storages)
					{
						if(hardware.HasUnique(out var id))
							text.Append($"Storage={id}&");
					}
				}

				return text.ToString().TrimEnd('&');
			}
		}
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
