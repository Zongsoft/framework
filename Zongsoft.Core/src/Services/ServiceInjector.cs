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

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services
{
	public static class ServiceInjector
	{
		#region 私有变量
		private static readonly TypeInfo ObjectType = typeof(object).GetTypeInfo();
		private static readonly ConcurrentDictionary<Type, MemberInjectionDescriptor[]> _descriptors = new ConcurrentDictionary<Type, MemberInjectionDescriptor[]>();
		#endregion

		#region 公共方法
		public static object Inject(this IServiceProvider provider, object target)
		{
			if(target == null || provider == null)
				return target;

			if(IsInjectable(provider, target.GetType(), out var descriptors))
			{
				for(int i = 0; i < descriptors.Length; i++)
					descriptors[i].SetValue(ref target);
			}

			return target;
		}

		public static bool IsInjectable(this IServiceProvider provider, Type type)
		{
			return IsInjectable(provider, type, out _);
		}
		#endregion

		#region 私有方法
		private static bool IsInjectable(this IServiceProvider provider, Type type, out MemberInjectionDescriptor[] descriptors)
		{
			descriptors = _descriptors.GetOrAdd(type, (t, p) =>
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
							list.Add(new MemberInjectionDescriptor(property, attribute, p));
					}

					foreach(var field in type.DeclaredFields)
					{
						if(field.IsInitOnly || field.IsStatic || !field.IsPublic)
							continue;

						var attribute = field.GetCustomAttribute<ServiceDependencyAttribute>();

						if(attribute != null)
							list.Add(new MemberInjectionDescriptor(field, attribute, p));
					}

					type = type.BaseType.GetTypeInfo();
				} while(type != ObjectType);

				return list.Count > 0 ? list.ToArray() : null;
			}, provider);

			return descriptors != null && descriptors.Length > 0;
		}
		#endregion

		#region 嵌套子类
		private class MemberInjectionDescriptor
		{
			public readonly MemberInfo Member;
			public readonly Type ServiceType;
			public readonly bool IsRequired;
			public readonly IServiceProvider Provider;

			public MemberInjectionDescriptor(MemberInfo member, ServiceDependencyAttribute attribute, IServiceProvider provider)
			{
				this.Member = member;
				this.IsRequired = attribute.IsRequired;

				if(string.IsNullOrEmpty(attribute.Provider))
					this.Provider = provider;
				else
					this.Provider = ApplicationContext.Current.Modules.Get(attribute.Provider).Services;

				this.ServiceType = attribute.ServiceType ??
					member switch
					{
						PropertyInfo property => property.PropertyType,
						FieldInfo field => field.FieldType,
						_ => null,
					};
			}

			public void SetValue(ref object target)
			{
				Zongsoft.Reflection.Reflector.SetValue
				(
					this.Member,
					ref target,
					this.IsRequired ?
						this.Provider.GetRequiredService(this.ServiceType) :
						this.Provider.GetService(this.ServiceType)
				);
			}
		}
		#endregion
	}
}
