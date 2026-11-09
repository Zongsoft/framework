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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Web;
using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Web.Controllers;

[Area("Externals/Wechat")]
[ControllerName("Files")]
public class FileController : ControllerBase
{
	[HttpPost("{name?}")]
	public async ValueTask<IActionResult> UploadAsync(string name = null, CancellationToken cancellation = default)
	{
		if(this.Request.ContentLength == null || this.Request.ContentLength == 0)
			return this.BadRequest();

		var filePath = await ReadAsStringAsync(this.Request);

		if(string.IsNullOrEmpty(filePath))
			return this.BadRequest();

		var result = await (await AuthorityUtility.GetAuthorityAsync(name, cancellation)).UploadAsync(filePath, cancellation);
		return string.IsNullOrEmpty(result) ? this.NoContent() : this.Content(result);
	}

	private static async ValueTask<string> ReadAsStringAsync(HttpRequest request)
	{
		using var reader = new StreamReader(
			request.Body,
			Encoding.UTF8,
			detectEncodingFromByteOrderMarks: true,
			bufferSize: 1024,
			leaveOpen: true);

		return await reader.ReadToEndAsync();
	}
}
