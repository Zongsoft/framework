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
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zongsoft.Data.Common.Expressions;

public class ExecutionStatementBuilder : IStatementBuilder<DataExecuteContext>
{
	#region 公共方法
	public IAsyncEnumerable<IStatementBase> BuildAsync(DataExecuteContext context, CancellationToken cancellation = default) => Collections.Enumerable.EnumerateAsync<IStatementBase>(this.Build(context), cancellation);

	private IEnumerable<IStatementBase> Build(DataExecuteContext context)
	{
		var statement = this.CreateStatement(context);

		foreach(var parameter in context.Command.Parameters)
		{
			statement.Parameters.Add(Expression.Parameter(parameter));
		}

		if(statement.Parameters.Count > 0 && context.Parameters != null)
		{
			foreach(var entry in context.Parameters)
			{
				if(IsInputParameter(entry) && statement.Parameters.TryGetValue(entry.Name, out var parameter))
					parameter.Value = entry.Value;
			}
		}

		yield return statement;

		static bool IsInputParameter(Parameter parameter) => parameter != null &&
		(
			parameter.Direction == ParameterDirection.Input ||
			parameter.Direction == ParameterDirection.InputOutput
		);
	}
	#endregion

	#region 虚拟方法
	protected virtual ExecutionStatement CreateStatement(DataExecuteContext context) => new(context.Command);
	#endregion
}
