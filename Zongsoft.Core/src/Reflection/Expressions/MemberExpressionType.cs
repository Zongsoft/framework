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

namespace Zongsoft.Reflection.Expressions;

/// <summary>表示成员表达式类型的枚举。</summary>
public enum MemberExpressionType
{
	/// <summary>常量表达式</summary>
	Constant,

	/// <summary>标识表达式</summary>
	Identifier,

	/// <summary>索引器表达式</summary>
	Indexer,

	/// <summary>方法调用表达式</summary>
	Method,
}
