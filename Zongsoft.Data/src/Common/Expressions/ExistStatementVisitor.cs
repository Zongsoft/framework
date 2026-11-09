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

public class ExistStatementVisitor : SelectStatementVisitorBase<ExistStatement>
{
	#region 重写方法
	protected override void OnVisiting(ExpressionVisitorContext context, ExistStatement statement)
	{
		context.Write("SELECT COUNT(*) FROM (");
		base.OnVisiting(context, statement);
	}

	protected override void OnVisited(ExpressionVisitorContext context, ExistStatement statement)
	{
		base.OnVisited(context, statement);
		context.WriteLine(") AS __zs_temporary__");
	}
	#endregion
}
