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
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Zongsoft.Externals.Wechat.Daemon.Controllers
{
	[ApiController]
	[Route("Externals/Wechat/Fallback")]
	public class FallbackController : ControllerBase
	{
		[HttpPost("{name}/{key?}")]
		public async Task<IActionResult> HandleAsync(string name, string key = null)
		{
			Zongsoft.Diagnostics.Logger.Debug(await GetRequestInfoAsync());

			var result = await FallbackHandlerFactory.HandleAsync(this.HttpContext, name, key);

			if(result.Succeed)
				return result.Value == null ? this.NoContent() : this.Ok(result);

			return result.Failure.Reason switch
			{
				FallbackHandlerFactory.ERROR_NOTFOUND => this.NotFound(),
				FallbackHandlerFactory.ERROR_UNSUPPORTED => this.BadRequest(),
				FallbackHandlerFactory.ERROR_CANNOTHANDLE => this.UnprocessableEntity(),
				_ => this.StatusCode((int)System.Net.HttpStatusCode.InternalServerError, result.Failure),
			};
		}

		private async ValueTask<string> GetRequestInfoAsync()
		{
			this.Request.EnableBuffering();

			var text = new System.Text.StringBuilder();

			text.Append("[" + this.Request.Method + "]");
			text.Append(this.Request.Path.ToString());

			if(this.Request.QueryString.HasValue)
			{
				text.Append("?");
				text.Append(this.Request.QueryString);
			}

			text.AppendLine();

			foreach(var header in this.Request.Headers)
			{
				text.AppendLine(header.Key + ":" + string.Join(";", header.Value));
			}

			if(this.Request.ContentLength > 0)
			{
				var reader = new StreamReader(this.Request.Body);
				text.AppendLine();
				text.AppendLine(await reader.ReadToEndAsync());
			}

			this.Request.Body.Position = 0;

			return text.ToString();
		}
	}
}
