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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

using Zongsoft.IO;
using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat.Paying
{
	public static class Uploader
	{
		private static readonly SHA256 _sha256 = SHA256.Create();

		public static ValueTask<OperationResult<string>> UploadAsync(this IAuthority authority, string filePath, CancellationToken cancellation = default)
		{
			if(authority == null)
				throw new ArgumentNullException(nameof(authority));

			if(string.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));

			var extension = System.IO.Path.GetExtension(filePath);

			switch(extension.ToLowerInvariant())
			{
				case ".png":
				case ".jpg":
				case ".jpeg":
					return UploadFileAsync(authority, filePath, "merchant/media/upload", cancellation);
				case ".avi":
				case ".wmv":
				case ".mp4":
				case ".mov":
				case ".mkv":
				case ".flv":
				case ".f4v":
				case ".m4v":
				case ".rmvb":
				case ".mpeg":
					return UploadFileAsync(authority, filePath, "merchant/media/video_upload", cancellation);
			}

			return ValueTask.FromResult((OperationResult<string>)OperationResult.Fail("InvalidFileFormat", $"Unsupported '{extension}' file format."));
		}

		private static async ValueTask<OperationResult<string>> UploadFileAsync(this IAuthority authority, string filePath, string url, CancellationToken cancellation = default)
		{
			if(authority == null)
				throw new ArgumentNullException(nameof(authority));

			if(string.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));

			var fileStream = FileSystem.File.Open(filePath, FileMode.Open, FileAccess.Read);
			var stream = new MemoryStream();
			await fileStream.CopyToAsync(stream);
			var data = stream.ToArray();

			stream.Position = 0;

			var client = HttpClientFactory.GetHttpClient(authority.Certificate);

			var form = new MultipartFormDataContent();
			var fileName = System.IO.Path.GetFileName(filePath);
			var digest = _sha256.ComputeHash(data);

			stream.Position = 0;

			form.Add(JsonContent.Create(new { filename = fileName, sha256 = System.Convert.ToHexString(digest) }), "meta");
			form.Add(new ByteArrayContent(data), "file", fileName);

			var response = await client.PostAsync(url, form, cancellation);
			var result = await response.GetResultAsync<UploaderResult>(cancellation);
			return result.Succeed ? OperationResult.Success(result.Value.Identifier) : result.Failure;
		}

		private struct UploaderResult
		{
			[System.Text.Json.Serialization.JsonPropertyName("media_id")]
			public string Identifier { get; set; }
		}
	}
}
