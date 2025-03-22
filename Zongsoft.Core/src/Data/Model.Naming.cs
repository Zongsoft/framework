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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Data;

partial class Model
{
	/// <summary>
	/// 提供数据模型名字映射的类。
	/// </summary>
	public static class Naming
	{
		#region 成员字段
		private static readonly ConcurrentDictionary<Type, string> _mapping = new();
		#endregion

		#region 公共属性
		public static int Count => _mapping.Count;
		#endregion

		#region 公共方法
		public static bool Contains<TModel>() => _mapping.ContainsKey(typeof(TModel));
		public static bool Contains(Type modelType) => modelType != null && _mapping.ContainsKey(modelType);

		public static bool Unmap<TModel>() => Unmap(typeof(TModel));
		public static bool Unmap(Type modelType) => modelType != null && _mapping.TryRemove(modelType, out _);

		public static bool Unmap<TModel>(out string name) => Unmap(typeof(TModel), out name);
		public static bool Unmap(Type modelType, out string name)
		{
			if(modelType == null)
			{
				name = null;
				return false;
			}	

			return _mapping.TryRemove(modelType, out name);
		}

		public static void Map<TModel>(string name = null) => Map(typeof(TModel), name);
		public static void Map(Type modelType, string name = null)
		{
			if(modelType == null)
				throw new ArgumentNullException(nameof(modelType));

			//对动态模型类进行特殊处理
			if(modelType.IsClass && modelType.Assembly.IsDynamic && modelType.BaseType.IsAbstract)
				modelType = modelType.BaseType;

			_mapping[modelType] = string.IsNullOrEmpty(name) ? GetName(modelType) : name;
		}

		public static string Get<TModel>() => Get(typeof(TModel));
		public static string Get(Type modelType)
		{
			if(modelType == null)
				throw new ArgumentNullException(nameof(modelType));

			//对动态模型类进行特殊处理
			if(modelType.IsClass && modelType.Assembly.IsDynamic && modelType.BaseType.IsAbstract)
				modelType = modelType.BaseType;

			return _mapping.GetOrAdd(modelType, GetName);
		}
		#endregion

		#region 私有方法
		private static string GetName(Type type)
		{
			//如果该类型应用了数据访问注解，则返回注解中声明的名字
			if(TryGetNameFromAttribute(type, out var name))
				return name;

			name = type.Name;

			if(type.IsGenericType)
			{
				//如果类型的声明类型为空，很可能它是一个匿名类（匿名类必定是一个泛型类）
				if(type.DeclaringType == null)
				{
					foreach(var attribute in type.CustomAttributes)
					{
						//如果指定的类型是一个匿名类，则返回空（因为匿名类名没有意义）
						if(attribute.AttributeType == typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute))
							return null;
					}
				}

				//去掉泛型类名中关于泛型参数数量的后缀部分
				name = type.Name[..type.Name.IndexOf('`')];
			}

			//处理接口命名的规范
			if(type.IsInterface && name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
				name = name[1..];
			//处理抽象类命名的规范
			else if(type.IsAbstract && name.Length > 4 && name.EndsWith("Base"))
				name = name[..^4];

			var module = Services.ApplicationModuleAttribute.Find(type)?.Name;
			return string.IsNullOrEmpty(module) ? name : $"{module}.{name}";
		}

		private static bool TryGetNameFromAttribute(Type type, out string name)
		{
			name = ((ModelAttribute)Attribute.GetCustomAttribute(type, typeof(ModelAttribute), true))?.Name;
			return name != null && name.Length > 0;
		}
		#endregion

		#region 遍历方法
		public static IEnumerable<KeyValuePair<Type, string>> Enumerable() => _mapping;
		#endregion
	}
}
