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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Common;

/// <summary>提供变量来源的顺序查询与字典视图扩展。</summary>
public static class VariablesExtension
{
	#region 公共方法
	/// <summary>按来源顺序查询全局变量，不允许回退。</summary>
	/// <param name="variables">按优先级排列的变量来源，忽略空来源。</param>
	/// <param name="name">变量名称，不裁剪、转换或拆分。</param>
	/// <param name="value">首个成功查询的原始值，可以为空；失败时为空。</param>
	/// <returns>是否找到变量。</returns>
	public static bool TryGetValue(this IEnumerable<IVariables> variables, string name, out object value) => TryGetValue(variables, null, name, false, out value);

	/// <summary>按来源顺序查询全局变量，并明确是否允许来源自身的默认值。</summary>
	/// <param name="variables">按优先级排列的变量来源，须可重复枚举且查询期间保持稳定；忽略空来源。</param>
	/// <param name="name">变量名称，不裁剪、转换或拆分。</param>
	/// <param name="fallback">是否在全部普通查询失败后，按来源顺序查询各来源的默认值。</param>
	/// <param name="value">首个成功查询的原始值，可以为空；失败时为空。</param>
	/// <returns>是否找到变量或允许的默认值。</returns>
	public static bool TryGetValue(this IEnumerable<IVariables> variables, string name, bool fallback, out object value) => TryGetValue(variables, null, name, fallback, out value);

	/// <summary>按来源顺序查询指定命名空间，不允许回退。</summary>
	/// <param name="variables">按优先级排列的变量来源，忽略空来源。</param>
	/// <param name="namespace">命名空间，<see langword="null"/> 或空字符串表示全局。</param>
	/// <param name="name">变量名称，不裁剪、转换或拆分。</param>
	/// <param name="value">首个成功查询的原始值，可以为空；失败时为空。</param>
	/// <returns>是否找到变量。</returns>
	public static bool TryGetValue(this IEnumerable<IVariables> variables, string @namespace, string name, out object value) => TryGetValue(variables, @namespace, name, false, out value);

	/// <summary>按命名空间层级及来源顺序查询变量，并明确是否允许回退。</summary>
	/// <param name="variables">按优先级排列的变量来源，须可重复枚举且查询期间保持稳定；忽略空来源。</param>
	/// <param name="namespace">命名空间，<see langword="null"/> 或空字符串表示全局；层级以点号分隔。</param>
	/// <param name="name">变量名称，不裁剪、转换或拆分。</param>
	/// <param name="fallback">是否逐级回退到父命名空间、全局命名空间，最后允许来源自身的默认值。</param>
	/// <param name="value">首个成功查询的原始值，可以为空；失败时为空。</param>
	/// <returns>是否找到变量或允许的默认值。</returns>
	/// <exception cref="ArgumentNullException">变量来源集合或名称为空。</exception>
	/// <remarks>
	/// 	<para>同一命名空间中按来源顺序执行不回退的查询，全部未找到才进入父级。全局普通查询也全部失败后，才按来源顺序执行全局回退查询。</para>
	/// 	<para>任何查询首次返回 <see langword="true"/> 即结束，包括值为 <see langword="null"/>、空字符串、<see langword="false"/> 或零的情况。不缓存、转换或展开结果，来源异常直接传播。</para>
	/// </remarks>
	public static bool TryGetValue(this IEnumerable<IVariables> variables, string @namespace, string name, bool fallback, out object value)
	{
		ArgumentNullException.ThrowIfNull(variables);
		ArgumentNullException.ThrowIfNull(name);

		while(true)
		{
			if(TryGetValueCore(variables, @namespace, name, false, out value))
				return true;

			if(!fallback)
				return false;

			if(string.IsNullOrEmpty(@namespace))
				break;

			@namespace = GetParentNamespace(@namespace);
		}

		return TryGetValueCore(variables, null, name, true, out value);
	}

	/// <summary>将指定字典转换为共享其当前内容的变量视图。</summary>
	/// <typeparam name="TDictionary">实现非泛型 <see cref="IDictionary"/> 接口的字典类型。</typeparam>
	/// <param name="dictionary">作为变量来源的字典，不能为 <see langword="null"/>。</param>
	/// <param name="reuse">是否通过弱引用缓存复用包装视图，默认为 <see langword="false"/>，每次新建包装，不访问或修改缓存；为 <see langword="true"/> 时按字典实例复用视图。</param>
	/// <returns>由 <see cref="Variables.Wrap{TDictionary}(TDictionary, bool)"/> 复用或新建的变量视图；字典本身实现 <see cref="IVariables"/> 时始终返回原对象。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="dictionary"/> 为 <see langword="null"/>。</exception>
	/// <remarks>查询规则和生命周期均遵循 <see cref="Variables.Wrap{TDictionary}(TDictionary, bool)"/>。</remarks>
	public static IVariables ToVariables<TDictionary>(this TDictionary dictionary, bool reuse = false) where TDictionary : IDictionary => Variables.Wrap(dictionary, reuse);

	/// <summary>将指定字典转换为共享其当前内容的变量视图。</summary>
	/// <param name="dictionary">作为变量来源的字典，不能为 <see langword="null"/>。</param>
	/// <param name="reuse">是否按字典引用身份复用包装视图，默认为 <see langword="false"/>，每次新建包装且不访问缓存。</param>
	/// <returns>复用或新建的变量视图；字典本身实现 <see cref="IVariables"/> 时始终返回原对象。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="dictionary"/> 为 <see langword="null"/>。</exception>
	/// <remarks>查询规则和生命周期均遵循 <see cref="Variables.Wrap{TDictionary}(TDictionary, bool)"/>。</remarks>
	public static IVariables ToVariables(this IDictionary<string, object> dictionary, bool reuse = false) => Variables.Wrap(dictionary, reuse);

	/// <summary>将指定字典转换为共享其当前内容的变量视图。</summary>
	/// <param name="dictionary">作为变量来源的字典，不能为 <see langword="null"/>。</param>
	/// <param name="reuse">是否按字典引用身份复用包装视图，默认为 <see langword="false"/>，每次新建包装且不访问缓存。</param>
	/// <returns>复用或新建的变量视图；字典本身实现 <see cref="IVariables"/> 时始终返回原对象。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="dictionary"/> 为 <see langword="null"/>。</exception>
	/// <remarks>查询规则和生命周期均遵循 <see cref="Variables.Wrap{TDictionary}(TDictionary, bool)"/>。</remarks>
	public static IVariables ToVariables(this IDictionary<object, object> dictionary, bool reuse = false) => Variables.Wrap(dictionary, reuse);
	#endregion

	#region 内部方法
	internal static bool TryGetValueCore(IVariables variables, string @namespace, string name, bool fallback, out object value)
	{
		while(true)
		{
			if(variables.TryGetValue(@namespace, name, out value))
				return true;

			if(!fallback || string.IsNullOrEmpty(@namespace))
				return false;

			@namespace = GetParentNamespace(@namespace);
		}
	}
	#endregion

	#region 私有方法
	private static bool TryGetValueCore(IEnumerable<IVariables> variables, string @namespace, string name, bool fallback, out object value)
	{
		foreach(var provider in variables)
		{
			if(provider != null && provider.TryGetValue(@namespace, name, fallback, out value))
				return true;
		}

		value = null;
		return false;
	}

	private static string GetParentNamespace(string @namespace)
	{
		var index = @namespace.LastIndexOf('.');
		return index < 0 ? null : @namespace[..index];
	}
	#endregion
}
