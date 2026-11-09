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

public class TableDefinitionVisitor : StatementVisitorBase<TableDefinition>
{
	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, TableDefinition statement)
	{
		if(statement.IsTemporary)
			context.WriteLine($"CREATE TEMPORARY TABLE {statement.Name} (");
		else
			context.WriteLine($"CREATE TABLE {statement.Name} (");

		int index = 0;

		foreach(var field in statement.Fields)
		{
			if(index++ > 0)
				context.WriteLine(",");

			context.Visit(field);
		}

		context.WriteLine(");");
	}
	#endregion
}
