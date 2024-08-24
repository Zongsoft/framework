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
 * This file is part of Zongsoft.Externals.Grapecity library.
 *
 * The Zongsoft.Externals.Grapecity is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Grapecity is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Grapecity library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;

using Zongsoft.Services;
using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Reporting.Web
{
	[ApiController]
	[Route("Grapecity/Reporting/Reports")]
	public class ReportController : ControllerBase
	{
		#region 成员字段
		private readonly IServiceProvider _serviceProvider;
		#endregion

		#region 构造函数
		public ReportController(IServiceProvider serviceProvider)
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
}
