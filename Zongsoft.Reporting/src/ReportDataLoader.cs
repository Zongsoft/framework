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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Data;
using Zongsoft.Services;

namespace Zongsoft.Reporting;

[Service(typeof(IReportDataLoader))]
public class ReportDataLoader : IReportDataLoader
{
	private IServiceProvider _serviceProvider;

	#region 构造函数
	//public ReportDataLoader(IDataAccess dataAccess)
	//{
	//	this.DataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
	//}

	//public ReportDataLoader(IDataService dataService)
	//{
	//	this.DataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
	//}

	public ReportDataLoader(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}
	#endregion

	#region 公共属性
	public IDataAccess DataAccess { get; }
	public IDataService DataService { get; }
	#endregion

	#region 数据加载
	public ValueTask<object> LoadAsync(IReport report, IReportDataModel model, CancellationToken cancellation = default)
	{
		if(_serviceProvider.Resolve(model.Name) is not IDataService service)
			return ValueTask.FromResult<object>(null);

		return ValueTask.FromResult<object>(service.SelectAsync(null, model.Schema, Paging.Page(1), cancellation));
	}
	#endregion
}
