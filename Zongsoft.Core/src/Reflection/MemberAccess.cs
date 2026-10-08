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
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
 * associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
 * NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Zongsoft.Reflection;

/// <summary>为模板导航提供公共实例成员读取及索引参数绑定。</summary>
internal static class MemberAccess
{
	public static object GetValue(object target, string name)
	{
		var type = target.GetType();
		var members = new List<MemberInfo>();
		members.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(field => string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase)));
		members.AddRange(GetProperties(type).Where(property => property.GetIndexParameters().Length == 0 && string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)));

		if(members.Count == 0)
			throw new MissingMemberException(string.Format(Properties.Resources.Reflection_MemberNotFound_Message, name, type));

		if(members.Count != 1)
			throw new AmbiguousMatchException(Properties.Resources.Template_AmbiguousMember_Message);

		return Reflector.GetValue(members[0], ref target);
	}

	public static object GetValue(object target, object[] arguments)
	{
		if(target is Array array)
		{
			if(arguments.Length != array.Rank)
				throw new ArgumentException(Properties.Resources.Template_IndexParameters_Message);

			var indices = new int[arguments.Length];

			for(int i = 0; i < indices.Length; i++)
				indices[i] = Common.Convert.ConvertValue<int>(arguments[i]);

			return array.GetValue(indices);
		}

		var type = target.GetType();
		var candidates = GetProperties(type).Where(property =>
			property.GetIndexParameters().Length > 0 && property.GetIndexParameters().Length == arguments.Length).ToArray();

		if(candidates.Length == 0)
			throw new ArgumentException(Properties.Resources.Template_IndexParameters_Message);

		var selected = Select(candidates, arguments);
		var parameters = selected.GetIndexParameters();
		var values = new object[arguments.Length];

		for(int i = 0; i < values.Length; i++)
			values[i] = Common.Convert.ConvertValue(arguments[i], parameters[i].ParameterType);

		CheckKey(target, selected, values);
		return Reflector.GetValue(selected, ref target, values);
	}

	private static PropertyInfo Select(PropertyInfo[] candidates, object[] arguments)
	{
		PropertyInfo selected = null;

		foreach(var candidate in candidates)
		{
			if(candidates.All(other => other == candidate || IsBetter(candidate, other, arguments)))
			{
				selected = candidate;
				break;
			}
		}

		return selected ?? throw new AmbiguousMatchException(Properties.Resources.Template_AmbiguousMember_Message);
	}

	private static bool IsBetter(PropertyInfo candidate, PropertyInfo other, object[] arguments)
	{
		var first = candidate.GetIndexParameters();
		var second = other.GetIndexParameters();
		var better = false;

		for(int i = 0; i < arguments.Length; i++)
		{
			var firstType = first[i].ParameterType;
			var secondType = second[i].ParameterType;
			var firstRank = Rank(firstType, arguments[i]);
			var secondRank = Rank(secondType, arguments[i]);

			if(firstRank > secondRank)
				return false;

			if(firstRank < secondRank)
				better = true;
			else if(firstType != secondType)
			{
				if(firstRank == 1 && secondType.IsAssignableFrom(firstType))
					better = true;
				else
					return false;
			}
		}

		return better;
	}

	private static int Rank(Type type, object value)
	{
		if(value != null && type == value.GetType())
			return 0;

		if(value == null ? !type.IsValueType || Nullable.GetUnderlyingType(type) != null :
			type.IsInstanceOfType(value) || Nullable.GetUnderlyingType(type) == value.GetType())
			return 1;

		return 2;
	}

	private static IEnumerable<PropertyInfo> GetProperties(Type type)
	{
		var properties = new Dictionary<MethodInfo, PropertyInfo>();

		foreach(var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if(property.GetMethod is { IsPublic: true, IsStatic: false } getter)
				properties.TryAdd(getter, property);
		}

		var contracts = type.GetInterfaces();
		var genericDictionary = contracts.Any(contract => contract.IsGenericType &&
			(contract.GetGenericTypeDefinition() == typeof(IDictionary<,>) || contract.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));
		var genericList = contracts.Any(contract => contract.IsGenericType &&
			(contract.GetGenericTypeDefinition() == typeof(IList<>) || contract.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)));

		foreach(var contract in contracts)
		{
			if(!(contract.IsGenericType ? contract.GetGenericTypeDefinition() : contract).IsVisible ||
				genericDictionary && contract == typeof(IDictionary) || genericList && contract == typeof(IList))
				continue;

			foreach(var property in contract.GetProperties())
			{
				if(property.GetMethod is not { IsPublic: true, IsStatic: false } getter)
					continue;

				var method = GetTargetMethod(type, getter);
				if(properties.ContainsKey(method))
					continue;

				//类的公共读取契约优先于相同签名的接口适配（例如列表的协变返回值）。
				if(properties.Values.Any(existing => !existing.DeclaringType.IsInterface && existing.Name == property.Name &&
					existing.GetIndexParameters().Select(parameter => parameter.ParameterType)
						.SequenceEqual(property.GetIndexParameters().Select(parameter => parameter.ParameterType))))
					continue;

				properties.Add(method, property);
			}
		}

		return properties.Values;
	}

	private static MethodInfo GetTargetMethod(Type type, MethodInfo method)
	{
		if(!method.DeclaringType.IsInterface)
			return method;

		var mapping = type.GetInterfaceMap(method.DeclaringType);
		var index = Array.IndexOf(mapping.InterfaceMethods, method);
		return index < 0 ? method : mapping.TargetMethods[index];
	}

	private static void CheckKey(object target, PropertyInfo indexer, object[] values)
	{
		if(values.Length != 1)
			return;

		var type = target.GetType();
		var getter = GetTargetMethod(type, indexer.GetMethod);

		foreach(var contract in type.GetInterfaces())
		{
			MethodInfo contains;
			PropertyInfo item;

			if(contract == typeof(IDictionary))
			{
				item = contract.GetProperty("Item");
				contains = contract.GetMethod(nameof(IDictionary.Contains));
			}
			else if(contract.IsGenericType && (contract.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
				contract.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)))
			{
				item = contract.GetProperty("Item");
				contains = contract.GetMethod("ContainsKey");
			}
			else
				continue;

			if(GetTargetMethod(type, item.GetMethod) != getter)
				continue;

			try
			{
				if(!(bool)contains.Invoke(target, values))
					throw new KeyNotFoundException(Properties.Resources.Template_KeyNotFound_Message);
			}
			catch(TargetInvocationException exception) when(exception.InnerException != null)
			{
				ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
			}

			return;
		}
	}
}
