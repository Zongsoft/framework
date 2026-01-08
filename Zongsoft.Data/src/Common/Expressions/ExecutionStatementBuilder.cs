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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Collections.Generic;

namespace Zongsoft.Data.Common.Expressions;

public class ExecutionStatementBuilder : IStatementBuilder<DataExecuteContext>
{
	#region 公共方法
	public IEnumerable<IStatementBase> Build(DataExecuteContext context)
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
