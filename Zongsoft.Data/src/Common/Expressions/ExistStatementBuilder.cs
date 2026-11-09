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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zongsoft.Data.Common.Expressions;

public class ExistStatementBuilder : IStatementBuilder<DataExistContext>
{
	#region 公共方法
	public async IAsyncEnumerable<IStatementBase> BuildAsync(DataExistContext context, [EnumeratorCancellation] CancellationToken cancellation = default)
	{
		//创建存在语句
		var statement = this.CreateStatement(context);

		//生成条件子句
		statement.Where = statement.Where(await context.ValidateAsync(cancellation).ConfigureAwait(false), context.Aliaser);

		//生成选择成员为主键项
		foreach(var key in statement.Table.Entity.Key)
		{
			statement.Select.Members.Add(statement.Table.CreateField(key));
		}

		yield return statement;
	}
	#endregion

	#region 虚拟方法
	protected virtual ExistStatement CreateStatement(DataExistContext context) => new(context.Entity);
	#endregion
}
