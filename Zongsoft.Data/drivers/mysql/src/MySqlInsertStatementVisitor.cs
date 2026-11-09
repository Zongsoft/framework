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

namespace Zongsoft.Data.MySql;

public class MySqlInsertStatementVisitor : InsertStatementVisitor
{
	#region 单例字段
	public static readonly MySqlInsertStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private MySqlInsertStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void VisitInsert(ExpressionVisitorContext context, InsertStatement statement)
	{
		if(statement.Options.ConstraintIgnored)
			context.Write("INSERT IGNORE INTO ");
		else
			context.Write("INSERT INTO ");
	}
	#endregion
}
