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
using System.ComponentModel;

namespace Zongsoft.Configuration
{
	public static class ConfigurationUtility
	{
		public static string GetConfigurationPath(string key)
		{
			return string.IsNullOrEmpty(key) ? string.Empty : key.Trim('/').Replace('/', ':');
		}

		public static Type GetImplementedContract(Type actual, params Type[] expectedTypes)
		{
			if(actual.IsGenericType && expectedTypes.Contains(actual.GetGenericTypeDefinition()))
				return actual;

			var contracts = actual.GetTypeInfo().ImplementedInterfaces;

			foreach(var contract in contracts)
			{
				if(contract.IsGenericType && expectedTypes.Contains(contract.GetGenericTypeDefinition()))
					return contract;
			}

			return null;
		}

		internal static TypeConverter GetConverter(MemberInfo member)
		{
			/*
			 * 注意：TypeDescriptor.GetConverter(...) 方法对于 MemberInfo(PropertyInfo/FieldInfo) 并不适用。
			 */

			var attribute = member.GetCustomAttribute<TypeConverterAttribute>(true);

			if(attribute != null && !string.IsNullOrEmpty(attribute.ConverterTypeName))
			{
				var type = Type.GetType(attribute.ConverterTypeName, assemblyName =>
				{
					var assemblies = AppDomain.CurrentDomain.GetAssemblies();

					for(int i = 0; i < assemblies.Length; i++)
					{
						var name = assemblies[i].GetName();

						if(string.Equals(assemblyName.FullName, name.FullName) && assemblyName.Version <= name.Version)
							return assemblies[i];
					}

					return null;
				}, null);

				return Activator.CreateInstance(type) as TypeConverter;
			}

			return null;
		}

		internal static ConfigurationAttribute GetConfigurationAttribute(this Type type)
		{
			if(type == null || type == typeof(object))
				return null;

			var attribute = type.GetCustomAttribute<ConfigurationAttribute>(true);

			if(attribute != null)
				return attribute;

			foreach(var contract in type.GetTypeInfo().ImplementedInterfaces)
			{
				attribute = contract.GetCustomAttribute<ConfigurationAttribute>(true);

				if(attribute != null)
					return attribute;
			}

			return GetConfigurationAttribute(type.BaseType);
		}
	}
}
