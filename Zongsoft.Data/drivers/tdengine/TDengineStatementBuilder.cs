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

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.TDengine
{
	public class TDengineStatementBuilder : StatementBuilderBase
	{
		#region 单例字段
		public static readonly TDengineStatementBuilder Default = new TDengineStatementBuilder();
		#endregion

		#region 构造函数
		private TDengineStatementBuilder() { }
		#endregion

		#region 重写方法
		protected override IStatementBuilder<DataSelectContext> CreateSelectStatementBuilder() => new TDengineSelectStatementBuilder();
		protected override IStatementBuilder<DataDeleteContext> CreateDeleteStatementBuilder() => new TDengineDeleteStatementBuilder();
		protected override IStatementBuilder<DataInsertContext> CreateInsertStatementBuilder() => new TDengineInsertStatementBuilder();
		protected override IStatementBuilder<DataUpdateContext> CreateUpdateStatementBuilder() => new TDengineUpdateStatementBuilder();
		protected override IStatementBuilder<DataUpsertContext> CreateUpsertStatementBuilder() => new TDengineUpsertStatementBuilder();
		protected override IStatementBuilder<DataAggregateContext> CreateAggregateStatementBuilder() => new TDengineAggregateStatementBuilder();
		protected override IStatementBuilder<DataExistContext> CreateExistStatementBuilder() => new TDengineExistStatementBuilder();
		protected override IStatementBuilder<DataExecuteContext> CreateExecutionStatementBuilder() => new TDengineExecutionStatementBuilder();
		#endregion
	}
}
