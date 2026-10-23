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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Zongsoft.Web;
using Zongsoft.Security.Privileges;

namespace Zongsoft.Externals.Wechat.Web.Controllers;

[Area("Externals/Wechat")]
[ControllerName("Applets")]
public class AppletController : ControllerBase
{
	[HttpPost("{id}/[action]")]
	[RequestSizeLimit(4096)]
	public ValueTask<IActionResult> Login(string id, [FromQuery] string scenario, CancellationToken cancellation = default) =>
		WebUtility.LoginAsync(this.HttpContext, Account.Applet(id), scenario, cancellation);

	[HttpPost("{id}/PhoneNumber")]
	[RequestSizeLimit(4096)]
	public async ValueTask<IActionResult> GetPhoneNumber(string id, [FromBody] PhoneRequest request, CancellationToken cancellation = default)
	{
		if(string.IsNullOrWhiteSpace(id) || request == null || string.IsNullOrWhiteSpace(request.Code))
			return this.BadRequest();
		if(this.User.Identity?.IsAuthenticated != true || !await WebUtility.AuthorizeAccountAsync(this.HttpContext, Account.Applet(id), cancellation))
			return this.StatusCode(StatusCodes.Status403Forbidden);

		var applet = await AppletManager.FindAppletAsync(id, cancellation);
		return applet == null ? this.NotFound() : this.Ok(await applet.GetPhoneNumberAsync(request.Code, request.OpenId, cancellation));
	}

	public sealed class PhoneRequest
	{
		public string Code { get; set; }
		public string OpenId { get; set; }
	}
}
