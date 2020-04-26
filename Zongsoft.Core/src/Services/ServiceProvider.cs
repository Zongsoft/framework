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
	public class ServiceProvider : IServiceProvider
	{
		#region 成员字段
		private readonly ServiceProviderOptions _options;
		private readonly IServiceCollection _descriptors;
		private readonly IList<IServiceProvider> _providers;
		#endregion

		#region 构造函数
		public ServiceProvider(IServiceCollection descriptors, ServiceProviderOptions options = null)
		{
			_options = options;
			_descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
			_providers = new List<IServiceProvider>();
		}
		#endregion

		#region 公共属性
		public IServiceCollection Services
		{
			get => _descriptors;
		}
		#endregion

		#region 公共方法
		public object GetService(Type serviceType)
		{
			if(_descriptors.Count > 0)
			{
				_providers.Add(
					_options == null ?
					_descriptors.BuildServiceProvider() :
					_descriptors.BuildServiceProvider(_options));

				_descriptors.Clear();
			}

			for(int i = _providers.Count - 1; i >= 0; i--)
			{
				var service = _providers[i].GetService(serviceType);

				if(service != null)
					return service;
			}

			return null;
		}
		#endregion
	}
}
