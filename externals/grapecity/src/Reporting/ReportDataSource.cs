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

using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Reporting
{
	public class ReportDataSource : IReportDataSource
	{
		public ReportDataSource(DataSource source)
		{
			this.Source = source ?? throw new ArgumentNullException(nameof(source));
			this.Name = source.Name;
			this.Provider = source.ConnectionProperties.DataProvider;

			var connectionString = source.ConnectionProperties.ConnectString.Expression;

			if(!string.IsNullOrEmpty(connectionString))
			{
				var parts = Zongsoft.Common.StringExtension.Slice(connectionString, ';');
				Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

				foreach(var part in parts)
				{
					if(part != null && part.Length > 0)
					{
						var index = part.IndexOf('=');

						if(index > 0)
						{
							var key = part.Substring(0, index);

							if(index >= part.Length - 1)
								Settings.Add(key, null);
							else
								Settings.Add(key, part.Substring(index + 1));
						}
					}
				}
			}
		}

		public string Name { get; }
		public string Provider { get; }
		public DataSource Source { get; }
		public IDictionary<string, string> Settings { get; }

		public ReportDataModel CreateModel(IDataSet dataSet) => new ReportDataModel(dataSet, this);
	}
}
