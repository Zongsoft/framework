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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Zongsoft.Externals.Wechat.Web.Controllers
{
	[ApiController]
	[Route("Externals/Wechat/Channels")]
	public class ChannelController : ControllerBase
	{
		[HttpPost("{id}/{action}")]
		public async ValueTask<IActionResult> Postmark(string id, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(id))
				return this.BadRequest();

			if(!ChannelManager.TryGetChannel(id, out var channel))
				return this.NotFound();

			var content = await this.Request.ReadAsStringAsync(cancellation);
			var (value, nonce, timestamp, period) = await channel.PostmarkAsync(content, cancellation);

			if(value == null || value.Length == 0)
				return this.NotFound();

			return this.Ok(new
			{
				Applet = channel.Account.Code,
				Nonce = nonce,
				Timestamp = timestamp,
				Period = period,
				Value = value,
			});
		}
	}

	[ApiController]
	[Route("Externals/Wechat/Channels")]
	public class ChannelCredentialController : ControllerBase
	{
		[HttpGet("{id}/Credential")]
		public async ValueTask<IActionResult> GetCredential(string id, CancellationToken cancellation = default)
		{
			if(!ChannelManager.TryGetChannel(id, out var channel))
				return this.NotFound();

			var credential = await channel.GetCredentialAsync(false, cancellation);
			return string.IsNullOrEmpty(credential) ? this.NoContent() : this.Content(credential);
		}

		[HttpPost("{id}/Credential/[action]")]
		public async ValueTask<IActionResult> Refresh(string key, CancellationToken cancellation = default)
		{
			if(!ChannelManager.TryGetChannel(key, out var channel))
				return this.NotFound();

			var credential = await channel.GetCredentialAsync(true, cancellation);
			return string.IsNullOrEmpty(credential) ? this.NoContent() : this.Content(credential);
		}
	}

	[ApiController]
	[Route("Externals/Wechat/Channels/{id}/Users")]
	public class ChannelUserController : ControllerBase
	{
		[HttpGet("{identifier?}")]
		public async ValueTask<IActionResult> Get(string id, string identifier = null, [FromQuery]string bookmark = null, CancellationToken cancellation = default)
		{
			if(!ChannelManager.TryGetChannel(id, out var channel))
				return this.NotFound();

			if(string.IsNullOrEmpty(identifier))
			{
				var result = await channel.Users.GetIdentifiersAsync(bookmark, cancellation);
				this.Response.Headers["X-Bookmark"] = result.bookmark;
				return result.identifiers == null || result.identifiers.Length == 0 ? this.NoContent() : this.Ok(result.identifiers);
			}

			var info = await channel.Users.GetInfoAsync(identifier, cancellation);
			return string.IsNullOrEmpty(info.OpenId) && string.IsNullOrEmpty(info.UnionId) ? this.NoContent() : this.Ok(info);
		}
	}

	[ApiController]
	[Route("Externals/Wechat/Channels/{id}/Messager")]
	public class ChannelMessagerController : ControllerBase
	{
		[HttpGet("Templates")]
		public async ValueTask<IActionResult> GetTemplatesAsync(string id, CancellationToken cancellation = default)
		{
			if(!ChannelManager.TryGetChannel(id, out var channel))
				return this.NotFound();

			var result = await channel.Messager.GetTemplatesAsync(cancellation);
			return result != null && result.Any() ? this.Ok(result) : this.NoContent();
		}

		[HttpPost("Send/{destination}")]
		public async ValueTask<IActionResult> SendAsync(string id, string destination, [FromQuery]string template, [FromQuery]string url, [FromBody]Dictionary<string, object> data, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(template) || data == null)
				return this.BadRequest();

			if(!ChannelManager.TryGetChannel(id, out var channel))
				return this.NotFound();

			var result = await channel.Messager.SendAsync(destination, template, data, url, cancellation);
			return string.IsNullOrEmpty(result) ? this.NoContent() : this.Content(result);
		}
	}

	[ApiController]
	[Route("Externals/Wechat/Channels/{id}/Authentication")]
	public class ChannelAuthenticationController : ControllerBase
	{
		[HttpPost("{token}")]
		public async ValueTask<IActionResult> AuthenticateAsync(string id, string token, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(token))
				return this.BadRequest();

			if(!ChannelManager.TryGetChannel(id, out var channel))
				return this.NotFound();

			var result = await channel.Authentication.AuthenticateAsync(token, cancellation);
			return this.Ok(result);
		}
	}
}
