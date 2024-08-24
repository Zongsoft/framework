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

namespace Zongsoft.Expressions.Tokenization
{
	public class IdentifierTokenizer : ITokenizer
	{
		#region 公共方法
		public TokenResult Tokenize(TextReader reader)
		{
			var valueRead = reader.Read();

			if(valueRead < 0)
				return TokenResult.Fail(0);

			var chr = (char)valueRead;
			var identifier = string.Empty;

			if(!IsIdentifierBeginning(chr))
				return TokenResult.Fail(-1);

			identifier += chr;

			while((valueRead = reader.Read()) > 0)
			{
				chr = (char)valueRead;

				if(char.IsLetterOrDigit(chr) || chr == '_')
					identifier += chr;
				else
					return new TokenResult(-1, new Token(TokenType.Identifier, identifier));
			}

			return new TokenResult(0, new Token(TokenType.Identifier, identifier));
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static bool IsIdentifierBeginning(char chr) => char.IsLetter(chr) || chr == '_';
		#endregion
	}
}
