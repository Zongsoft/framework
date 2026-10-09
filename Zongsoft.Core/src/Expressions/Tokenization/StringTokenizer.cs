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
using System.IO;
using System.Text;

namespace Zongsoft.Expressions.Tokenization;

public class StringTokenizer : ITokenizer
{
	public TokenResult Tokenize(TextReader reader)
	{
		var quote = reader.Peek();

		if(quote is not '\'' and not '"')
			return TokenResult.Fail(0);

		reader.Read();

		int value;
		var text = new StringBuilder();

		while((value = reader.Read()) >= 0)
		{
			if(value == quote)
				return new TokenResult(0, new Token(TokenType.Constant, text.ToString()));

			if(value is '\r' or '\n')
				throw new SyntaxException(Properties.Resources.StringTokenizer_NewLine_Message);

			if(value == '\\')
			{
				value = reader.Read();

				if(value < 0 || !TryEscape((char)value, out var character))
					throw new SyntaxException(Properties.Resources.Template_InvalidEscape_Message);

				text.Append(character);
			}
			else
			{
				text.Append((char)value);
			}
		}

		throw new SyntaxException(string.Format(Properties.Resources.StringTokenizer_ClosingQuoteRequired_Message, (char)quote));
	}

	internal static bool TryEscape(char character, out char result)
	{
		switch(character)
		{
			case '"':
			case '$':
			case '\'':
			case '\\':
				result = character;
				return true;
			case 's':
				result = ' ';
				return true;
			case 't':
				result = '\t';
				return true;
			case 'n':
				result = '\n';
				return true;
			case 'r':
				result = '\r';
				return true;
		}

		result = '\0';
		return false;
	}
}
