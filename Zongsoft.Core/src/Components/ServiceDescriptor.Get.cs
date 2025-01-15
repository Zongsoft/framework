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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Components;

partial class ServiceDescriptor
{
	#region 私有字段
	private static readonly ConcurrentDictionary<Type, ServiceDescriptor> _descriptors = new();
	#endregion

	#region 公共方法
	public static ServiceDescriptor Get<T>() => Get(typeof(T));
	public static ServiceDescriptor Get(Type type)
	{
		if(type == null)
			throw new ArgumentNullException(nameof(type));

		//优先获取类型显式声明的服务描述器提供程序
		var provider = type.GetCustomAttribute<ServiceDescriptorAttribute>(true)?.GetProvider();

		//如果未显式声明描述器提供程序或其不支持该类型则尝试获取其他提供程序
		if(provider == null || !provider.Support(type))
		{
			//获取当前进程中的所有服务描述器提供程序
			var providers = GetProviders();

			for(int i = 0; i < providers.Length; i++)
			{
				if(providers[i] != provider && providers[i].Support(type))
					provider = providers[i];
			}
		}

		return _descriptors.GetOrAdd(type, type => provider?.GetDescriptor(type));
	}
	#endregion

	#region 私有方法
	private static readonly object _locker = new();
	private static IServiceDescriptorProvider[] _providers;
	private static IServiceDescriptorProvider[] GetProviders()
	{
		if(_providers != null)
			return _providers;

		lock(_locker)
		{
			if(_providers == null)
			{
				var providers = new List<IServiceDescriptorProvider>();
				var assemblies = AppDomain.CurrentDomain.GetAssemblies();

				for(int i = 0; i < assemblies.Length; i++)
				{
					if(assemblies[i].IsDynamic)
						continue;

					foreach(var type in GetTypes(assemblies[i]))
					{
						if(type.IsPublic && type.IsClass && !type.IsAbstract && typeof(IServiceDescriptorProvider).IsAssignableFrom(type))
							providers.Add((IServiceDescriptorProvider)Activator.CreateInstance(type));
					}
				}

				_providers = providers.ToArray();
			}
		}

		return _providers;

		static IEnumerable<TypeInfo> GetTypes(Assembly assembly)
		{
			if(assembly == null)
				return [];

			try
			{
				return assembly.DefinedTypes;
			}
			catch { return []; }
		}
	}
	#endregion
}
