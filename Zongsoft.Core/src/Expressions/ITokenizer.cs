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

namespace Zongsoft.Expressions;

/// <summary>表示分词器的接口。</summary>
public interface ITokenizer
{
	/// <summary>从指定文本的起始位置提取一个词素。</summary>
	/// <param name="text">尚未扫描的文本。</param>
	/// <returns>返回词素及其消耗的字符数；不匹配时返回失败结果，不消耗文本。</returns>
	TokenResult Tokenize(ReadOnlySpan<char> text);
}
