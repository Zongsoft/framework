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
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services
{
	internal class ServiceProvider : IServiceProvider, IDisposable
	{
		#region 静态字段
		private static readonly MethodInfo GetFacotryMethod = typeof(ServiceProvider).GetMethod(nameof(GetFactory), 1, (BindingFlags.Static | BindingFlags.NonPublic), null, Array.Empty<Type>(), null);
		#endregion

		#region 成员字段
		private readonly string _name;
		private readonly IServiceProvider _provider;
		#endregion

		#region 构造函数
		public ServiceProvider(IServiceCollection descriptors, ServiceProviderOptions options = null)
		{
			if(descriptors == null)
				throw new ArgumentNullException(nameof(descriptors));

			for(int i = 0; i < descriptors.Count; i++)
			{
				var descriptor = descriptors[i];

				if(descriptor.ImplementationType != null && descriptor.ImplementationType.IsInjectable())
				{
					var method = GetFacotryMethod.MakeGenericMethod(descriptor.ImplementationType);
					var factory = (Func<IServiceProvider, object>)method.Invoke(null, Array.Empty<object>());
					descriptors[i] = new ServiceDescriptor(descriptor.ServiceType, factory, descriptor.Lifetime);
				}
			}

			descriptors.AddSingleton(this);
			descriptors.AddSingleton<IServiceProvider>(this);

			_provider = descriptors.BuildServiceProvider(options);
		}

		internal ServiceProvider(string name, IServiceProvider provider)
		{
			_name = name ?? throw new ArgumentNullException(nameof(name));
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		}
		#endregion

		#region 公共属性
		public string Name { get => _name; }
		#endregion

		#region 公共方法
		public object GetService(Type serviceType)
		{
			if(string.IsNullOrEmpty(_name))
				return _provider.GetService(serviceType);

			if(serviceType.IsAbstract && ServiceModular.TryGetContract(_name, serviceType, out var contract))
				return _provider.GetService(contract);

			return _provider.GetService(serviceType);
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			if(_provider is IDisposable disposable)
				disposable.Dispose();
		}
		#endregion

		#region 私有方法
		private static Func<IServiceProvider, T> GetFactory<T>()
		{
			return new Func<IServiceProvider, T>(provider => (T)provider.Inject(ActivatorUtilities.CreateInstance<T>(provider)));
		}
		#endregion
	}
}
