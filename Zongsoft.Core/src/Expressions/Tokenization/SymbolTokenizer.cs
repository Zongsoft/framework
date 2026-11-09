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

public class SymbolTokenizer : LiteralTokenizerBase
{
	#region 构造函数
	public SymbolTokenizer(params string[] symbols) : base(symbols)
	{
		if(symbols == null || symbols.Length == 0)
			base.Literals = SymbolToken.Symbols;
	}
	#endregion

	#region 重写方法
	protected override Token CreateToken(string literal) => SymbolToken.GetToken(literal);
	#endregion
}
