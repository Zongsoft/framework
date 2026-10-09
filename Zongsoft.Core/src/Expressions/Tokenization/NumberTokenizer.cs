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
using System.Globalization;

namespace Zongsoft.Expressions.Tokenization;

public class NumberTokenizer : ITokenizer
{
	#region 成员字段
	private readonly bool _signed;
	#endregion

	#region 构造函数
	public NumberTokenizer() { }
	internal NumberTokenizer(bool signed) => _signed = signed;
	#endregion

	#region 公共方法
	public TokenResult Tokenize(ReadOnlySpan<char> text)
	{
		var length = _signed && !text.IsEmpty && text[0] == '-' ? 1 : 0;

		if(length == text.Length || !char.IsAsciiDigit(text[length]))
			return TokenResult.Fail();

		while(length < text.Length && char.IsAsciiDigit(text[length]))
			length++;

		var fractional = length < text.Length && text[length] == '.';

		if(fractional)
		{
			length++;

			if(length == text.Length || !char.IsAsciiDigit(text[length]))
				throw new SyntaxException(Properties.Resources.NumberTokenizer_TrailingDot_Message);

			while(length < text.Length && char.IsAsciiDigit(text[length]))
				length++;

			if(length < text.Length && text[length] == '.')
				throw new SyntaxException(Properties.Resources.NumberTokenizer_MultipleDots_Message);
		}

		var literal = text[..length];
		var suffix = length < text.Length ? text[length] : '\0';
		if(suffix is 'l' or 'L' or 'f' or 'F' or 'd' or 'D' or 'm' or 'M')
			length++;

		object number;

		switch(suffix)
		{
			case 'l':
			case 'L':
				if(fractional)
					throw new SyntaxException(Properties.Resources.NumberTokenizer_InvalidLongSuffix_Message);
				number = long.Parse(literal, CultureInfo.InvariantCulture);
				break;
			case 'f':
			case 'F':
				var single = float.Parse(literal, CultureInfo.InvariantCulture);
				if(!float.IsFinite(single))
					throw new OverflowException(Properties.Resources.NumberTokenizer_NumberOverflow_Message);
				number = single;
				break;
			case 'd':
			case 'D':
				number = ParseDouble(literal);
				break;
			case 'm':
			case 'M':
				number = decimal.Parse(literal, CultureInfo.InvariantCulture);
				break;
			default:
				if(fractional)
					number = ParseDouble(literal);
				else
				{
					var integer = long.Parse(literal, CultureInfo.InvariantCulture);
					number = integer is >= int.MinValue and <= int.MaxValue ? (object)(int)integer : integer;
				}
				break;
		}

		return new TokenResult(length, new Token(TokenType.Constant, number));

		static double ParseDouble(ReadOnlySpan<char> literal)
		{
			var value = double.Parse(literal, CultureInfo.InvariantCulture);

			if(!double.IsFinite(value))
				throw new OverflowException(Properties.Resources.NumberTokenizer_NumberOverflow_Message);

			return value;
		}
	}
	#endregion
}
