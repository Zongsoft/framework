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

/// <summary>表示词素的类型。</summary>
public enum TokenType
{
	/// <summary>常量（包括：字符串、数值、布尔值、空值(<c>null</c>)等）</summary>
	Constant,

	/// <summary>标识（变量名、参数名、方法名、类名）</summary>
	Identifier,

	/// <summary>符号</summary>
	Symbol,

	/// <summary>关键字</summary>
	Keyword,
}
