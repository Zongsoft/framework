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

namespace Zongsoft.Services;

/// <summary>
/// 提供一种特定于类型的通用匹配方法，某些同类型的类通过实现此接口对其进行更进一步的匹配。
/// </summary>
public interface IMatchable
{
	/// <summary>指示当前对象是否匹配指定参数的条件约束。</summary>
	/// <param name="argument">指定是否匹配逻辑的参数。</param>
	/// <returns>如果当前对象符合<paramref name="argument"/>参数的匹配规则，则为真(<c>True</c>)；否则为假(<c>False</c>)。</returns>
	bool Match(object argument);
}

/// <summary>
/// 提供一种特定于类型的通用匹配方法，某些同类型的类通过实现此接口对其进行更进一步的匹配。
/// </summary>
public interface IMatchable<in T>
{
	/// <summary>指示当前对象是否匹配指定参数的条件约束。</summary>
	/// <param name="argument">指定是否匹配逻辑的参数。</param>
	/// <returns>如果当前对象符合<paramref name="argument"/>参数的匹配规则，则为真(<c>True</c>)；否则为假(<c>False</c>)。</returns>
	bool Match(T argument);
}
