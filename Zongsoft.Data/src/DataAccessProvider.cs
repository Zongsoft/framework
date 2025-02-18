﻿/*
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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Data
{
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
			var services = ApplicationContext.Current.Services;

			if(!string.IsNullOrEmpty(name) && ApplicationContext.Current.Modules.TryGetValue(name, out var module))
				services = module.Services;

			ServiceInjector.Inject(services, result);
			return result;
		}
		#endregion
	}
}