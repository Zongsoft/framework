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

namespace Zongsoft.Services;

/// <summary>
/// 表示特定类型的服务提供程序。
/// </summary>
/// <typeparam name="T">特定的服务类型。</typeparam>
public interface IServiceProvider<out T> where T : class
{
	/// <summary>获取指定名称的服务。</summary>
	/// <param name="name">指定的要获取的服务名称。</param>
	/// <returns>返回指定名称的服务，如果为空(<c>null</c>)则表示指定名称的服务不存在。</returns>
	/// <remarks>对于实现者的要求：当指定名称的服务不存在时，确保返回值为空(<c>null</c>)而不要抛出异常。</remarks>
	T GetService(string name = null);
}
