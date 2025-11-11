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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.TDengine library.
 *
 * The Zongsoft.Data.TDengine is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.TDengine is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.TDengine library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.TDengine;

public class TDengineDeleteStatementVisitor : DeleteStatementVisitor
{
	#region 单例字段
	public static readonly TDengineDeleteStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private TDengineDeleteStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void VisitTables(ExpressionVisitorContext context, DeleteStatement statement, IList<TableIdentifier> tables) { }
	protected override void VisitFrom(ExpressionVisitorContext context, DeleteStatement statement, ICollection<ISource> sources)
	{
		if(sources == null || sources.Count == 0)
			return;

		context.Write(" FROM ");

		var index = 0;

		foreach(var source in sources)
		{
			if(index++ > 0)
				context.Write(',');

			if(source is TableIdentifier table)
				context.Write(context.Dialect.GetIdentifier(table.Name));
			else
				throw new DataAccessException(TDengineDriver.NAME, -1, $"The ‘{source.GetType().FullName}’ type expression is not supported within the FROM clause.");
		}
	}
	#endregion
}
