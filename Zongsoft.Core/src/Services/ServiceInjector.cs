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
using System.Collections.Generic;
using System.Collections.Concurrent;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services
{
	public static class ServiceInjector
	{
		#region 私有变量
		private static readonly TypeInfo ObjectType = typeof(object).GetTypeInfo();
		private static readonly ConcurrentDictionary<Type, MemberInjectionDescriptor[]> _descriptors = new ConcurrentDictionary<Type, MemberInjectionDescriptor[]>();
		private static readonly ConcurrentDictionary<Type, PropertyInfo> _options = new ConcurrentDictionary<Type, PropertyInfo>();
		#endregion

		#region 公共方法
		public static object Inject(this IServiceProvider provider, Type instanceType, params object[] parameters)
		{
			if(provider == null)
				throw new ArgumentNullException(nameof(provider));

			if(instanceType == null)
				throw new ArgumentNullException(nameof(instanceType));

			//如果是静态类型则返回空
			if(instanceType.IsAbstract && instanceType.IsSealed)
				return null;

			var instance = instanceType.IsAbstract ?
				Data.Model.Build(instanceType) :
				ActivatorUtilities.CreateInstance(provider, instanceType, parameters ?? Array.Empty<Type>());

			if(IsInjectable(instanceType, out var descriptors))
			{
				for(int i = 0; i < descriptors.Length; i++)
					descriptors[i].SetValue(provider, ref instance);
			}

			return instance;
		}

		public static object Inject(this IServiceProvider provider, object instance)
		{
			if(provider == null)
				throw new ArgumentNullException(nameof(provider));

			if(instance == null)
				return instance;

			if(IsInjectable(instance.GetType(), out var descriptors))
			{
				for(int i = 0; i < descriptors.Length; i++)
					descriptors[i].SetValue(provider, ref instance);
			}

			return instance;
		}

		public static bool IsInjectable(this Type type) => type != null && IsInjectable(type, out _);
		#endregion

		#region 私有方法
		private static bool IsInjectable(Type type, out MemberInjectionDescriptor[] descriptors)
		{
			descriptors = _descriptors.GetOrAdd(type, t =>
			{
				var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public);
				var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
				var list = new List<MemberInjectionDescriptor>(properties.Length + fields.Length);

				foreach(var property in properties)
				{
					if(!property.CanWrite || property.SetMethod.IsStatic || !property.SetMethod.IsPublic)
						continue;

					var attribute = property.GetCustomAttribute<ServiceDependencyAttribute>();

					if(attribute != null)
						list.Add(new MemberInjectionDescriptor(property, attribute));
					else if(property.IsDefined(typeof(Configuration.Options.OptionsAttribute), true))
						list.Add(new MemberInjectionDescriptor(property));
				}

				foreach(var field in fields)
				{
					if(field.IsInitOnly || field.IsStatic || !field.IsPublic)
						continue;

					var attribute = field.GetCustomAttribute<ServiceDependencyAttribute>();

					if(attribute != null)
						list.Add(new MemberInjectionDescriptor(field, attribute));
					else if(field.IsDefined(typeof(Configuration.Options.OptionsAttribute), true))
						list.Add(new MemberInjectionDescriptor(field));
				}

				return list.Count > 0 ? list.ToArray() : null;
			});

			return descriptors != null && descriptors.Length > 0;
		}
		#endregion

		#region 嵌套子类
		private class MemberInjectionDescriptor
		{
			private readonly MemberInfo _member;
			private readonly Func<IServiceProvider, object, object> _valueFactory;

			public MemberInjectionDescriptor(MemberInfo member, ServiceDependencyAttribute attribute)
			{
				_member = member;
				var memberType = GetMemberType(member);

				//如果待注入的成员类型是服务访问器接口，则优先以服务访问器方式进行注入
				if(IsServiceAccessor(memberType, out var accessorType))
				{
					_valueFactory = (provider, target) =>
						Activator.CreateInstance(accessorType, new object[]
						{
							GetApplicationModule(target?.GetType() ?? member.ReflectedType),
							attribute.GetServiceName(member.ReflectedType)
						});

					return;
				}

				if(attribute.ServiceName != null)
				{
					var invoker = ServiceProviderInvoker.GetInvoker(attribute.ServiceType ?? memberType, attribute.IsRequired);
					var serviceName = attribute.GetServiceName(member.ReflectedType);
					_valueFactory = (provider, target) => invoker.Invoke(provider, serviceName);
					return;
				}

				//根据待注入的成员类型以及声明的服务注入注解来确定最终的注入服务类型
				var serviceType = GetServiceType(member, attribute.ServiceType);

				if(attribute.IsRequired)
				{
					if(string.IsNullOrEmpty(attribute.Provider))
						_valueFactory = (provider, target) =>
						{
							if(ModularServicerUtility.TryGetModularServiceType(target, serviceType, out var modularType))
								return ((IModularService)provider.GetRequiredService(modularType)).GetValue(provider) ?? throw new InvalidOperationException($"No service for type '{serviceType}' has been registered.");
							else
								return provider.GetRequiredService(serviceType);
						};
					else if(attribute.IsApplicationProvider)
						_valueFactory = (provider, target) => ApplicationContext.Current.Services.GetRequiredService(serviceType);
					else
						_valueFactory = (provider, target) =>
						{
							if(ModularServicerUtility.TryGetModularServiceType(attribute.Provider, serviceType, out var modularType))
								return ((IModularService)provider.GetRequiredService(modularType)).GetValue(provider) ?? throw new InvalidOperationException(@"No service for type '{serviceType}' has been registered.");
							else
								return provider.GetRequiredService(serviceType);
						};
				}
				else
				{
					if(string.IsNullOrEmpty(attribute.Provider))
						_valueFactory = (provider, target) =>
						{
							if(ModularServicerUtility.TryGetModularServiceType(target, serviceType, out var modularType))
								return ((IModularService)provider.GetService(modularType))?.GetValue(provider);
							else
								return provider.GetService(serviceType);
						};
					else if(attribute.IsApplicationProvider)
						_valueFactory = (provider, target) => ApplicationContext.Current.Services.GetService(serviceType);
					else
						_valueFactory = (provider, target) =>
						{
							if(ModularServicerUtility.TryGetModularServiceType(attribute.Provider, serviceType, out var modularType))
								return ((IModularService)provider.GetService(modularType))?.GetValue(provider);
							else
								return provider.GetService(serviceType);
						};
				}
			}

			public MemberInjectionDescriptor(MemberInfo member)
			{
				_member = member;

				var memberType = member switch
				{
					PropertyInfo property => property.PropertyType,
					FieldInfo field => field.FieldType,
					_ => throw new ArgumentException("Invalid member type."),
				};

				if(!memberType.IsGenericType || memberType.GetGenericTypeDefinition() != typeof(IOptions<>))
				{
					var optionType = typeof(IOptions<>).MakeGenericType(memberType);

					_valueFactory = (provider, target) =>
					{
						var property = _options.GetOrAdd(optionType, t => t.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance));
						var instance = provider.GetService(optionType);
						return instance == null ? null : Reflection.Reflector.GetValue(property, ref instance);
					};
				}
				else
				{
					_valueFactory = (provider, target) =>
					{
						if(ModularServicerUtility.TryGetModularServiceType(target, memberType, out var modularType))
							return ((IModularService)provider.GetService(modularType)).GetValue(provider);
						else
							return provider.GetService(memberType);
					};
				}
			}

			public void SetValue(IServiceProvider provider, ref object target)
			{
				Reflection.Reflector.SetValue
				(
					_member,
					ref target,
					_valueFactory(provider, target)
				);
			}

			private static bool IsServiceAccessor(Type type, out Type accessorType)
			{
				if(type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IServiceAccessor<>))
				{
					accessorType = typeof(ServiceAccessor<>).MakeGenericType(type.GenericTypeArguments[0]);
					return true;
				}

				accessorType = null;
				return false;
			}

			private static Type GetMemberType(MemberInfo member) => member switch
			{
				PropertyInfo property => property.PropertyType,
				FieldInfo field => field.FieldType,
				_ => throw new InvalidOperationException($"The '{member.ReflectedType.FullName}.{member.Name}' member is an unsupported injection member."),
			};

			private static Type GetServiceType(MemberInfo member, Type serviceType)
			{
				//如果未指定注入的服务类型，则将待注入的成员类型作为服务类型
				if(serviceType == null)
					return GetMemberType(member);

				//如果指定注入的服务类型是泛型原型，则返回以该原型泛化的成员类型的泛型
				if(serviceType.IsGenericTypeDefinition)
					return serviceType.MakeGenericType(GetMemberType(member));

				return serviceType;
			}

			private static IApplicationModule GetApplicationModule(Type type)
			{
				var moduleName = ModularServicerUtility.GetModuleName(type);

				if(!string.IsNullOrEmpty(moduleName) && ApplicationContext.Current.Modules.TryGet(moduleName, out var module))
					return module;

				return ApplicationContext.Current;
			}
		}

		private static class ServiceProviderInvoker<T> where T : class
		{
			public static object GetService(IServiceProvider serviceProvider, string name) =>
				serviceProvider.GetService<IServiceProvider<T>>()?.GetService(name);

			public static object GetRequiredService(IServiceProvider serviceProvider, string name) =>
				serviceProvider.GetRequiredService<IServiceProvider<T>>().GetService(name) ?? throw new InvalidOperationException($"No service named '{name}' was found in the service provider of type '{typeof(IServiceProvider<T>).FullName}'.");
		}

		private static class ServiceProviderInvoker
		{
			public static Func<IServiceProvider, string, object> GetInvoker(Type type, bool required) =>
				required ?
				typeof(ServiceProviderInvoker<>).MakeGenericType(type)
				.GetMethod(nameof(ServiceProviderInvoker<object>.GetRequiredService), BindingFlags.Static | BindingFlags.Public)
				.CreateDelegate<Func<IServiceProvider, string, object>>() :
				typeof(ServiceProviderInvoker<>).MakeGenericType(type)
				.GetMethod(nameof(ServiceProviderInvoker<object>.GetService), BindingFlags.Static | BindingFlags.Public)
				.CreateDelegate<Func<IServiceProvider, string, object>>();
		}
		#endregion
	}
}
