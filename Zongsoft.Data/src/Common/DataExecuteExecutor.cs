/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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

using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Common
{
	public class DataExecuteExecutor : IDataExecutor<ExecutionStatement>
	{
		#region 执行方法
		public bool Execute(IDataAccessContext context, ExecutionStatement statement)
		{
			if(context is DataExecuteContext ctx)
				return this.OnExecute(ctx, statement);

			throw new DataException($"Data Engine Error: The '{this.GetType().Name}' executor does not support execution of '{context.GetType().Name}' context.");
		}

		protected virtual bool OnExecute(DataExecuteContext context, ExecutionStatement statement)
		{
			//根据生成的脚本创建对应的数据命令
			var command = context.Session.Build(context, statement);

			if(context.IsScalar)
			{
				context.Result = command.ExecuteScalar();
				return true;
			}

			using(var reader = command.ExecuteReader())
			{
				context.Result = this.Populate(reader, context.ResultType);
			}

			return true;
		}
		#endregion

		#region 私有方法
		private System.Collections.IEnumerable Populate(IDataReader reader, Type entityType)
		{
			return null;
		}
		#endregion
	}
}
