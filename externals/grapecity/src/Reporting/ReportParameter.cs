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

using GrapeCity.ActiveReports;
using GrapeCity.ActiveReports.PageReportModel;

using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Reporting
{
	public class ReportParameter : IReportParameter
	{
		#region 构造函数
		public ReportParameter(GrapeCity.ActiveReports.PageReportModel.ReportParameter parameter)
		{
			this.Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
		}
		#endregion

		#region 公共属性
		public GrapeCity.ActiveReports.PageReportModel.ReportParameter Parameter { get; }
		public string Name { get => this.Parameter.Name; set => this.Parameter.Name = value; }
		public string Label { get => this.Parameter.Prompt; set => this.Parameter.Prompt = value; }
		public object Value { get => this.Parameter.DefaultValue; set => throw new NotImplementedException(); }
		public string Description { get => this.Parameter.Prompt; set => this.Parameter.Prompt = value; }

		public ReportParameterType ParameterType
		{
			get => this.Parameter.DataType switch
			{
				ReportParameterDataType.String => ReportParameterType.String,
				ReportParameterDataType.Boolean => ReportParameterType.Boolean,
				ReportParameterDataType.Integer => ReportParameterType.Integer,
				ReportParameterDataType.Float => ReportParameterType.Float,
				ReportParameterDataType.DateTime => ReportParameterType.DateTime,
				_ => ReportParameterType.String,
			};
			set => this.Parameter.DataType = value switch
			{
				ReportParameterType.String => ReportParameterDataType.String,
				ReportParameterType.Boolean => ReportParameterDataType.Boolean,
				ReportParameterType.Integer => ReportParameterDataType.Integer,
				ReportParameterType.Float => ReportParameterDataType.Float,
				ReportParameterType.Date => ReportParameterDataType.DateTime,
				ReportParameterType.Time => ReportParameterDataType.DateTime,
				ReportParameterType.DateTime => ReportParameterDataType.DateTime,
				_ => ReportParameterDataType.String,
			};
		}
		#endregion
	}
}
