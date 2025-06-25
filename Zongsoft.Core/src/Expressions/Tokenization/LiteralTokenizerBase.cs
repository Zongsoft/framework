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
using System.Linq;

namespace Zongsoft.Expressions.Tokenization;

public abstract class LiteralTokenizerBase : ITokenizer
{
	#region 成员字段
	private readonly bool _ignoreCase;
	private string[] _literals;
	#endregion

	#region 构造函数
	protected LiteralTokenizerBase(params string[] literals) : this(false, literals) { }
	protected LiteralTokenizerBase(bool ignoreCase, params string[] literals)
	{
		if(literals == null)
			throw new ArgumentNullException(nameof(literals));

		_ignoreCase = ignoreCase;
		_literals = literals;
	}
	#endregion

	#region 保护属性
	protected bool IgnoreCase => _ignoreCase;
	protected string[] Literals
	{
		get => _literals;
		set => _literals = value ?? throw new ArgumentNullException(nameof(value));
	}
	#endregion

	#region 公共方法
	public TokenResult Tokenize(TextReader reader)
	{
		var literal = string.Empty;
		var valueRead = 0;

		while((valueRead = reader.Read()) > 0)
		{
			var chr = (char)valueRead;

			if(char.IsWhiteSpace(chr))
			{
				if(string.IsNullOrEmpty(literal))
					return TokenResult.Fail(0);

				return new TokenResult(0, this.CreateToken(literal));
			}

			if(!this.Literals.Any(p => p.StartsWith(literal + chr, _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
			{
				if(this.Literals.Any(p => string.Equals(p, literal, _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
					return new TokenResult(-1, this.CreateToken(literal));
				else
					return TokenResult.Fail(-(literal.Length + 1));
			}

			literal += chr;
		}

		if(string.IsNullOrEmpty(literal))
			return TokenResult.Fail(0);
		else
			return new TokenResult(0, this.CreateToken(literal));
	}
	#endregion

	#region 抽象方法
	protected abstract Token CreateToken(string literal);
	#endregion
}
