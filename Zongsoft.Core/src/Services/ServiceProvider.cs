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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services;

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
	public ServiceProvider(IServiceCollection services, ServiceProviderOptions options = null)
	{
		if(services == null)
			throw new ArgumentNullException(nameof(services));

		for(int i = 0; i < services.Count; i++)
		{
			var descriptor = services[i];

			if(descriptor.ImplementationType != null && descriptor.ImplementationType.IsInjectable())
			{
				var method = GetFacotryMethod.MakeGenericMethod(descriptor.ImplementationType);
				var factory = (Func<IServiceProvider, object>)method.Invoke(null, Array.Empty<object>());
				services[i] = new ServiceDescriptor(descriptor.ServiceType, factory, descriptor.Lifetime);
			}
		}

		services.AddSingleton(this);
		services.AddSingleton<IServiceProvider>(this);

		_provider = services.BuildServiceProvider(options);
	}

	internal ServiceProvider(string name, IServiceProvider provider)
	{
		_name = name ?? throw new ArgumentNullException(nameof(name));
		_provider = provider ?? throw new ArgumentNullException(nameof(provider));
	}
	#endregion

	#region 公共属性
	public string Name => _name;
	#endregion

	#region 公共方法
	public object GetService(Type serviceType)
	{
		//如果是默认服务容器则可直接解析返回
		if(string.IsNullOrEmpty(_name))
			return _provider.GetService(serviceType);

		//获取多个服务，即解析服务集
		if(serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
		{
			//解析出要获取的服务类型
			var elementType = serviceType.GenericTypeArguments[0];

			//获取模块化服务类型
			if(ModularServiceUtility.TryGetModularServiceType(_name, elementType, out var modularType))
			{
				var result = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
				var modulars = _provider.GetServices(modularType).Cast<IModularService>();

				if(modulars != null)
				{
					foreach(var modular in modulars)
						result.Add(modular.GetValue(_provider));
				}

				var services = (IEnumerable)this.GetServiceBacktrack(serviceType);
				if(services != null)
				{
					var exister = new ServiceExister(result);

					foreach(var service in services)
					{
						//不要添加同类型的服务到结果集
						if(!exister.Exists(service.GetType()))
							result.Add(service);
					}
				}

				return result;
			}

			return this.GetServiceBacktrack(serviceType) ?? Common.ArrayExtension.Empty(elementType);
		}

		//获取单个模块化类型
		//注意：如果当前模块服务容器没有找到指定的服务则再从应用服务容器中获取指定类型的服务
		if(ModularServiceUtility.TryGetModularServiceType(_name, serviceType, out var contractType))
			return ((IModularService)_provider.GetService(contractType)).GetValue(_provider) ?? this.GetServiceBacktrack(serviceType);

		return this.GetServiceBacktrack(serviceType);
	}

	private object GetServiceBacktrack(Type serviceType)
	{
		var result = _provider.GetService(serviceType);
		if(result != null)
			return result;

		return object.ReferenceEquals(this, ApplicationContext.Current.Services) ||
			object.ReferenceEquals(_provider, ApplicationContext.Current.Services) ?
			result : ApplicationContext.Current.Services.GetService(serviceType);
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

	#region 嵌套结构
	private readonly struct ServiceExister
	{
		private readonly IList _services;
		private readonly HashSet<Type> _types;

		public ServiceExister(IList services)
		{
			this._services = services;

			if(services.Count > 7)
			{
				_types = new HashSet<Type>(services.Count);

				foreach(var service in services)
					_types.Add(service.GetType());
			}
		}

		public bool Exists(Type type)
		{
			if(_types is not null)
				return _types.Contains(type);

			for(int i = 0; i < _services.Count; i++)
			{
				if(_services[i].GetType() == type)
					return true;
			}

			return false;
		}
	}
	#endregion
}
