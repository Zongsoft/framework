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

namespace Zongsoft.Expressions.Tokenization
{
	public class NumberTokenizer : ITokenizer
	{
		#region 公共方法
		public TokenResult Tokenize(TextReader reader)
		{
			var valueRead = reader.Read();

			if(valueRead < 0)
				return TokenResult.Fail(0);

			var chr = (char)valueRead;
			var number = string.Empty;

			if(!char.IsDigit(chr))
				return TokenResult.Fail(-1);

			number += chr;

			while((valueRead = reader.Read()) > 0)
			{
				chr = (char)valueRead;

				if(char.IsDigit(chr))
					number += chr;
				else if(chr == '.')
				{
					if(number.Contains('.'))
						throw new SyntaxException("Illegal numeric literal, it contains multiple dot(.) symbol.");

					number += chr;
				}
				else if(chr == 'L')
				{
					if(number.Contains('.'))
						throw new SyntaxException("Illegal long integer suffix symbol(L), because it's a float numeric literal.");

					return new TokenResult(0, new Token(TokenType.Constant, long.Parse(number)));
				}
				else if(chr == 'm' || chr == 'M')
				{
					return new TokenResult(0, new Token(TokenType.Constant, decimal.Parse(number)));
				}
				else if(chr == 'f' || chr == 'F')
				{
					return new TokenResult(0, new Token(TokenType.Constant, float.Parse(number)));
				}
				else if(chr == 'd' || chr == 'D')
				{
					return new TokenResult(0, new Token(TokenType.Constant, double.Parse(number)));
				}
				else
				{
					if(number[number.Length - 1] == '.')
						throw new SyntaxException("Illegal numeric literal, cann't end with a dot(.) symbol.");

					return new TokenResult(-1, CreateToken(number));
				}
			}

			return new TokenResult(0, CreateToken(number));
		}
		#endregion

		#region 私有方法
		private static Token CreateToken(string value) => value.Contains('.') ?
			new Token(TokenType.Constant, double.Parse(value)) :
			new Token(TokenType.Constant, int.Parse(value));
		#endregion
	}
}
