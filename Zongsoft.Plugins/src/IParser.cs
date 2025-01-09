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

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 表示解析器的接口。
	/// </summary>
	public interface IParser
	{
		/// <summary>获取解析器目标对象的类型。</summary>
		/// <param name="context">解析器上下文对象。</param>
		/// <returns>返回的目标类型。</returns>
		/// <remarks>该方法尽量以不构建目标类型的方式去获取目标类型。</remarks>
		Type GetValueType(Parsers.ParserContext context);

		/// <summary>解析表达式，返回目标对象。</summary>
		/// <param name="context">解析器上下文对象。</param>
		/// <returns>返回解析后的目标对象。</returns>
		object Parse(Parsers.ParserContext context);
	}
}
