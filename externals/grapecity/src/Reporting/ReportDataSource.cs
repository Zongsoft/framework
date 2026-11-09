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

using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Reporting;

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
			this.Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			foreach(var part in parts)
			{
				if(part != null && part.Length > 0)
				{
					var index = part.IndexOf('=');

					if(index > 0)
					{
						var key = part.Substring(0, index);

						if(index >= part.Length - 1)
							this.Settings.Add(key, null);
						else
							this.Settings.Add(key, part.Substring(index + 1));
					}
				}
			}
		}
	}

	public string Name { get; }
	public string Provider { get; }
	public DataSource Source { get; }
	public IDictionary<string, string> Settings { get; }

	public ReportDataModel CreateModel(IDataSet dataSet) => new(dataSet, this);
}
