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

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Common;

public class DataInsertExecutor : DataMutateExecutor<InsertStatement>
{
	#region 重写方法
	protected override async ValueTask<bool> OnMutatedAsync(IDataMutateContext context, InsertStatement statement, int count, CancellationToken cancellation)
	{
		//执行获取新增后的自增型字段值
		if(count > 0 && statement.SequenceRetriever != null)
			await context.Provider.Executor.ExecuteAsync(context, statement.SequenceRetriever, cancellation);

		return count > 0;
	}
	#endregion
}
