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

public class ExecutionStatementVisitor : StatementVisitorBase<ExecutionStatement>
{
	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, ExecutionStatement statement)
	{
		if(statement.Command.Type == Metadata.DataCommandType.Procedure)
			context.Output.AppendLine(string.IsNullOrEmpty(statement.Command.Alias) ? statement.Command.Name : statement.Command.Alias);
		else
			context.Output.AppendLine(statement.Command.Scriptor.GetScript(context.Dialect.Name));
	}
	#endregion
}
