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
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Data;

namespace Zongsoft.Externals.Wechat.Web.Controllers
{
	[ApiController]
	[Route("Externals/Wechat/Banks")]
	public class BankController : ControllerBase
	{
		[HttpGet("{kind}")]
		public async ValueTask<IActionResult> Get(BankUtility.BankKind kind, [FromQuery]Paging page = null, CancellationToken cancellation = default)
		{
			if(page == null)
				page = Paging.Page(1, 50);

			var result = await AuthorityUtility.GetAuthority().GetBanksAsync(kind, page, cancellation);

			if(page.TotalCount > 0)
			{
				this.Response.Headers.Add("X-Paging", page.ToString());
				this.Response.Headers.Add("X-Pagination", page.ToString());

				return this.Ok(result);
			}

			return this.NotFound();
		}

		[HttpGet("{code}/Branches")]
		public async ValueTask<IActionResult> GetBranches(string code, [FromQuery]string city, [FromQuery]Paging page = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(code))
				return this.BadRequest();
			if(string.IsNullOrEmpty(city))
				return this.BadRequest();

			if(page == null)
				page = Paging.Page(1, 50);

			var result = await AuthorityUtility.GetAuthority().GetBranchesAsync(code, city, page, cancellation);

			if(page.TotalCount > 0)
			{
				this.Response.Headers.Add("X-Paging", page.ToString());
				this.Response.Headers.Add("X-Pagination", page.ToString());

				return this.Ok(result);
			}

			return this.NotFound();
		}

		[HttpGet("Find/card:{code}")]
		public async ValueTask<IActionResult> Find(string code, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(code))
				return this.BadRequest();

			var result = await AuthorityUtility.GetAuthority().FindAsync(code, cancellation);
			return result != null ? this.Ok(result.Value) : this.NotFound();
		}
	}
}
