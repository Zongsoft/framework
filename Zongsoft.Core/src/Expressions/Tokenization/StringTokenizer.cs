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
using System.Buffers;

namespace Zongsoft.Expressions.Tokenization;

public class StringTokenizer : ITokenizer
{
	public TokenResult Tokenize(ReadOnlySpan<char> text)
	{
		if(text.IsEmpty || text[0] is not '\'' and not '"')
			return TokenResult.Fail();

		var quote = text[0];
		var escapes = 0;

		for(int i = 1; i < text.Length; i++)
		{
			var value = text[i];

			if(value == quote)
			{
				var content = text.Slice(1, i - 1);
				var literal = escapes == 0 ? content.ToString() : Unescape(content, content.Length - escapes);
				return new TokenResult(i + 1, new Token(TokenType.Constant, literal));
			}

			if(value is '\r' or '\n')
				throw new SyntaxException(Properties.Resources.StringTokenizer_NewLine_Message);

			if(value == '\\')
			{
				if(++i == text.Length || !TryEscape(text[i], out _))
					throw new SyntaxException(Properties.Resources.Template_InvalidEscape_Message);

				escapes++;
			}
		}

		throw new SyntaxException(string.Format(Properties.Resources.StringTokenizer_ClosingQuoteRequired_Message, quote));
	}

	private static string Unescape(ReadOnlySpan<char> text, int length)
	{
		//短字符串使用栈缓冲区；长字符串租用数组，只为最终词素值创建字符串
		char[] rented = null;
		var buffer = length <= 256 ? stackalloc char[length] : (rented = ArrayPool<char>.Shared.Rent(length));

		try
		{
			var position = 0;

			for(int i = 0; i < text.Length; i++)
			{
				if(text[i] == '\\')
					TryEscape(text[++i], out buffer[position++]);
				else
					buffer[position++] = text[i];
			}

			return new string(buffer[..length]);
		}
		finally
		{
			if(rented != null)
				ArrayPool<char>.Shared.Return(rented, true);
		}
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
