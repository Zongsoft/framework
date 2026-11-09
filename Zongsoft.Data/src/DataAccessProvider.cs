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

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Data;

[Service<
	IServiceProvider<IDataAccess>,
	IServiceProvider<DataAccessBase>,
	IServiceProvider<DataAccess>>(Members = nameof(Instance))]
public class DataAccessProvider : DataAccessProviderBase<DataAccess>
{
	#region 单例字段
	public static readonly DataAccessProvider Instance = new();
	#endregion

	#region 重写方法
	protected override DataAccess CreateAccessor(string name, IDataAccessOptions options)
	{
		var result = new DataAccess(name, options);
		if(ApplicationContext.Current == null)
			return result;

		var services = ApplicationContext.Current.Services;

		if(!string.IsNullOrEmpty(name) && ApplicationContext.Current.Modules.TryGetValue(name, out var module))
			services = module.Services;

		ServiceInjector.Inject(services, result);
		return result;
	}
	#endregion
}