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

public class MsSqlInsertStatementVisitor : InsertStatementVisitor
{
	#region 单例字段
	public static readonly MsSqlInsertStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private MsSqlInsertStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, InsertStatement statement)
	{
		if(!statement.Options.ConstraintIgnored)
		{
			base.OnVisit(context, statement);
			return;
		}

		const string TARGET_ALIAS = "T";
		const string SOURCE_ALIAS = "SRC";

		if(statement.Fields == null || statement.Fields.Count == 0)
			throw new DataException(Properties.Resources.InsertStatement_MissingFields_Message);
		if(!statement.Entity.HasKey)
			throw new DataException(string.Format(Properties.Resources.InsertStatement_EntityKeyRequired_Message, statement.Entity.Name));

		context.Write("MERGE INTO ");
		context.Visit(statement.Table);
		context.WriteLine(" AS " + TARGET_ALIAS);
		context.WriteLine("USING (VALUES");

		for(int i = 0; i < statement.Values.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			if(i % statement.Fields.Count == 0)
				context.Write("(");

			context.Visit(statement.Values[i]);

			if(i % statement.Fields.Count == statement.Fields.Count - 1)
				context.Write(")");
		}

		context.WriteLine(") AS " + SOURCE_ALIAS + " (");

		for(int i = 0; i < statement.Fields.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			context.Write(context.Dialect.GetIdentifier(statement.Fields[i].Name));
		}

		context.WriteLine(") ON");

		for(int i = 0; i < statement.Entity.Key.Length; i++)
		{
			var field = Metadata.DataEntityPropertyExtension.GetFieldName(statement.Entity.Key[i], out _);

			if(i > 0)
				context.Write(" AND ");

			context.Write($"{TARGET_ALIAS}.[{field}]={SOURCE_ALIAS}.[{field}]");
		}

		context.WriteLine();
		context.WriteLine("WHEN NOT MATCHED THEN");
		context.Write("\tINSERT (");

		for(int i = 0; i < statement.Fields.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			context.Write(context.Dialect.GetIdentifier(statement.Fields[i]));
		}

		context.Write(") VALUES (");

		for(int i = 0; i < statement.Fields.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			context.Write(SOURCE_ALIAS + ".");
			context.Write(context.Dialect.GetIdentifier(statement.Fields[i].Name));
		}

		context.Write(")");

		if(statement.Returning != null)
			this.VisitReturning(context, statement.Returning);
	}

	protected override void VisitValues(ExpressionVisitorContext context, InsertStatement statement, ICollection<IExpression> values, int rounds)
	{
		//生成OUTPUT(RETURNING)子句
		if(statement.Returning != null)
			this.VisitReturning(context, statement.Returning);

		//调用基类同名方法
		base.VisitValues(context, statement, values, rounds);
	}

	protected override void OnVisited(ExpressionVisitorContext context, InsertStatement statement) => context.WriteLine(";");
	protected override void OnVisiteReturning(ExpressionVisitorContext context, ReturningClause clause) => context.WriteLine(" OUTPUT");
	#endregion
}
