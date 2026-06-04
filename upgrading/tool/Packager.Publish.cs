/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * The MIT License (MIT)
 * 
 * Copyright (C) 2020-2026 Zongsoft Corporation <http://www.zongsoft.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Upgrading;

partial class Packager
{
	[CommandOption(CHANNEL_OPTION, typeof(string), Required = true)]
	public sealed class PublishCommand : CommandBase<CommandContext>
	{
		#region 常量定义
		private const string CHANNEL_OPTION = "channel";
		private const string AUTHORIZATION_OPTION = "authorization";
		private const string CREDENTIAL_OPTION = "credential";
		#endregion

		#region 单例字段
		private static readonly JsonSerializerOptions _jsonOptions = new()
		{
			PropertyNameCaseInsensitive = true,
		};
		#endregion

		#region 重写方法
		protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			if(context.Arguments.IsEmpty)
			{
				Terminal.WriteLine(CommandOutletColor.Red, Properties.Resources.MissingRequiredArgments);
				return null;
			}

			switch(context.Options.GetValue<string>(CHANNEL_OPTION)?.ToLowerInvariant())
			{
				case "web":
					await PublishToWebAsync(context, cancellation);
					break;
				case "s3":
				case "amazon.s3":
					await PublishToAmazonS3Async(context, cancellation);
					break;
				default:
					throw new CommandOptionValueException(CHANNEL_OPTION, context.Options.GetValue<string>(CHANNEL_OPTION));
			}

			return null;
		}
		#endregion

		#region 发布方法
		private static async ValueTask PublishToWebAsync(CommandContext context, CancellationToken cancellation)
		{
			const string URL_OPTION = "url";

			if(!context.Options.TryGetValue<string>(URL_OPTION, out var url) || string.IsNullOrWhiteSpace(url))
				throw new CommandOptionMissingException(URL_OPTION);

			using var client = new HttpClient();
			ConfigureClient(client, context);

			var endpoint = GetReleasesEndpoint(url);

			foreach(var argument in context.Arguments)
			{
				var packagePath = GetPackagePath(argument);
				if(!File.Exists(packagePath))
				{
					Terminal.WriteLine(CommandOutletColor.Red, string.Format(Properties.Resources.FileNotExist_Message, packagePath));
					continue;
				}

				var manifestPath = GetManifestPath(argument);
				if(!File.Exists(manifestPath))
				{
					Terminal.WriteLine(CommandOutletColor.Red, string.Format(Properties.Resources.FileNotExist_Message, manifestPath));
					continue;
				}

				var releases = await ImportManifestAsync(client, endpoint, manifestPath, cancellation);

				//显示清单文件发布成功
				Terminal.WriteLine(CommandOutletColor.DarkGreen, string.Format(Properties.Resources.ManifestPublishedSuccessfully_Message, Path.GetFileName(manifestPath)));

				foreach(var release in releases)
					await UploadPackageAsync(client, endpoint, release.ReleaseId, packagePath, cancellation);

				//显示包文件发布成功
				Terminal.WriteLine(CommandOutletColor.DarkGreen, string.Format(Properties.Resources.PackagePublishedSuccessfully_Message, Path.GetFileName(packagePath)));

				foreach(var release in releases)
					await PublishReleaseAsync(client, endpoint, release.ReleaseId, cancellation);
			}
		}

		private static async ValueTask PublishToAmazonS3Async(CommandContext context, CancellationToken cancellation)
		{
			const string SERVER_OPTION = "server";
			const string REGION_OPTION = "region";
			const string ACCESS_OPTION = "access";
			const string SECRET_OPTION = "secret";
			const string DESTINATION_OPTION = "destination";

			if(!context.Options.TryGetValue<string>(SERVER_OPTION, out var server) || string.IsNullOrWhiteSpace(server))
				throw new CommandOptionMissingException(SERVER_OPTION);

			if(!context.Options.TryGetValue<string>(ACCESS_OPTION, out var access) || string.IsNullOrWhiteSpace(access))
				throw new CommandOptionMissingException(ACCESS_OPTION);

			if(!context.Options.TryGetValue<string>(SECRET_OPTION, out var secret) || string.IsNullOrWhiteSpace(secret))
				throw new CommandOptionMissingException(SECRET_OPTION);

			if(!context.Options.TryGetValue<string>(DESTINATION_OPTION, out var destination) || string.IsNullOrWhiteSpace(destination))
				throw new CommandOptionMissingException(DESTINATION_OPTION);

			destination = destination.Trim('/');
			using var client = new AmazonS3(server, access, secret, context.Options.GetValue<string>(REGION_OPTION, null));

			foreach(var argument in context.Arguments)
			{
				var packagePath = GetPackagePath(argument);
				if(!File.Exists(packagePath))
				{
					Terminal.WriteLine(CommandOutletColor.Red, string.Format(Properties.Resources.FileNotExist_Message, packagePath));
					continue;
				}

				var manifestPath = GetManifestPath(argument);
				if(!File.Exists(manifestPath))
				{
					Terminal.WriteLine(CommandOutletColor.Red, string.Format(Properties.Resources.FileNotExist_Message, manifestPath));
					continue;
				}

				using var package = File.OpenRead(packagePath);
				await client.UploadAsync(package, $"{destination}/{Path.GetFileName(packagePath)}", cancellation);
				package.Dispose();

				//显示包文件发布成功
				Terminal.WriteLine(CommandOutletColor.DarkGreen, string.Format(Properties.Resources.PackagePublishedSuccessfully_Message, Path.GetFileName(packagePath)));

				using var manifest = File.OpenRead(manifestPath);
				await client.UploadAsync(manifest, $"{destination}/{Path.GetFileName(manifestPath)}", cancellation);
				manifest.Dispose();

				//显示清单文件发布成功
				Terminal.WriteLine(CommandOutletColor.DarkGreen, string.Format(Properties.Resources.ManifestPublishedSuccessfully_Message, Path.GetFileName(manifestPath)));
			}
		}
		#endregion

