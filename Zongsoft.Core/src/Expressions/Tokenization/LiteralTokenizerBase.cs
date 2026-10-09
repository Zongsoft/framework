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
using System.Text;

namespace Zongsoft.Expressions.Tokenization;

public abstract class LiteralTokenizerBase : ITokenizer
{
	#region 成员字段
	private string[] _literals;
	private readonly bool _ignoreCase;
	#endregion

	#region 构造函数
	protected LiteralTokenizerBase(params string[] literals) : this(false, literals) { }
	protected LiteralTokenizerBase(bool ignoreCase, params string[] literals)
	{
		_ignoreCase = ignoreCase;
		_literals = literals ?? throw new ArgumentNullException(nameof(literals));
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
		var comparison = _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		var text = new StringBuilder();
		string matched = null;
		int value;

		while((value = reader.Peek()) >= 0)
		{
			var next = text.ToString() + (char)value;

			if(!_literals.Any(literal => literal.StartsWith(next, comparison)))
				break;

			text.Append((char)reader.Read());

			if(_literals.Any(literal => string.Equals(literal, next, comparison)))
				matched = next;
		}

		if(matched == null)
			return TokenResult.Fail(-text.Length);

		//关键字必须在完整标识符边界结束；符号仍采用最长匹配
		if(char.IsLetterOrDigit(matched[0]) || matched[0] == '_')
		{
			if(text.Length != matched.Length || value >= 0 && (char.IsLetterOrDigit((char)value) || value == '_'))
				return TokenResult.Fail(-text.Length);
		}

		return new TokenResult(matched.Length - text.Length, this.CreateToken(matched));
	}
	#endregion

	#region 抽象方法
	protected abstract Token CreateToken(string literal);
	#endregion
}
