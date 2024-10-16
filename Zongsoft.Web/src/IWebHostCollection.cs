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
 * Copyright (C) 2015-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Web;

/// <summary>
/// 表示 <see cref="IWebHost"/> 宿主集合的接口。
/// </summary>
public interface IWebHostCollection : ICollection<IWebHost>
{
	/// <summary>获取默认宿主。</summary>
	IWebHost Default { get; }

	/// <summary>获取指定名称的宿主。</summary>
	/// <param name="name">指定的宿主名称。</param>
	/// <returns>返回指定名称的宿主。</returns>
	IWebHost this[string name] { get; }

	/// <summary>尝试获取指定名称的宿主。</summary>
	/// <param name="name">指定要获取的宿主名称。</param>
	/// <param name="value">输出参数，表示指定名称的宿主。</param>
	/// <returns>如果指定名称的宿主获取成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	bool TryGetValue(string name, out IWebHost value);
}
