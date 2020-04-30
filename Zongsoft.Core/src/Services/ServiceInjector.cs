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
	internal static class ServiceInjector
	{
		private static readonly TypeInfo ObjectType = typeof(object).GetTypeInfo();
		private static readonly ConcurrentDictionary<Type, PropertyInjectionDescriptor[]> _descriptors = new ConcurrentDictionary<Type, PropertyInjectionDescriptor[]>();

		public static object Inject(object target, IServiceProvider provider)
		{
			if(target == null || provider == null)
				return target;

			var descriptors = _descriptors.GetOrAdd(target.GetType(), (t, p) =>
			{
				var type = t.GetType().GetTypeInfo();
				var list = new List<PropertyInjectionDescriptor>();

				do
				{
					foreach(var property in type.DeclaredProperties)
					{
						if(!property.CanWrite || property.SetMethod.IsStatic || !property.SetMethod.IsPublic)
							continue;

						var attribute = property.GetCustomAttribute<ServiceDependencyAttribute>();

						if(attribute != null)
							list.Add(new PropertyInjectionDescriptor(property, attribute, p));
					}

					foreach(var field in type.DeclaredFields)
					{
						if(field.IsInitOnly || field.IsStatic || !field.IsPublic)
							continue;

						var attribute = field.GetCustomAttribute<ServiceDependencyAttribute>();

						if(attribute != null)
							list.Add(new PropertyInjectionDescriptor(field, attribute, p));
					}

					type = type.BaseType.GetTypeInfo();
				} while(type != ObjectType);

				return list.Count > 0 ? list.ToArray() : null;
			}, provider);

			if(descriptors == null || descriptors.Length == 0)
				return target;

			for(int i = 0; i < descriptors.Length; i++)
			{
				descriptors[i].SetValue(ref target);
			}

			return target;
		}

		private class PropertyInjectionDescriptor
		{
			public MemberInfo Member;
			public Type ServiceType;
			public bool IsRequired;
			public IServiceProvider Provider;

			public PropertyInjectionDescriptor(MemberInfo member, ServiceDependencyAttribute attribute, IServiceProvider provider)
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
	}
}
