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

using Microsoft.AspNetCore.Mvc;

using Zongsoft.Web;
using Zongsoft.Web.Security;
using Zongsoft.Upgrading.Models;
using Zongsoft.Upgrading.Services;

using Microsoft.AspNetCore.Authorization;

namespace Zongsoft.Upgrading.Web.Controllers;

[Authorize]
[Authorization]
[Area("Upgrading")]
[ControllerName("Applications")]
public class ApplicationController : ServiceController<Models.Application, ApplicationService>
{
	#region 嵌套子类
	[Authorize]
	[ControllerName("Editions")]
	public class EditionController : SubserviceController<ApplicationEdition, ApplicationService.EditionService>
	{
	}
	#endregion
}
