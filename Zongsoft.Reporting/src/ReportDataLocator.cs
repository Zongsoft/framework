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

using Zongsoft.Data;
using Zongsoft.Services;

namespace Zongsoft.Reporting;

[Service(typeof(IReportDataLocator))]
public class ReportDataLocator : IReportDataLocator
{
	private readonly IServiceProvider _serviceProvider;

	public ReportDataLocator(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

	public IReportDataLoader Locate(IReportDataSource source)
	{
		//var dataAccess = string.IsNullOrEmpty(source.Name) || source.Name == "_" ? _serviceProvider.GetDataAccess() : _serviceProvider.GetDataAccess(source.Name);
		return null;
		//return new ReportDataLoader(dataAccess);
	}
}
