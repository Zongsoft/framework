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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Web;

namespace Zongsoft.Externals.Wechat.Web.Controllers;

[Area("Externals/Wechat")]
[ControllerName("Channels")]
public class ChannelController : ControllerBase
{
	#region 公共方法
	[HttpPost("{id}/[action]")]
	[RequestSizeLimit(4096)]
	public async ValueTask<IActionResult> Postmark(string id, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.BadRequest();

		var content = await this.Request.ReadAsStringAsync(cancellation);
		if(!Uri.TryCreate(content, UriKind.Absolute, out var url) || url.Scheme is not ("https" or "http") || !string.IsNullOrEmpty(url.UserInfo))
			return this.BadRequest();
		if(!await WebUtility.AuthorizeAccountAsync(this.HttpContext, Account.Channel(id), cancellation) ||
			!await WebUtility.AuthorizeAsync(this.HttpContext, new PostmarkContext(Account.Channel(id), url), "Wechat.JSSDK", cancellation))
			return this.StatusCode(StatusCodes.Status403Forbidden);

		var channel = await ChannelManager.FindChannelAsync(id, cancellation);
		if(channel == null)
			return this.NotFound();

		var (value, nonce, timestamp, period) = await channel.PostmarkAsync(content, cancellation);

		if(value == null || value.Length == 0)
			return this.NotFound();

		return this.Ok(new
		{
			AppId = channel.Account.Code,
			NonceStr = nonce,
			Timestamp = timestamp,
			Period = period,
			Signature = Convert.ToHexString(value).ToLowerInvariant(),
		});
	}

	[HttpPost("{id}/Login")]
	[RequestSizeLimit(4096)]
	public ValueTask<IActionResult> Login(string id, [FromQuery] string scenario, CancellationToken cancellation = default) =>
		WebUtility.LoginAsync(this.HttpContext, Account.Channel(id), scenario, cancellation);
	#endregion

	#region 嵌套子类
	[ControllerName("Users")]
	public class UserController : ControllerBase
	{
		[HttpGet("/[area]/{id}/[controller]/{identifier?}")]
		public async ValueTask<IActionResult> Get(string id, string identifier = null, [FromQuery] string bookmark = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrWhiteSpace(id))
				return this.BadRequest();
			if(this.User.Identity?.IsAuthenticated != true || !await WebUtility.AuthorizeAccountAsync(this.HttpContext, Account.Channel(id), cancellation))
				return this.StatusCode(StatusCodes.Status403Forbidden);

			var channel = await ChannelManager.FindChannelAsync(id, cancellation);
			if(channel == null)
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

	[ControllerName("Messager")]
	public class MessagerController : ControllerBase
	{
		[HttpGet("/[area]/{id}/[controller]/Templates")]
		public async ValueTask<IActionResult> GetTemplatesAsync(string id, CancellationToken cancellation = default)
		{
			if(string.IsNullOrWhiteSpace(id))
				return this.BadRequest();
			if(this.User.Identity?.IsAuthenticated != true || !await WebUtility.AuthorizeAccountAsync(this.HttpContext, Account.Channel(id), cancellation))
				return this.StatusCode(StatusCodes.Status403Forbidden);

			var channel = await ChannelManager.FindChannelAsync(id, cancellation);
			if(channel == null)
				return this.NotFound();

			var result = await channel.Messager.GetTemplatesAsync(cancellation);
			return result != null && result.Any() ? this.Ok(result) : this.NoContent();
		}

		[HttpPost("/[area]/{id}/[controller]/[action]/{destination}")]
		[RequestSizeLimit(65536)]
		public async ValueTask<IActionResult> SendAsync(string id, string destination, [FromQuery] string template, [FromQuery] string url, [FromBody] Dictionary<string, object> data, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(template) || data == null)
				return this.BadRequest();

			if(string.IsNullOrWhiteSpace(id))
				return this.BadRequest();
			if(this.User.Identity?.IsAuthenticated != true || !await WebUtility.AuthorizeAccountAsync(this.HttpContext, Account.Channel(id), cancellation))
				return this.StatusCode(StatusCodes.Status403Forbidden);

			var channel = await ChannelManager.FindChannelAsync(id, cancellation);
			if(channel == null)
				return this.NotFound();

			var result = await channel.Messager.SendAsync(destination, template, data, url, cancellation);
			return string.IsNullOrEmpty(result) ? this.NoContent() : this.Content(result);
		}
	}

	public readonly record struct PostmarkContext(Account Account, Uri Url);
	#endregion
}
