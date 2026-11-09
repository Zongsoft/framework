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
using System.Collections.Generic;

using GrapeCity.ActiveReports;
using GrapeCity.ActiveReports.PageReportModel;

using Zongsoft.Data;
using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Reporting;

public class ReportDataModel : IReportDataModel
{
	#region 构造函数
	public ReportDataModel(IDataSet dataSet, IReportDataSource source)
	{
		this.Name = dataSet.Name;
		this.Source = source;
		this.Schema = dataSet.Query.CommandText;

		if(dataSet.Query.QueryParameters.Count > 0)
		{
			this.Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			foreach(var parameter in dataSet.Query.QueryParameters)
			{
				this.Settings.Add(parameter.Name, parameter.Value.Expression);
			}
		}
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Schema { get; set; }
	public Paging Paging { get; set; }
	public IReportDataSource Source { get; }
	public IDictionary<string, string> Settings { get; }
	#endregion
}
