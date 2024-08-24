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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Expressions
{
	/// <summary>
	/// 提供表达式解析功能的接口。
	/// </summary>
	public interface IExpressionParser
	{
		/// <summary>解析表达式。</summary>
		/// <param name="text">指定的表达式文本。</param>
		/// <returns>返回解析成功的表达式。</returns>
		IExpression Parse(ReadOnlySpan<char> text);

		/// <summary>尝试解析表达式。</summary>
		/// <param name="text">指定的表达式文本。</param>
		/// <param name="result">输出参数，表示解析成功后的表达式。</param>
		/// <returns>返回一个布尔值，如果为真(<c>True</c>)则表示解析成功，否则表示失败。</returns>
		bool TryParse(ReadOnlySpan<char> text, out IExpression result);
	}
}