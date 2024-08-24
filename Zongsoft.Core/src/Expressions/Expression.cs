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
using System.Collections.Generic;

namespace Zongsoft.Expressions
{
	public static class Expression
	{
		#region 公共属性
		public static IExpressionParser Default { get; set; }
		#endregion

		#region 公共方法
		public static IExpression Parse(ReadOnlySpan<char> text) => Default?.Parse(text);
		public static bool TryParse(ReadOnlySpan<char> text, out IExpression result)
		{
			var parser = Default;
			if(parser != null)
				return parser.TryParse(text, out result);

			result = null;
			return false;
		}

		public static object Evaluate(ReadOnlySpan<char> text, IDictionary<string, object> variables = null) => Parse(text)?.Evaluate(variables);
		#endregion
	}
}