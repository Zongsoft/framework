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

namespace Zongsoft.Expressions.Tokenization;

public class IdentifierTokenizer : ITokenizer
{
	public TokenResult Tokenize(ReadOnlySpan<char> text)
	{
		var length = GetLength(text);
		return length == 0 ? TokenResult.Fail() : new TokenResult(length, new Token(TokenType.Identifier, text[..length].ToString()));
	}

	internal static bool IsIdentifier(ReadOnlySpan<char> text) => !text.IsEmpty && GetLength(text) == text.Length;

	private static int GetLength(ReadOnlySpan<char> text)
	{
		if(text.IsEmpty || !IsBeginning(text[0]))
			return 0;

		var length = 1;

		while(length < text.Length && (IsBeginning(text[length]) || char.IsAsciiDigit(text[length])))
			length++;

		return length;

		static bool IsBeginning(char character) => char.IsAsciiLetter(character) || character == '_';
	}
}
