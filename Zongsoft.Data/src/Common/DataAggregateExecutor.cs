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

using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Common;

public class DataAggregateExecutor : IDataExecutor<AggregateStatement>
{
	#region 异步执行
	public ValueTask<bool> ExecuteAsync(IDataAccessContext context, AggregateStatement statement, CancellationToken cancellation)
	{
		if(context is DataAggregateContext ctx)
			return this.OnExecuteAsync(ctx, statement, cancellation);

		throw new DataException(string.Format(Properties.Resources.DataExecutor_UnsupportedContext_Message, this.GetType().Name, context.GetType().Name));
	}

	protected virtual async ValueTask<bool> OnExecuteAsync(DataAggregateContext context, AggregateStatement statement, CancellationToken cancellation)
	{
		//根据生成的脚本创建对应的数据命令
		await using var command = context.Session.Build(context, statement);

		//执行命令
		var result = await command.ExecuteScalarAsync(cancellation);

		if(result == null || System.Convert.IsDBNull(result))
			context.Result = null;
		else
			context.Result = result;

		return true;
	}
	#endregion
}
