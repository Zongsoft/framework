﻿/*
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

using Zongsoft.Services;

namespace Zongsoft.Externals.Wechat.Web.Controllers
{
	[ApiController]
	[Route("Externals/Wechat/Applets")]
	public class AppletController : ControllerBase
	{
		[HttpPost("{key}/{action}")]
		public async ValueTask<IActionResult> Login(string key)
		{
			if(string.IsNullOrEmpty(key))
				return this.BadRequest();

			var applet = this.GetApplet(key);
			if(applet == null)
				return this.NotFound();

			var content = await this.Request.ReadAsStringAsync();
			var result = await applet.LoginAsync(content);

			return result.Succeed ? this.Ok(new { Applet = applet.Account.Code, result.Value.Identifier }) : this.NotFound(result.Failure);
		}

		private Applet GetApplet(string key)
		{
			var account = this.HttpContext.RequestServices.ResolveRequired<IAccountProvider>().GetAccount(key);
			return account.IsEmpty ? null : new Applet(account);
		}
	}
}
