﻿/*
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
using System.Collections.Generic;

namespace Zongsoft.Data;

/// <summary>
/// 表示数据实体或业务模型的接口。
/// </summary>
public interface IModel
{
	/// <summary>重置变更属性，并获取重置之前的值。</summary>
	/// <param name="name">指定要重置的属性名，如果为空(<c>null</c>)或空字符串则返回假(<c>False</c>)。</param>
	/// <param name="value">输出参数，如果重置成功则返回重置之前的值否则输出空(<c>null</c>)。</param>
	/// <returns>如果指定要重置的属性名是存在并且发生过变更则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	bool Reset(string name, out object value);

	/// <summary>重置变更属性。</summary>
	/// <param name="names">指定要重置的属性名数组，如果为空或空数组则表示重置所有。</param>
	void Reset(params string[] names);

	/// <summary>获取实体中变更的属性数。</summary>
	/// <returns>返回变更的属性数。</returns>
	int GetCount();

	/// <summary>判断指定的属性或任意属性是否被变更过。</summary>
	/// <param name="names">指定要判断的属性名数组，如果为空或空数组则表示判断任意属性。</param>
	/// <returns>
	/// 	<para>如果指定的<paramref name="names"/>参数有值，当只有参数中指定的属性发生过更改则返回真(<c>True</c>)，否则返回假(<c>False</c>)；</para>
	/// 	<para>如果指定的<paramref name="names"/>参数为空(<c>null</c>)或空数组，当实体中任意属性发生过更改则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</para>
	/// </returns>
	bool HasChanges(params string[] names);

	/// <summary>获取实体中发生过变更的属性集。</summary>
	/// <returns>如果实体没有任何属性发生过变更，则返回空(<c>null</c>)，否则返回被变更过的属性键值对。</returns>
	IDictionary<string, object> GetChanges();

	/// <summary>尝试获取指定名称的属性变更后的值。</summary>
	/// <param name="name">指定要获取的属性名。</param>
	/// <param name="value">输出参数，指定属性名对应的变更后的值。</param>
	/// <returns>如果指定名称的属性是存在的并且发生过变更，则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	/// <remarks>
	/// 	<para>注意：即使指定名称的属性是存在的，但只要其值未被更改过，也会返回假(<c>False</c>)。</para>
	/// </remarks>
	bool TryGetValue(string name, out object value);

	/// <summary>尝试设置指定名称的属性值。</summary>
	/// <param name="name">指定要设置的属性名。</param>
	/// <param name="value">指定要设置的属性值。</param>
	/// <returns>如果指定名称的属性是存在的并且可写入，则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	bool TrySetValue(string name, object value);
}
