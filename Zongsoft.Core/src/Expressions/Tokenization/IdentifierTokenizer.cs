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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
