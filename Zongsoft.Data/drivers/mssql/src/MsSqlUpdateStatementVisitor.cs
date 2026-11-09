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

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MsSql;

public class MsSqlUpdateStatementVisitor : UpdateStatementVisitor
{
	#region 单例字段
	public static readonly MsSqlUpdateStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private MsSqlUpdateStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void VisitTables(ExpressionVisitorContext context, UpdateStatement statement, IList<TableIdentifier> tables)
	{
		if(tables.Count > 1)
			throw new DataException(Properties.Resources.UpdateStatement_MultiTableUnsupported_Message);

		if(string.IsNullOrEmpty(tables[0].Alias))
			context.Visit(tables[0]);
		else
			context.Write(tables[0].Alias);
	}

	protected override void VisitFrom(ExpressionVisitorContext context, UpdateStatement statement, ICollection<ISource> sources)
	{
		//生成OUTPUT(RETURNING)子句
		if(statement.Returning != null)
			this.VisitReturning(context, statement.Returning);

		//调用基类同名方法
		base.VisitFrom(context, statement, sources);
	}

	protected override void OnVisited(ExpressionVisitorContext context, UpdateStatement statement) => context.WriteLine(";");
	protected override void OnVisiteReturning(ExpressionVisitorContext context, ReturningClause clause) => context.WriteLine(" OUTPUT");
	#endregion
}
