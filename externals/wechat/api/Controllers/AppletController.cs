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
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Zongsoft.Externals.Wechat.Web.Controllers
{
	[ApiController]
	[Route("Externals/Wechat/Applets")]
	public class AppletController : ControllerBase
	{
		[HttpPost("{action}")]
		[HttpPost("{id}/{action}")]
		public async ValueTask<IActionResult> Login(string id, CancellationToken cancellation = default)
		{
			var token = await this.Request.ReadAsStringAsync(cancellation);

			if(string.IsNullOrEmpty(token))
				return this.BadRequest();

			if(!AppletManager.TryGetApplet(id, out var applet))
				return this.NotFound();

			var result = await applet.LoginAsync(token, cancellation);
			return string.IsNullOrEmpty(result.OpenId) ? this.NotFound() : this.Ok(result);
		}

		[HttpGet("Phone/{token}")]
		[HttpGet("PhoneNumber/{token}")]
		[HttpGet("{id}/Phone/{token}")]
		[HttpGet("{id}/PhoneNumber/{token}")]
		public async ValueTask<IActionResult> GetPhoneNumber(string id, string token, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(token))
				return this.BadRequest();

			if(!AppletManager.TryGetApplet(id, out var applet))
				return this.NotFound();

			var result = await applet.GetPhoneNumberAsync(token, cancellation);
			return string.IsNullOrEmpty(result) ? this.NotFound() : this.Content(result);
		}
	}

	[ApiController]
	[Route("Externals/Wechat/Applets/{id}/Users")]
	public class AppletUserController : ControllerBase
	{
		[HttpGet("{identifier?}")]
		public async ValueTask<IActionResult> Get(string id, string identifier = null, [FromQuery]string bookmark = null, CancellationToken cancellation = default)
		{
			if(!AppletManager.TryGetApplet(id, out var applet))
				return this.NotFound();

			if(string.IsNullOrEmpty(identifier))
			{
				var result = await applet.Users.GetIdentifiersAsync(bookmark, cancellation);
				this.Response.Headers["X-Bookmark"] = result.bookmark;
				return result.identifiers == null || result.identifiers.Length == 0 ? this.NoContent() : this.Ok(result.identifiers);
			}

			var info = await applet.Users.GetInfoAsync(identifier, cancellation);
			return string.IsNullOrEmpty(info.OpenId) && string.IsNullOrEmpty(info.UnionId) ? this.NoContent() : this.Ok(info);
		}
	}

	[ApiController]
	[Route("Externals/Wechat/Applets")]
	public class AppletCredentialController : ControllerBase
	{
		[HttpGet("Credential")]
		[HttpGet("{key}/Credential")]
		public async ValueTask<IActionResult> GetCredential(string key, CancellationToken cancellation = default)
		{
			if(!AppletManager.TryGetApplet(key, out var applet))
				return this.NotFound();

			var credential = await applet.GetCredentialAsync(false, cancellation);
			return string.IsNullOrEmpty(credential) ? this.NoContent() : this.Content(credential);
		}

		[HttpPost("Credential/[action]")]
		[HttpPost("{key}/Credential/[action]")]
		public async ValueTask<IActionResult> Refresh(string key, CancellationToken cancellation = default)
		{
			if(!AppletManager.TryGetApplet(key, out var applet))
				return this.NotFound();

			var credential = await applet.GetCredentialAsync(true, cancellation);
			return string.IsNullOrEmpty(credential) ? this.NoContent() : this.Content(credential);
		}
	}
}
