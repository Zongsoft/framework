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
using System.IO;
using System.Collections.Generic;

namespace Zongsoft.Expressions;

/// <summary>提供词法解析的类。</summary>
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
	public TokenScanner GetScanner(ReadOnlySpan<char> text) => new(this, text);
	public TokenScanner GetScanner(Stream stream)
	{
		if(stream == null)
			throw new ArgumentNullException(nameof(stream));

		return new TokenScanner(this, stream);
	}
	#endregion
}
