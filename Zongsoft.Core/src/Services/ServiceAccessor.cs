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

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services
{
	[Obsolete($"Use the '{nameof(ServiceDependencyAttribute.ServiceName)}' property of '{nameof(ServiceDependencyAttribute)}' annotation class instead.")]
	public class ServiceAccessor<T> : IServiceAccessor<T> where T : class
	{
		#region 构造函数
		public ServiceAccessor(T value) => this.Value = value;
		public ServiceAccessor(IApplicationModule module, string name)
		{
			static T GetValue(string name, IServiceProvider serviceProvider)
			{
				if(serviceProvider == null)
					return default;

				var provider = serviceProvider.GetService<IServiceProvider<T>>();

				if(provider == null)
					return serviceProvider.GetService<T>();

				if(string.IsNullOrEmpty(name))
					return provider.GetService(string.Empty) ?? serviceProvider.GetService<T>();
				else
					return provider.GetService(name) ?? provider.GetService(string.Empty) ?? serviceProvider.GetService<T>();
			}

			//注意：以下代码的处理机制必须遵循服务注入注解类中ServiceName属性的规范！
			if(module == null || module is IApplicationContext)
				this.Value = GetValue(name ?? string.Empty, ApplicationContext.Current.Services);
			else
				this.Value = GetValue(name ?? module.Name, module.Services);
		}
		#endregion

		#region 公共属性
		public T Value { get; }
		#endregion
	}
}
