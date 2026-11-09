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
using System.Collections.Generic;

namespace Zongsoft.Data.Common.Expressions;

public class SelectClause
{
	#region 构造函数
	public SelectClause() => this.Members = new List<IExpression>();
	public SelectClause(bool isDistinct)
	{
		this.IsDistinct = isDistinct;
		this.Members = new List<IExpression>();
	}
	#endregion

	#region 公共属性
	public bool IsDistinct { get; set; }
	public IList<IExpression> Members { get; }
	#endregion
}
