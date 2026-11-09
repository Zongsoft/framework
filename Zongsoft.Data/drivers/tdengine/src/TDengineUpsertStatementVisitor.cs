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

using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.TDengine;

public class TDengineUpsertStatementVisitor : UpsertStatementVisitor
{
	#region 单例字段
	public static readonly TDengineUpsertStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private TDengineUpsertStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, UpsertStatement statement)
		=> TDengineInsertStatementVisitor.Visit(context, statement, statement.Fields, statement.Values, "upsert");

	protected override void OnVisiting(ExpressionVisitorContext context, UpsertStatement statement) { }
	protected override void OnVisited(ExpressionVisitorContext context, UpsertStatement statement) { }
	#endregion
}
