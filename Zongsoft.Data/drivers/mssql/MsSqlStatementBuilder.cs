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
 * This file is part of Zongsoft.Data.MsSql library.
 *
 * The Zongsoft.Data.MsSql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.MsSql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.MsSql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MsSql
{
	public class MsSqlStatementBuilder : StatementBuilderBase
	{
		#region 单例字段
		public static readonly MsSqlStatementBuilder Default = new MsSqlStatementBuilder();
		#endregion

		#region 构造函数
		private MsSqlStatementBuilder() { }
		#endregion

		#region 重写方法
		protected override IStatementBuilder<DataSelectContext> CreateSelectStatementBuilder()
		{
			return new MsSqlSelectStatementBuilder();
		}

		protected override IStatementBuilder<DataDeleteContext> CreateDeleteStatementBuilder()
		{
			return new MsSqlDeleteStatementBuilder();
		}

		protected override IStatementBuilder<DataInsertContext> CreateInsertStatementBuilder()
		{
			return new MsSqlInsertStatementBuilder();
		}

		protected override IStatementBuilder<DataUpdateContext> CreateUpdateStatementBuilder()
		{
			return new MsSqlUpdateStatementBuilder();
		}

		protected override IStatementBuilder<DataUpsertContext> CreateUpsertStatementBuilder()
		{
			return new MsSqlUpsertStatementBuilder();
		}

		protected override IStatementBuilder<DataAggregateContext> CreateAggregateStatementBuilder()
		{
			return new MsSqlAggregateStatementBuilder();
		}

		protected override IStatementBuilder<DataExistContext> CreateExistStatementBuilder()
		{
			return new MsSqlExistStatementBuilder();
		}

		protected override IStatementBuilder<DataExecuteContext> CreateExecutionStatementBuilder()
		{
			return new MsSqlExecutionStatementBuilder();
		}
		#endregion
	}
}
