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

public static class CollectionUtility
{
	#region 公共方法
	public static bool TryAdd(object target, object value)
	{
		var added = false;
		var adders = GetMethods(target, nameof(ICollection<object>.Add));

		foreach(var adder in adders)
		{
			if(Common.Convert.TryConvertValue(value, adder.ParameterType, out var parameter))
			{
				adder.Invoker?.DynamicInvoke(parameter);
				added = true;
			}
		}

		if(!added && target is IList list)
			added = list.Add(value) >= 0;

		return added;
	}

	public static bool TryRemove(object target, object value)
	{
		var removed = false;
		var removers = GetMethods(target, nameof(ICollection<object>.Remove));

		foreach(var remover in removers)
		{
			if(Common.Convert.TryConvertValue(value, remover.ParameterType, out var parameter))
			{
				remover.Invoker?.DynamicInvoke(target, parameter);
				removed = true;
			}
		}

		if(!removed && target is IList list)
		{
			list.Remove(value);
			return true;
		}

		return removed;
	}
	#endregion

	#region 私有方法
	private static IEnumerable<MethodToken> GetMethods(object target, string name)
	{
		if(target == null)
			yield break;

		var contracts = target.GetType().GetInterfaces();

		foreach(var contract in target.GetType().GetInterfaces().Where(p => p.IsGenericType && p.GetGenericTypeDefinition() == typeof(ICollection<>)))
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
	private readonly struct MethodToken
	{
		public MethodToken(object target, MethodInfo method)
		{
			this.ParameterType = method.GetParameters()[0].ParameterType;
			this.Invoker = method.CreateDelegate(typeof(Action<>).MakeGenericType(this.ParameterType), target);
		}

		public readonly Delegate Invoker;
		public readonly Type ParameterType;
	}
	#endregion
}
