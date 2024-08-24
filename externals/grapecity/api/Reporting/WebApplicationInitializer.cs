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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

using GrapeCity.ActiveReports;
using GrapeCity.ActiveReports.Aspnetcore.Viewer;
using GrapeCity.ActiveReports.Aspnetcore.Designer;

using Zongsoft.Services;
using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Reporting.Web
{
	[Service(typeof(IApplicationInitializer<IApplicationBuilder>))]
	public class WebApplicationInitializer : IApplicationInitializer<IApplicationBuilder>
	{
		#region 常量定义
		private const string ROUTE_PREFIX = "/Grapecity/Reporting";
		#endregion

		#region 初始方法
		public void Initialize(IApplicationBuilder app)
		{
			var reportService = app.ApplicationServices.Resolve<Designing.ResourceService>();

			app.UseReporting(settings =>
			{
				settings.Prefix = ROUTE_PREFIX;
				settings.UseCompression = true;
				settings.UseCustomStore(reportService.GetReport);
				settings.LocateDataSource = GetData;
				settings.SetLocateDataSource(GetData);
			});

			app.UseDesigner(settings =>
			{
				settings.Prefix = ROUTE_PREFIX;
				settings.UseCustomStore(reportService);
			});
		}
		#endregion

		#region 私有方法
		private static object GetData(GrapeCity.ActiveReports.Rendering.LocateDataSourceArgs args)
		{
			var source = GetDataSource(args.DataSet.Query.DataSourceName, args.Report.DataSources);

			if(source != null)
			{
				var model = source.CreateModel(args.DataSet);
				var loader = ApplicationContext.Current.Services.Resolve<IReportDataLoader>();

				if(loader != null)
				{
					return loader.Load(null, model);
				}
			}

			return null;
		}

		private static object GetData(GrapeCity.ActiveReports.Web.Viewer.LocateDataSourceArgs args)
		{
			var source = GetDataSource(args.DataSet.Query.DataSourceName, args.Report.DataSources);

			if(source != null)
			{
				var model = source.CreateModel(args.DataSet);
				var loader = ApplicationContext.Current.Services.Resolve<IReportDataLoader>();

				if(loader != null)
				{
					return loader.Load(null, model);
				}
			}

			return null;
		}

		private static ReportDataSource GetDataSource(string name, IEnumerable<GrapeCity.ActiveReports.PageReportModel.DataSource> sources)
		{
			foreach(var source in sources)
			{
				if(source.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
				{
					return new ReportDataSource(source);
				}
			}

			return null;
		}
		#endregion
	}
}
