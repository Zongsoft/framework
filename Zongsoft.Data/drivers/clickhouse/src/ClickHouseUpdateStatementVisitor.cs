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

namespace Zongsoft.Data.ClickHouse;

public class ClickHouseUpdateStatementVisitor : UpdateStatementVisitor
{
	#region 单例字段
	public static readonly ClickHouseUpdateStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private ClickHouseUpdateStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, UpdateStatement statement)
	{
		if(statement.Tables == null || statement.Tables.Count != 1)
			throw new NotSupportedException();
		if(statement.Fields == null || statement.Fields.Count == 0)
			throw new NotSupportedException();

		var table = statement.Tables[0];
		var alias = table.Alias;
		table.Alias = null;

		try
		{
			context.Write("ALTER TABLE ");
			context.Visit(table);
			context.WriteLine(" UPDATE");

			this.VisitFields(context, statement, statement.Fields);
			this.VisitWhere(context, statement, statement.Where);
		}
		finally
		{
			table.Alias = alias;
		}
	}

	protected override void OnVisited(ExpressionVisitorContext context, UpdateStatement statement)
	{
		context.WriteLine(" SETTINGS mutations_sync=1;");
	}
	#endregion
}
