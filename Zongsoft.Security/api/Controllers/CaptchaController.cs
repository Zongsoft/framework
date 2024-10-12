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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Web;
using Zongsoft.Web.Http;
using Zongsoft.Services;
using Zongsoft.Collections;

namespace Zongsoft.Security.Controllers
{
	[ApiController]
	[Area(Module.NAME)]
	[Route("[area]/[controller]")]
	public class CaptchaController : ControllerBase
	{
		[HttpPost("{scheme}")]
		public async Task<IActionResult> IssueAsync(string scheme, [FromQuery]string extra = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(scheme))
				return this.BadRequest();

			var captch = this.HttpContext.RequestServices.Resolve<ICaptcha>(scheme);
			if(captch == null)
				return this.BadRequest($"The specified '{scheme}' captcha does not exist.");

			object argument = this.Request.HasFormContentType ?
				await this.Request.ReadFormAsync(cancellation) :
				await this.Request.ReadAsStringAsync();

			var result = await captch.IssueAsync(argument, new Parameters(this.Request.GetParameters()), cancellation);
			return result == null ? this.NoContent() : this.Ok(result);
		}

		[HttpPost("{scheme}/[action]")]
		public async Task<IActionResult> VerifyAsync(string scheme, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(scheme))
				return this.BadRequest();

			var captch = this.HttpContext.RequestServices.Resolve<ICaptcha>(scheme);
			if(captch == null)
				return this.BadRequest($"The specified '{scheme}' captcha does not exist.");

			object argument = this.Request.HasFormContentType ?
				await this.Request.ReadFormAsync(cancellation) :
				await this.Request.ReadAsStringAsync();

			var result = await captch.VerifyAsync(argument, new Parameters(this.Request.GetParameters()), cancellation);
			return string.IsNullOrEmpty(result) ? this.NoContent() : this.Content(result);
		}
	}
}