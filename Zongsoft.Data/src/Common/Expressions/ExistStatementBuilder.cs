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
using System.Collections.Generic;

namespace Zongsoft.Data.Common.Expressions;

public class ExistStatementBuilder : IStatementBuilder<DataExistContext>
{
	#region 公共方法
	public IEnumerable<IStatementBase> Build(DataExistContext context)
	{
		//创建存在语句
		var statement = this.CreateStatement(context);

		//生成条件子句
		statement.Where = statement.Where(context.Validate(), context.Aliaser);

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
