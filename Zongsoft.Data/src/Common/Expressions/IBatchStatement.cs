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

using System.Collections.Generic;

namespace Zongsoft.Data.Common.Expressions;

/// <summary>表示支持按行分批执行的写入语句。</summary>
public interface IBatchStatement : IMutateStatement
{
	/// <summary>获取单次执行的最大记录数。</summary>
	int BatchSize { get; }

	/// <summary>获取单次执行的参数数量上限，零表示不额外限制。</summary>
	int ParameterLimit { get; }

	/// <summary>获取每行写入的字段集合。</summary>
	IList<FieldIdentifier> Fields { get; }

	/// <summary>获取按行排列的字段值集合，每行的值数量必须与字段数量一致。</summary>
	IList<IExpression> Values { get; }
}
