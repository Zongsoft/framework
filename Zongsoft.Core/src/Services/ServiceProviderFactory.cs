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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services
{
	public sealed class ServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
	{
		#region 私有变量
		private readonly ServiceProviderOptions _options;
		#endregion

		#region 构造函数
		public ServiceProviderFactory(ServiceProviderOptions options = null)
		{
			_options = options;
		}
		#endregion

		#region 公共方法
		/// <inheritdoc />
		public IServiceCollection CreateBuilder(IServiceCollection services)
		{
			return services;
		}

		/// <inheritdoc />
		public System.IServiceProvider CreateServiceProvider(IServiceCollection services)
		{
			return new ServiceProvider(services, _options);
		}
		#endregion
	}
}