		#region 私有方法
		private static void ConfigureClient(HttpClient client, CommandContext context)
		{
			if(context.Options.TryGetValue<string>(AUTHORIZATION_OPTION, out var authorization) && !string.IsNullOrWhiteSpace(authorization))
				client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorization);
			else if(context.Options.TryGetValue<string>(CREDENTIAL_OPTION, out var credential) && !string.IsNullOrWhiteSpace(credential))
				client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Credential {credential}");
		}

		private static async ValueTask<WebRelease[]> ImportManifestAsync(HttpClient client, Uri endpoint, string manifestPath, CancellationToken cancellation)
		{
			using var content = new MultipartFormDataContent();
			using var stream = File.OpenRead(manifestPath);
			using var file = new StreamContent(stream);
			file.Headers.ContentType = new("application/xml");
			content.Add(file, "file", Path.GetFileName(manifestPath));

			using var response = await client.PostAsync(new Uri(endpoint, "Import?format=manifest"), content, cancellation);
			await EnsureSuccessAsync(response, cancellation);

			if(response.StatusCode == System.Net.HttpStatusCode.NoContent)
				throw new InvalidOperationException($"The manifest file '{manifestPath}' did not import any release.");

			await using var result = await response.Content.ReadAsStreamAsync(cancellation);
			var releases = await JsonSerializer.DeserializeAsync<WebRelease[]>(result, _jsonOptions, cancellation);

			if(releases == null || releases.Length == 0)
				throw new InvalidOperationException($"The manifest file '{manifestPath}' did not import any release.");

			for(int i = 0; i < releases.Length; i++)
			{
				if(releases[i].ReleaseId == 0)
					throw new InvalidOperationException($"The web service returned an invalid release id for manifest file '{manifestPath}'.");
			}

			return releases;
		}

		private static async ValueTask UploadPackageAsync(HttpClient client, Uri endpoint, uint releaseId, string packagePath, CancellationToken cancellation)
		{
			using var content = new MultipartFormDataContent();
			using var stream = File.OpenRead(packagePath);
			using var file = new StreamContent(stream);
			file.Headers.ContentType = new("application/zip");
			content.Add(file, "file", Path.GetFileName(packagePath));

			using var response = await client.PostAsync(new Uri(endpoint, $"{releaseId}/Upload?overwrite=true"), content, cancellation);
			await EnsureSuccessAsync(response, cancellation);
		}

		private static async ValueTask PublishReleaseAsync(HttpClient client, Uri endpoint, uint releaseId, CancellationToken cancellation)
		{
			using var content = new StringContent("{\"Published\":true}", Encoding.UTF8, "application/json");
			using var response = await client.PatchAsync(new Uri(endpoint, releaseId.ToString()), content, cancellation);
			await EnsureSuccessAsync(response, cancellation);
		}

		private static async ValueTask EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellation)
		{
			if(response.IsSuccessStatusCode)
				return;

			var content = response.Content == null ? null : await response.Content.ReadAsStringAsync(cancellation);

			throw new HttpRequestException(string.IsNullOrWhiteSpace(content) ?
				$"The web service returned {(int)response.StatusCode} ({response.ReasonPhrase})." :
				$"The web service returned {(int)response.StatusCode} ({response.ReasonPhrase}): {content}");
		}

		private static Uri GetReleasesEndpoint(string url)
		{
			if(!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
				throw new UriFormatException($"The specified web publish URL '{url}' is invalid.");

			var builder = new UriBuilder(uri)
			{
				Query = null,
				Fragment = null,
			};

			var path = builder.Path.TrimEnd('/');

			if(string.IsNullOrEmpty(path) || path == "/")
				builder.Path = "/Upgrading/Releases/";
			else if(path.EndsWith("/Releases", StringComparison.OrdinalIgnoreCase))
				builder.Path = path + "/";
			else if(path.EndsWith("/Upgrading", StringComparison.OrdinalIgnoreCase))
				builder.Path = path + "/Releases/";
			else if(path.EndsWith("/Upgrading/Upgrader", StringComparison.OrdinalIgnoreCase))
				builder.Path = path[..^"/Upgrader".Length] + "/Releases/";
			else
				builder.Path = path + "/Upgrading/Releases/";

			return builder.Uri;
		}

		static string GetPackagePath(string path)
		{
			if(string.IsNullOrEmpty(path))
				return null;

			var extension = Path.GetExtension(path);
			if(extension == ".zip")
				return path;
			if(extension == Manifest.FILE_NAME)
				return Path.ChangeExtension(path, ".zip");

			return $"{path}.zip";
		}

		static string GetManifestPath(string path)
		{
			if(string.IsNullOrEmpty(path))
				return null;

			var extension = Path.GetExtension(path);
			if(extension == Manifest.FILE_NAME)
				return path;
			if(extension == ".zip")
				return Path.ChangeExtension(path, Manifest.FILE_NAME);

			return $"{path}{Manifest.FILE_NAME}";
		}

		private sealed class WebRelease
		{
			public uint ReleaseId { get; set; }
		}
		#endregion
	}
}
