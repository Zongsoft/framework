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

		public static object Inject(this IServiceProvider provider, object target)
		{
			if(provider == null)
				throw new ArgumentNullException(nameof(provider));

			if(target == null)
				return target;

			if(IsInjectable(target.GetType(), out var descriptors))
			{
				for(int i = 0; i < descriptors.Length; i++)
					descriptors[i].SetValue(provider, ref target);
			}

			return target;
		}

		public static bool IsInjectable(this Type type)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			return IsInjectable(type, out _);
		}
		#endregion

		#region 私有方法
		private static bool IsInjectable(Type type, out MemberInjectionDescriptor[] descriptors)
		{
			descriptors = _descriptors.GetOrAdd(type, t =>
			{
				var type = t.GetTypeInfo();
				var list = new List<MemberInjectionDescriptor>();

				do
				{
					foreach(var property in type.DeclaredProperties)
					{
						if(!property.CanWrite || property.SetMethod.IsStatic || !property.SetMethod.IsPublic)
							continue;

						var attribute = property.GetCustomAttribute<ServiceDependencyAttribute>();

						if(attribute != null)
							list.Add(new MemberInjectionDescriptor(property, attribute));
						else if(property.IsDefined(typeof(Configuration.Options.OptionsAttribute), true))
							list.Add(new MemberInjectionDescriptor(property));
					}

					foreach(var field in type.DeclaredFields)
					{
						if(field.IsInitOnly || field.IsStatic || !field.IsPublic)
							continue;

						var attribute = field.GetCustomAttribute<ServiceDependencyAttribute>();

						if(attribute != null)
							list.Add(new MemberInjectionDescriptor(field, attribute));
						else if(field.IsDefined(typeof(Configuration.Options.OptionsAttribute), true))
							list.Add(new MemberInjectionDescriptor(field));
					}

					type = type.BaseType?.GetTypeInfo();
				} while(type != null && type != ObjectType);

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

				var serviceType = attribute.ServiceType ?? member switch
				{
					PropertyInfo property => property.PropertyType,
					FieldInfo field => field.FieldType,
					_ => throw new ArgumentException("Invalid member type."),
				};

				if(IsServiceAccessor(serviceType, out var accessorType))
				{
					_valueFactory = (provider, target) => ActivatorUtilities.CreateInstance(provider, accessorType, new object[] { GetApplicationModule(member.ReflectedType) });
					return;
				}

				if(attribute.IsRequired)
				{
					if(string.IsNullOrEmpty(attribute.Provider))
						_valueFactory = (provider, target) =>
						{
							if(ServiceModular.TryGetContract(target, serviceType, out var contract))
								return provider.GetRequiredService(contract);
							else
								return provider.GetRequiredService(serviceType);
						};
					else
						_valueFactory = (provider, target) =>
						{
							if(ServiceModular.TryGetContract(attribute.Provider, serviceType, out var contract))
								return provider.GetRequiredService(contract);
							else
								return provider.GetRequiredService(serviceType);
						};
				}
				else
				{
					if(string.IsNullOrEmpty(attribute.Provider))
						_valueFactory = (provider, target) =>
						{
							if(ServiceModular.TryGetContract(target, serviceType, out var contract))
								return provider.GetService(contract);
							else
								return provider.GetService(serviceType);
						};
					else
						_valueFactory = (provider, target) =>
						{
							if(ServiceModular.TryGetContract(attribute.Provider, serviceType, out var contract))
								return provider.GetService(contract);
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
					_valueFactory = (provider, target) => provider.GetService(ServiceModular.TryGetContract(target, memberType, out var contract) ? contract : memberType);
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
				accessorType = null;

				if(type.IsValueType)
					return false;

				if(type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IServiceAccessor<>))
				{
					accessorType = typeof(ServiceAccessor<>).MakeGenericType(type.GenericTypeArguments[0]);
					return true;
				}

				foreach(var contract in type.GetTypeInfo().ImplementedInterfaces)
				{
					if(contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IServiceAccessor<>))
					{
						accessorType = type;
						return true;
					}
				}

				return false;
			}

			private static IApplicationModule GetApplicationModule(Type type)
			{
				var moduleName = ServiceModular.GetModuleName(type);

				if(!string.IsNullOrEmpty(moduleName) && ApplicationContext.Current.Modules.TryGet(moduleName, out var module))
					return module;

				return ApplicationContext.Current;
			}
		}
		#endregion
	}
}
