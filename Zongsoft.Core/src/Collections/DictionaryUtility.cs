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
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Collections;

public static class DictionaryUtility
{
	#region 公共方法
	public static bool TryAdd(object target, object key, object value)
	{
		var added = false;
		var adders = GetMethods(target, nameof(IDictionary<object, object>.Add));

		foreach(var adder in adders)
		{
			if(Common.Convert.TryConvertValue(key, adder.KeyType, out var keyed) &&
			   Common.Convert.TryConvertValue(value, adder.ValueType, out var valued))
			{
				adder.Invoker?.DynamicInvoke(keyed, valued);
				added = true;
			}
		}

		if(!added && target is IDictionary dictionary)
		{
			dictionary.Add(key, value);
			return true;
		}

		return added;
	}

	public static bool TryRemove(object target, object key)
	{
		var removed = false;
		var removers = GetMethods(target, nameof(IDictionary<object, object>.Remove));

		foreach(var remover in removers)
		{
			if(Common.Convert.TryConvertValue(key, remover.KeyType, out var parameter))
			{
				remover.Invoker?.DynamicInvoke(parameter);
				removed = true;
			}
		}

		if(!removed && target is IDictionary dictionary)
		{
			dictionary.Remove(key);
			return true;
		}

		return removed;
	}
	#endregion

	#region 私有方法
	private static IEnumerable<DictionaryToken> GetMethods(object target, string name)
	{
		if(target == null)
			yield break;

		foreach(var contract in target.GetType().GetInterfaces().Where(p => p.IsGenericType && p.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
		{
			var mapping = target.GetType().GetInterfaceMap(contract);

			for(int i = 0; i < mapping.InterfaceMethods.Length; i++)
			{
				if(mapping.InterfaceMethods[i].Name == name)
					yield return new(target, mapping.TargetMethods[i]);
			}
		}
	}
	#endregion

	#region 嵌套结构
	private readonly struct DictionaryToken
	{
		public DictionaryToken(object target, MethodInfo method)
		{
			var parameters = method.GetParameters();

			switch(method.Name)
			{
				case nameof(IDictionary<object, object>.Add):
					this.KeyType = parameters[0].ParameterType;
					this.ValueType = parameters[1].ParameterType;
					this.Invoker = method.CreateDelegate(typeof(Action<,>).MakeGenericType(this.KeyType, this.ValueType), target);
					break;
				case nameof(IDictionary<object, object>.Remove):
					this.KeyType = parameters[0].ParameterType;
					this.Invoker = method.CreateDelegate(typeof(Func<,>).MakeGenericType(this.KeyType, method.ReturnType), target);
					break;
			}
		}

		public readonly Delegate Invoker;
		public readonly Type KeyType;
		public readonly Type ValueType;
	}
	#endregion
}
