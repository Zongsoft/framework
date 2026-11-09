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

namespace Zongsoft.Data.MsSql;

public class MsSqlSelectStatementVisitor : SelectStatementVisitor
{
	#region 单例字段
	public static readonly MsSqlSelectStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private MsSqlSelectStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, SelectStatement statement)
	{
		//由于分页子句必须依赖于排序(OrderBy)子句，所以在没有指定排序子句的情况下默认以主键进行排序
		if(statement.Paging != null && (statement.Paging.IsPaged() || statement.Paging.IsLimited()) &&
		   (statement.OrderBy == null || statement.OrderBy.Members.Count == 0) &&
		   statement.Table != null)
		{
			statement.OrderBy ??= new OrderByClause();

			foreach(var key in statement.Table.Entity.Key)
				statement.OrderBy.Add(statement.Table.CreateField(key));
		}

		//调用基类同名方法
		base.OnVisit(context, statement);

		if(statement.Paging != null)
		{
			if(statement.Paging.IsPaged(out var index, out var size))
				this.VisitPage(context, index, size);
			else if(statement.Paging.IsLimited(out var count, out var offset))
				this.VisitLimit(context, count, offset);
		}
	}
	#endregion

	#region 虚拟方法
	protected virtual void VisitPage(ExpressionVisitorContext context, int index, int size)
	{
		if(context.Output.Length > 0)
			context.WriteLine();

		context.Write($"OFFSET {(index - 1) * size} ROWS FETCH NEXT {size} ROWS ONLY");
	}

	protected virtual void VisitLimit(ExpressionVisitorContext context, int count, long offset)
	{
		if(context.Output.Length > 0)
			context.WriteLine();

		context.Write($"OFFSET {offset} ROWS FETCH NEXT {count} ROWS ONLY");
	}
	#endregion
}
