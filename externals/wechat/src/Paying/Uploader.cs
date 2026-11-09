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
using System.IO;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

using Zongsoft.IO;
using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat.Paying;

public static class Uploader
{
	public static ValueTask<string> UploadAsync(this IAuthority authority, string filePath, CancellationToken cancellation = default)
	{
		if(authority == null)
			throw new ArgumentNullException(nameof(authority));

		if(string.IsNullOrEmpty(filePath))
			throw new ArgumentNullException(nameof(filePath));

		var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
		var type = extension switch
		{
			".png" => "image/png",
			".jpg" or ".jpeg" => "image/jpeg",
			".bmp" => "image/bmp",
			".pdf" => "application/pdf",
			".avi" => "video/x-msvideo",
			".wmv" => "video/x-ms-wmv",
			".mp4" or ".f4v" or ".m4v" => "video/mp4",
			".mov" => "video/quicktime",
			".mkv" => "video/x-matroska",
			".flv" => "video/x-flv",
			".rmvb" => "application/vnd.rn-realmedia-vbr",
			".mpeg" => "video/mpeg",
			_ => throw new OperationException("InvalidFileFormat", null),
		};
		var video = type.StartsWith("video/", StringComparison.Ordinal) || extension == ".rmvb";
		return UploadFileAsync(authority, filePath, video ? "merchant/media/video_upload" : "merchant/media/upload", type, extension == ".pdf" ? 15 * 1024 * 1024 / 2 : 5 * 1024 * 1024, cancellation);
	}

	private static async ValueTask<string> UploadFileAsync(IAuthority authority, string filePath, string url, string type, int limit, CancellationToken cancellation)
	{
		var fileName = System.IO.Path.GetFileName(filePath);
		if(string.IsNullOrWhiteSpace(fileName) || Encoding.UTF8.GetByteCount(fileName) > 128 || fileName.Any(character => char.IsControl(character) || character == '"'))
			throw new ArgumentException(null, nameof(filePath));

		byte[] data;

		await using(var file = await FileSystem.File.OpenAsync(filePath, FileMode.Open, FileAccess.Read, cancellation))
		{
			if(file.CanSeek && (file.Length == 0 || file.Length > limit))
				throw new OperationException("InvalidFileSize", null);

			using var content = new MemoryStream();
			var buffer = new byte[81920];
			int count;

			while((count = await file.ReadAsync(buffer, cancellation)) > 0)
			{
				if(content.Length + count > limit)
					throw new OperationException("InvalidFileSize", null);

				await content.WriteAsync(buffer.AsMemory(0, count), cancellation);
			}

			if(content.Length == 0)
				throw new OperationException("InvalidFileSize", null);

			data = content.ToArray();
		}

		var client = HttpClientFactory.GetHttpClient(authority);
		var boundary = Guid.NewGuid().ToString("N");
		using var form = new MultipartFormDataContent(boundary);
		form.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data; boundary=" + boundary);

		var digest = SHA256.HashData(data);
		form.Add(JsonContent.Create(new { filename = fileName, sha256 = System.Convert.ToHexString(digest) }), "meta");
		var contentFile = new ByteArrayContent(data);
		contentFile.Headers.ContentType = new MediaTypeHeaderValue(type);
		form.Add(contentFile, "file", fileName);

		using var message = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
		using var response = await client.SendAsync(message, cancellation);
		var result = await response.GetResultAsync<UploaderResult>(cancellation);

		if(string.IsNullOrWhiteSpace(result.Identifier))
			throw new FormatException();

		return result.Identifier;
	}

	private struct UploaderResult
	{
		[System.Text.Json.Serialization.JsonPropertyName("media_id")]
		public string Identifier { get; set; }
	}
}
