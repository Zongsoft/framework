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
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Builder;

using GrapeCity.ActiveReports;
using GrapeCity.ActiveReports.Aspnetcore.Viewer;
using GrapeCity.ActiveReports.Aspnetcore.Designer;

using Zongsoft.Services;
using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Reporting.Web;

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
	private static object GetData(GrapeCity.ActiveReports.Rendering.LocateDataSourceArgs args) =>
		GetData(args.DataSet, args.Report.DataSources);

	private static object GetData(GrapeCity.ActiveReports.Web.Viewer.LocateDataSourceArgs args) =>
		GetData(args.DataSet, args.Report.DataSources);

	private static object GetData(GrapeCity.ActiveReports.PageReportModel.IDataSet dataSet, IEnumerable<GrapeCity.ActiveReports.PageReportModel.DataSource> sources)
	{
		var source = GetDataSource(dataSet.Query.DataSourceName, sources);
		if(source == null)
			return null;

		var loader = ApplicationContext.Current.Services.Resolve<IReportDataLoader>();
		if(loader == null)
			return null;

		var model = source.CreateModel(dataSet);
		return Task.Run(() => model.LoadAsync(loader, null).AsTask()).GetAwaiter().GetResult();
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
