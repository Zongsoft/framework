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

using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.TDengine;

public class TDengineDeleteStatementVisitor : DeleteStatementVisitor
{
	#region 单例字段
	public static readonly TDengineDeleteStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private TDengineDeleteStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, DeleteStatement statement)
	{
		var alias = statement.Table.Alias;

		try
		{
			statement.Table.Alias = null;
			base.OnVisit(context, statement);
		}
		finally
		{
			statement.Table.Alias = alias;
		}
	}

	protected override void VisitTables(ExpressionVisitorContext context, DeleteStatement statement, IList<TableIdentifier> tables) { }
	protected override void VisitFrom(ExpressionVisitorContext context, DeleteStatement statement, ICollection<ISource> sources)
	{
		if(sources == null || sources.Count == 0)
			return;

		context.Write(" FROM ");

		var index = 0;

		foreach(var source in sources)
		{
			if(index++ > 0)
				context.Write(',');

			if(source is TableIdentifier table)
				context.Write(context.Dialect.GetIdentifier(table.Name));
			else
				throw new DataAccessException(TDengineDriver.NAME, -1, string.Format(Properties.Resources.DeleteStatement_SourceUnsupported_Message, source.GetType().FullName));
		}
	}
	#endregion
}
