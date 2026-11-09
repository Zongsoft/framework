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

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.TDengine;

public class TDengineExistStatementVisitor : ExistStatementVisitor
{
	#region 单例字段
	public static readonly TDengineExistStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private TDengineExistStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, ExistStatement statement)
	{
		//调用基类同名方法
		base.OnVisit(context, statement);

		//限制最多只返回一条记录
		context.Write(" LIMIT 1");
	}
	#endregion
}
