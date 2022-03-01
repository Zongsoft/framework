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
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Zongsoft.Externals.Wechat.Web.Controllers
{
	[ApiController]
	[Route("Externals/Wechat/Channels")]
	public class ChannelController : ControllerBase
	{
		[HttpPost("{id}/{action}")]
		public async ValueTask<IActionResult> Postmark(string id)
		{
			if(string.IsNullOrEmpty(id))
				return this.BadRequest();

			if(!ChannelManager.TryGetChannel(id, out var channel))
				return this.NotFound();

			var content = await this.Request.ReadAsStringAsync();
			var (value, nonce, timestamp, period) = await channel.PostmarkAsync(content);

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
		public async ValueTask<IActionResult> GetCredential(string id)
		{
			if(!ChannelManager.TryGetChannel(id, out var channel))
				return this.NotFound();

			var credential = await channel.GetCredentialAsync(false);
			return string.IsNullOrEmpty(credential) ? this.NoContent() : this.Content(credential);
		}

		[HttpPost("{id}/Credential/[action]")]
		public async ValueTask<IActionResult> Refresh(string key)
		{
			if(!ChannelManager.TryGetChannel(key, out var channel))
				return this.NotFound();

			var credential = await channel.GetCredentialAsync(true);
			return string.IsNullOrEmpty(credential) ? this.NoContent() : this.Content(credential);
		}
	}

	[ApiController]
	[Route("Externals/Wechat/Channels/{id}/Users")]
	public class ChannelUserController : ControllerBase
	{
		[HttpGet("{identifier?}")]
		public async ValueTask<IActionResult> Get(string id, string identifier = null, [FromQuery]string bookmark = null)
		{
			if(!ChannelManager.TryGetChannel(id, out var channel))
				return this.NotFound();

			if(string.IsNullOrEmpty(identifier))
			{
				var result = await channel.Users.GetIdentifiersAsync(bookmark);
				this.Response.Headers["X-Bookmark"] = result.bookmark;
				return result.identifiers == null || result.identifiers.Length == 0 ? this.NoContent() : this.Ok(result.identifiers);
			}

			var info = await channel.Users.GetInfoAsync(identifier);

			if(info.Succeed)
				return string.IsNullOrEmpty(info.Value.OpenId) && string.IsNullOrEmpty(info.Value.UnionId) ? this.NoContent() : this.Ok(info.Value);

			return this.NotFound(info.Failure);
		}
	}

	[ApiController]
	[Route("Externals/Wechat/Channels/{id}/Authentication")]
	public class ChannelAuthenticationController : ControllerBase
	{
		[HttpPost("{token}")]
		public async ValueTask<IActionResult> AuthenticateAsync(string id, string token)
		{
			if(string.IsNullOrEmpty(token))
				return this.BadRequest();

			if(!ChannelManager.TryGetChannel(id, out var channel))
				return this.NotFound();

			var result = await channel.Authentication.AuthenticateAsync(token);
			return result.Succeed ? this.Ok(result.Value) : this.NotFound(result.Failure);
		}
	}
}
