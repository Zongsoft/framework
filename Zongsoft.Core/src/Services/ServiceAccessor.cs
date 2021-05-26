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

namespace Zongsoft.Services
{
	public class ServiceAccessor<T> : IServiceAccessor<T> where T : class
	{
		#region 构造函数
		public ServiceAccessor(T value) => this.Value = value;

		public ServiceAccessor(IApplicationModule module)
		{
			static T GetValue(string name, IServiceProvider serviceProvider)
			{
				if(serviceProvider == null)
					return default;

				var provider = serviceProvider.Resolve<IServiceProvider<T>>();

				if(provider == null)
					return serviceProvider.Resolve<T>();

				if(string.IsNullOrEmpty(name))
					return provider.GetService(string.Empty) ?? serviceProvider.Resolve<T>();
				else
					return provider.GetService(name) ?? provider.GetService(string.Empty) ?? serviceProvider.Resolve<T>();
			}

			if(module == null)
				module = ApplicationContext.Current;

			if(module != null)
			{
				var value = GetValue(module.Name, module.Services);

				if(value == null && module is not IApplicationContext)
					value = GetValue(null, ApplicationContext.Current.Services);

				this.Value = value;
			}
		}
		#endregion

		#region 公共属性
		public T Value { get; }
		#endregion
	}
}
