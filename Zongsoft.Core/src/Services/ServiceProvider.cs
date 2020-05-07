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
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services
{
	[DefaultMember(nameof(Descriptors))]
	public class ServiceProvider : IServiceProvider, IDisposable
	{
		#region 静态字段
		private static readonly MethodInfo GetFacotryMethod = typeof(ServiceProvider).GetMethod(nameof(GetFactory), 1, (BindingFlags.Static | BindingFlags.NonPublic), null, new[] { typeof(IServiceProvider) }, null);
		#endregion

		#region 成员字段
		private readonly string _name;
		private readonly IServiceProvider _provider;
		private readonly ServiceProviderOptions _options;
		private readonly IList<IServiceProvider> _providers;
		private readonly IServiceCollection _descriptors;
		private ServiceScopeFactoryProxy _scopeFactory;
		#endregion

		#region 构造函数
		public ServiceProvider(IServiceCollection descriptors, ServiceProviderOptions options = null)
		{
			_options = options;
			_providers = new List<IServiceProvider>();
			_descriptors = descriptors ?? new ServiceCollection();
		}

		public ServiceProvider(IServiceProvider provider, ServiceProviderOptions options = null)
		{
			_options = options;
			_providers = new List<IServiceProvider>() { provider ?? throw new ArgumentNullException(nameof(provider)) };
			_descriptors = new ServiceCollection();
		}
		#endregion

		#region 公共属性
		public IServiceCollection Descriptors
		{
			get => _descriptors;
		}
		#endregion

		#region 公共方法
		public object GetService(Type serviceType)
		{
			if(_descriptors.Count > 0)
			{
				lock(_providers)
				{
					if(_descriptors.Count > 0)
					{
						for(int i = 0; i < _descriptors.Count; i++)
						{
							var descriptor = _descriptors[i];

							if(descriptor.ImplementationType != null)
							{
								descriptor = Adaptive(descriptor.ServiceType, descriptor.ImplementationType, descriptor.Lifetime);

								if(descriptor != null)
									_descriptors[i] = descriptor;
							}
						}

						_providers.Add(
							_options == null ?
							_descriptors.BuildServiceProvider() :
							_descriptors.BuildServiceProvider(_options));

						_scopeFactory = null;
						_descriptors.Clear();
					}
				}
			}

			if(string.IsNullOrEmpty(_name))
				return _provider.GetService(serviceType);

			if(ServiceModular.TryGet(_name, serviceType, out var type))
				return _provider.GetService(type);

			if(serviceType == typeof(IServiceProvider))
				return this;

			if(serviceType == typeof(IServiceScopeFactory))
				Debug.WriteLine("ServiceScopeFactory");

			if(serviceType == typeof(IServiceScopeFactory) && _providers.Count > 1)
			{
				if(_scopeFactory == null)
				{
					lock(this)
					{
						if(_scopeFactory == null)
							_scopeFactory = new ServiceScopeFactoryProxy(_providers.Select(p => p.GetRequiredService<IServiceScopeFactory>()));
					}
				}

				return _scopeFactory;
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

		#region 处置方法
		public void Dispose()
		{
			while(_providers.Count > 0)
			{
				if(_providers[0] is IDisposable disposable)
				{
					disposable.Dispose();
					_providers.Remove((IServiceProvider)disposable);
				}
			}
		}
		#endregion

		#region 私有方法
		private ServiceDescriptor Adaptive(Type serviceType, Type implementationType, ServiceLifetime lifetime)
		{
			if(implementationType.ContainsGenericParameters)
				return null;

			var method = GetFacotryMethod.MakeGenericMethod(implementationType);
			var factory = (Func<IServiceProvider, object>)method.Invoke(null, new object[] { this });
			return new ServiceDescriptor(serviceType, factory, lifetime);
		}

		private static Func<IServiceProvider, T> GetFactory<T>(IServiceProvider provider)
		{
			if(provider.IsInjectable(typeof(T)))
				return new Func<IServiceProvider, T>(_ => (T)provider.Inject(ActivatorUtilities.CreateInstance<T>(provider)));
			else
				return new Func<IServiceProvider, T>(_ => ActivatorUtilities.CreateInstance<T>(provider));
		}
		#endregion

		private class ServiceScopeFactoryProxy : IServiceScopeFactory
		{
			private readonly IEnumerable<IServiceScopeFactory> _factories;

			public ServiceScopeFactoryProxy(IEnumerable<IServiceScopeFactory> factories)
			{
				_factories = factories;
			}

			public IServiceScope CreateScope()
			{
				return new ServiceScopeProxy(_factories.Select(p => p.CreateScope()));
			}
		}

		private class ServiceScopeProxy : IServiceScope
		{
			private readonly IEnumerable<IServiceScope> _scopes;

			public ServiceScopeProxy(IEnumerable<IServiceScope> scopes)
			{
				_scopes = scopes;
			}

			public IServiceProvider ServiceProvider => new ServiceProviderProxy(_scopes.Select(p => p.ServiceProvider));

			public void Dispose()
			{
				foreach(var scope in _scopes)
					scope.Dispose();
			}
		}

		private class ServiceProviderProxy : IServiceProvider, IDisposable
		{
			private readonly IEnumerable<IServiceProvider> _providers;

			public ServiceProviderProxy(IEnumerable<IServiceProvider> providers)
			{
				_providers = providers;
			}

			public object GetService(Type serviceType)
			{
				foreach(var provider in _providers)
				{
					var instance = provider.GetService(serviceType);

					if(instance != null)
						return instance;
				}

				return null;
			}

			public void Dispose()
			{
				foreach(var provider in _providers)
				{
					if(provider is IDisposable disposable)
						disposable.Dispose();
				}
			}
		}
	}
}
