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

namespace Zongsoft.Data.Common.Expressions;

public readonly struct FieldValue
{
	#region 构造函数
	public FieldValue(FieldIdentifier field, IExpression value)
	{
		this.Field = field;
		this.Value = value;
	}
	#endregion

	#region 公共属性
	public FieldIdentifier Field { get; }
	public IExpression Value { get; }
	#endregion

	#region 重写方法
	public override string ToString() => $"{this.Field}={this.Value}";
	#endregion
}
