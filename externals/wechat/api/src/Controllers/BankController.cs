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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Zongsoft.Web;
using Zongsoft.Data;

namespace Zongsoft.Externals.Wechat.Web.Controllers;

[Area("Externals/Wechat")]
[ControllerName("Banks")]
public class BankController : ControllerBase
{
	[HttpGet("{kind}")]
	public async ValueTask<IActionResult> Get(BankUtility.BankKind kind, [FromQuery] Paging page = null, CancellationToken cancellation = default)
	{
		page ??= Paging.Page(1, 50);
		var result = await (await AuthorityUtility.GetAuthorityAsync(cancellation: cancellation)).GetBanksAsync(kind, page, cancellation);

		if(page.Total > 0)
		{
			Zongsoft.Web.Http.HeaderDictionaryExtension.SetPagination(this.Response.Headers, page);
			return this.Ok(result);
		}

		return this.NotFound();
	}

	[HttpGet("{code}/Branches")]
	public async ValueTask<IActionResult> GetBranches(string code, [FromQuery] string city, [FromQuery] Paging page = null, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(code))
			return this.BadRequest();
		if(string.IsNullOrEmpty(city))
			return this.BadRequest();

		page ??= Paging.Page(1, 50);
		var result = await (await AuthorityUtility.GetAuthorityAsync(cancellation: cancellation)).GetBranchesAsync(code, city, page, cancellation);

		if(page.Total > 0)
		{
			Zongsoft.Web.Http.HeaderDictionaryExtension.SetPagination(this.Response.Headers, page);
			return this.Ok(result);
		}

		return this.NotFound();
	}

	[HttpGet("Find/card:{code}")]
	public async ValueTask<IActionResult> Find(string code, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(code))
			return this.BadRequest();

		var result = await (await AuthorityUtility.GetAuthorityAsync(cancellation: cancellation)).FindAsync(code, cancellation);
		return result.Count > 0 ? this.Ok(result) : this.NotFound();
	}
}
