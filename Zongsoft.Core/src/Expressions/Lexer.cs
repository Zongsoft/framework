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
using System.Collections.Generic;

namespace Zongsoft.Expressions
{
	/// <summary>
	/// 提供词法解析的类。
	/// </summary>
	public class Lexer
	{
		#region 单例字段
		public static readonly Lexer Instance = new Lexer();
		#endregion

		#region 构造函数
		public Lexer()
		{
			this.Tokenizers = new List<ITokenizer>()
			{
				new Tokenization.NullTokenizer(),
				new Tokenization.NumberTokenizer(),
				new Tokenization.StringTokenizer(),
				new Tokenization.BooleanTokenizer(),
				new Tokenization.IdentifierTokenizer(),
				new Tokenization.SymbolTokenizer(),
			};
		}
		#endregion

		#region 公共属性
		public IList<ITokenizer> Tokenizers { get; }
		#endregion

		#region 公共方法
		public TokenScanner GetScanner(string text)
		{
			if(string.IsNullOrEmpty(text))
				throw new ArgumentNullException(nameof(text));

			return new TokenScanner(this, text);
		}

		public TokenScanner GetScanner(Stream stream)
		{
			if(stream == null)
				throw new ArgumentNullException(nameof(stream));

			return new TokenScanner(this, stream);
		}
		#endregion
	}
}
