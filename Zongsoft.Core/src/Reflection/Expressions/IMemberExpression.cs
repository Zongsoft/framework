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

/// <summary>表示表达式元素(节点)的接口。</summary>
public interface IMemberExpression
{
	/// <summary>获取一个值，指示表达式的种类。</summary>
	MemberExpressionType ExpressionType { get; }

	/// <summary>获取上一个表达式。</summary>
	IMemberExpression Previous { get; }

	/// <summary>获取下一个表达式。</summary>
	IMemberExpression Next { get; }
}
