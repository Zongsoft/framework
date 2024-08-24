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
 * This file is part of Zongsoft.Externals.Scriban library.
 *
 * The Zongsoft.Externals.Scriban is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Scriban is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Scriban library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Expressions;

using Scriban;
using Scriban.Parsing;

namespace Zongsoft.Externals.Scriban;

public class ScribanExpressionParser : IExpressionParser
{
	#region 静态常量
	private static readonly ParserOptions _parserOptions = new()
	{
		LiquidFunctionsToScriban = true,
	};

	private static readonly LexerOptions _lexerOptions = new()
	{
		Lang = ScriptLang.Default,
		Mode = ScriptMode.ScriptOnly,
	};
	#endregion

	#region 解析方法
	public IExpression Parse(ReadOnlySpan<char> text) => new ScribanExpression(Template.Parse(text.ToString(), null, _parserOptions, _lexerOptions));
	public bool TryParse(ReadOnlySpan<char> text, out IExpression result)
	{
		try
		{
			result = new ScribanExpression(Template.Parse(text.ToString(), null, _parserOptions, _lexerOptions));
			return true;
		}
		catch
		{
			result = null;
			return false;
		}
	}
	#endregion
}
