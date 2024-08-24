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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Web.Controllers
{
	[ApiController]
	[Route("Externals/Wechat/Files")]
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

			var result = await AuthorityUtility.GetAuthority(name).UploadAsync(filePath, cancellation);
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
}
