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

public class SelectStatementVisitor : SelectStatementVisitorBase<SelectStatement>
{
	#region 构造函数
	protected SelectStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, SelectStatement statement)
	{
		this.VisitWith(context, statement.With);
		this.VisitSelect(context, statement.Select);
		this.VisitInto(context, statement.Into);
		this.VisitFrom(context, statement.From);
		this.VisitWhere(context, statement.Where);
		this.VisitGroupBy(context, statement.GroupBy);
		this.VisitOrderBy(context, statement.OrderBy);
	}

	protected override void OnVisiting(ExpressionVisitorContext context, SelectStatement statement)
	{
		if(!string.IsNullOrEmpty(statement.Alias))
			context.WriteLine($"/* {statement.Alias} */");

		//如果需要分页，则首先生成分页查询
		if(statement.Paging != null && statement.Paging.IsPaged())
		{
			/*
			 * 注意：有分组子句的分页和没有分组子句的分页查询是不同的。
			 */

			if(statement.GroupBy == null)
			{
				context.Write("SELECT COUNT(*)");
				this.VisitFrom(context, statement.From);
				this.VisitWhere(context, statement.Where);
				context.WriteLine(";");
			}
			else
			{
				context.WriteLine("SELECT COUNT(*) FROM (");
				context.WriteLine("SELECT ");

				int index = 0;

				foreach(var key in statement.GroupBy.Keys)
				{
					if(index++ > 0)
						context.Write(",");

					context.Visit(key);
				}

				this.VisitFrom(context, statement.From);
				this.VisitWhere(context, statement.Where);
				this.VisitGroupBy(context, statement.GroupBy);
				context.WriteLine(") AS __wrapping__;");
			}
		}

		//调用基类同名方法
		base.OnVisiting(context, statement);
	}

	protected override void OnVisited(ExpressionVisitorContext context, SelectStatement statement)
	{
		if(context.Depth == 0)
			context.WriteLine(";");

		//调用基类同名方法
		base.OnVisited(context, statement);
	}
	#endregion

	#region 虚拟方法
	protected virtual void VisitInto(ExpressionVisitorContext context, IIdentifier into)
	{
		if(into == null)
			return;

		context.Write(" INTO ");
		context.Visit(into);
	}

	protected virtual void VisitGroupBy(ExpressionVisitorContext context, GroupByClause clause)
	{
		if(clause == null || clause.Keys.Count == 0)
			return;

		if(context.Output.Length > 0)
			context.WriteLine();

		context.Write("GROUP BY ");

		int index = 0;

		foreach(var key in clause.Keys)
		{
			if(index++ > 0)
				context.Write(",");

			context.Visit(key);
		}

		if(clause.Having != null)
		{
			context.WriteLine();
			context.Write("HAVING ");
			context.Visit(clause.Having);
		}
	}

	protected virtual void VisitOrderBy(ExpressionVisitorContext context, OrderByClause clause)
	{
		if(clause == null || clause.Members.Count == 0)
			return;

		if(context.Output.Length > 0)
			context.WriteLine();

		context.Write("ORDER BY ");

		int index = 0;

		foreach(var member in clause.Members)
		{
			if(index++ > 0)
				context.Write(",");

			context.Visit(member.Field);

			if(member.Mode == SortingMode.Descending)
				context.Write(" DESC");
		}
	}
	#endregion
}
