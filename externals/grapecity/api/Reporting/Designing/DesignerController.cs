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
using System.Linq;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;

using Zongsoft.Services;
using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Web.Reporting.Designing;

public class DesignerController : ControllerBase
{
	#region 构造函数
	private readonly IServiceProvider _serviceProvider;
	#endregion

	#region 构造函数
	public DesignerController(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}
	#endregion

	#region 公共方法
	[HttpGet]
	public IActionResult GetReports()
	{
		var providers = _serviceProvider.ResolveAll<IReportLocator>();
		var reports = new List<IReportDescriptor>();

		foreach(var provider in providers)
		{
			reports.AddRange(provider.GetReports());
		}

		return reports.Count > 0 ? this.Ok(reports.Select(report => report.Name)) : this.NoContent();
	}
	#endregion
}
