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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.ClickHouse library.
 *
 * The Zongsoft.Data.ClickHouse is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.ClickHouse is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.ClickHouse library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.ClickHouse
{
	public class ClickHouseStatementBuilder : StatementBuilderBase
	{
		#region 单例字段
		public static readonly ClickHouseStatementBuilder Default = new ClickHouseStatementBuilder();
		#endregion

		#region 构造函数
		private ClickHouseStatementBuilder() { }
		#endregion

		#region 重写方法
		protected override IStatementBuilder<DataSelectContext> CreateSelectStatementBuilder() => new ClickHouseSelectStatementBuilder();
		protected override IStatementBuilder<DataDeleteContext> CreateDeleteStatementBuilder() => new ClickHouseDeleteStatementBuilder();
		protected override IStatementBuilder<DataInsertContext> CreateInsertStatementBuilder() => new ClickHouseInsertStatementBuilder();
		protected override IStatementBuilder<DataUpdateContext> CreateUpdateStatementBuilder() => new ClickHouseUpdateStatementBuilder();
		protected override IStatementBuilder<DataUpsertContext> CreateUpsertStatementBuilder() => new ClickHouseUpsertStatementBuilder();
		protected override IStatementBuilder<DataAggregateContext> CreateAggregateStatementBuilder() => new ClickHouseAggregateStatementBuilder();
		protected override IStatementBuilder<DataExistContext> CreateExistStatementBuilder() => new ClickHouseExistStatementBuilder();
		protected override IStatementBuilder<DataExecuteContext> CreateExecutionStatementBuilder() => new ClickHouseExecutionStatementBuilder();
		#endregion
	}
}
