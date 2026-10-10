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

/// <summary>提供字典的变量视图扩展。</summary>
public static class VariablesExtension
{
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
}
