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
using System.Collections.Generic;

using GrapeCity.ActiveReports;
using GrapeCity.ActiveReports.PageReportModel;

using Zongsoft.Data;
using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Reporting
{
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
				foreach(var parameter in dataSet.Query.QueryParameters)
				{
					Settings.Add(parameter.Name, parameter.Value.Expression);
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
}
