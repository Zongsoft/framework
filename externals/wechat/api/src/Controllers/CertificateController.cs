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
using Microsoft.AspNetCore.Http;

namespace Zongsoft.Externals.Wechat.Web.Controllers;

[Area("Externals/Wechat")]
[Zongsoft.Web.ControllerName("Certificates")]
public class CertificateController : ControllerBase
{
	[HttpGet("{code?}")]
	public async ValueTask<IActionResult> GetCertificateAsync(string code = null, CancellationToken cancellation = default)
	{
		var certificate = await (await AuthorityUtility.GetAuthorityAsync(cancellation: cancellation)).GetCertificateAsync(code, cancellation);
		return certificate == null ? this.NotFound() : this.Ok(certificate);
	}
}
