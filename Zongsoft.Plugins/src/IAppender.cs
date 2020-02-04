/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 提供追加器的功能接口。
	/// </summary>
	/// <remarks>
	///		<para>由构建器可选性的实现该接口，实现该接口的构建器支持将子级构建器创建的目标对象追加到当前目标对象的特定集合中。</para>
	/// </remarks>
	public interface IAppender
	{
		/// <summary>
		/// 将指定的子级目标对象追加到当前目标对象的特定集合中。
		/// </summary>
		/// <param name="context">追加器的上下文对象。</param>
		/// <returns>如果追加成功则返回真(true)，否则返回假(false)。</returns>
		bool Append(AppenderContext context);
	}
}
