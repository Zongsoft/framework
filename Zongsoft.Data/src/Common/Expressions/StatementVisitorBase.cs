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

public abstract class StatementVisitorBase<TStatement> : IStatementVisitor<TStatement> where TStatement : IStatementBase
{
	#region 构造函数
	protected StatementVisitorBase() { }
	#endregion

	#region 公共方法
	public void Visit(ExpressionVisitorContext context, TStatement statement)
	{
		//通知当前语句开始访问
		this.OnVisiting(context, statement);

		//调用具体的访问方法
		this.OnVisit(context, statement);

		//通知当前语句访问完成
		this.OnVisited(context, statement);
	}
	#endregion

	#region 抽象方法
	protected abstract void OnVisit(ExpressionVisitorContext context, TStatement statement);
	#endregion

	#region 虚拟方法
	protected virtual void OnVisiting(ExpressionVisitorContext context, TStatement statement) { }
	protected virtual void OnVisited(ExpressionVisitorContext context, TStatement statement) { }
	#endregion
}
