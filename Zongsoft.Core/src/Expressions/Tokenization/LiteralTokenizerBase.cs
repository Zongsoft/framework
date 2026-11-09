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
	public TokenResult Tokenize(ReadOnlySpan<char> text)
	{
		var comparison = _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		string matched = null;

		foreach(var literal in _literals)
		{
			if(string.IsNullOrEmpty(literal) || literal.Length <= (matched?.Length ?? 0) || !text.StartsWith(literal, comparison))
				continue;

			//关键字必须在完整标识符边界结束；符号仍采用最长匹配
			if((char.IsLetterOrDigit(literal[0]) || literal[0] == '_') && text.Length > literal.Length &&
			   (char.IsLetterOrDigit(text[literal.Length]) || text[literal.Length] == '_'))
				continue;

			matched = literal;
		}

		return matched == null ? TokenResult.Fail() : new TokenResult(matched.Length, this.CreateToken(matched));
	}
	#endregion

	#region 抽象方法
	/// <summary>为匹配的已配置字面量创建词素。</summary>
	/// <param name="literal">配置中的字面量字符串，保留其配置时的大小写。</param>
	/// <returns>返回匹配的词素。</returns>
	protected abstract Token CreateToken(string literal);
	#endregion
}
