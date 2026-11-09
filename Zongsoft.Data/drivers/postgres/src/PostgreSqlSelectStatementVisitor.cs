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

namespace Zongsoft.Data.PostgreSql;

public class PostgreSqlSelectStatementVisitor : SelectStatementVisitor
{
	#region 单例字段
	public static readonly PostgreSqlSelectStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private PostgreSqlSelectStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, SelectStatement statement)
	{
		//调用基类同名方法
		base.OnVisit(context, statement);

		if(statement.Paging != null)
		{
			if(statement.Paging.IsPaged(out var index, out var size))
				this.VisitPage(context, index, size);
			if(statement.Paging.IsLimited(out var count, out var offset))
				this.VisitLimit(context, count, offset);
		}
	}
	#endregion

	#region 虚拟方法
	protected virtual void VisitPage(ExpressionVisitorContext context, int index, int size)
	{
		if(context.Output.Length > 0)
			context.WriteLine();

		context.Write($"LIMIT {size}");

		if(index > 1)
			context.Write($" OFFSET {(index - 1) * size}");
	}

	protected virtual void VisitLimit(ExpressionVisitorContext context, int count, long offset)
	{
		if(context.Output.Length > 0)
			context.WriteLine();

		context.Write($"LIMIT {count}");

		if(offset > 0)
			context.Write($" OFFSET {offset}");
	}
	#endregion
}
