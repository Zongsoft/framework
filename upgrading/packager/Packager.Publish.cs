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
		private const string CHANNEL_OPTION = "channel";

		protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			if(context.Arguments.IsEmpty)
			{
				Terminal.WriteLine(CommandOutletColor.Red, Properties.Resources.MissingRequiredArgments);
				return null;
			}

			switch(context.Options.GetValue<string>(CHANNEL_OPTION)?.ToUpperInvariant())
			{
				case "s3":
				case "amazon.s3":
					await this.PublishToAmazonS3Async(context, cancellation);
					break;
				default:
					throw new CommandOptionValueException(CHANNEL_OPTION, context.Options.GetValue<string>(CHANNEL_OPTION));
			}

			return null;
		}

		private async ValueTask PublishToAmazonS3Async(CommandContext context, CancellationToken cancellation)
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

			using var client = new AmazonS3(server, access, secret, context.Options.GetValue<string>(REGION_OPTION));

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
	}
}
