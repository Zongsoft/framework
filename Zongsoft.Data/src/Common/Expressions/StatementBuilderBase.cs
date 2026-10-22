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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zongsoft.Data.Common.Expressions;

public abstract class StatementBuilderBase : IStatementBuilder
{
	#region 私有变量
	private readonly object _syncRoot;
	#endregion

	#region 成员字段
	private IStatementBuilder<DataExistContext> _exist;
	private IStatementBuilder<DataExecuteContext> _execution;
	private IStatementBuilder<DataAggregateContext> _aggregate;

	private IStatementBuilder<DataSelectContext> _select;
	private IStatementBuilder<DataDeleteContext> _delete;
	private IStatementBuilder<DataInsertContext> _insert;
	private IStatementBuilder<DataUpdateContext> _update;
	private IStatementBuilder<DataUpsertContext> _upsert;
	#endregion

	#region 构造函数
	protected StatementBuilderBase() => _syncRoot = new object();
	#endregion

	#region 公共方法
	public virtual IAsyncEnumerable<IStatementBase> BuildAsync(IDataAccessContext context, CancellationToken cancellation = default) => context.Method switch
	{
		DataAccessMethod.Select => this.GetBuilder(ref _select, this.CreateSelectStatementBuilder).BuildAsync((DataSelectContext)context, cancellation),
		DataAccessMethod.Delete => this.GetBuilder(ref _delete, this.CreateDeleteStatementBuilder).BuildAsync((DataDeleteContext)context, cancellation),
		DataAccessMethod.Insert => this.GetBuilder(ref _insert, this.CreateInsertStatementBuilder).BuildAsync((DataInsertContext)context, cancellation),
		DataAccessMethod.Update => this.GetBuilder(ref _update, this.CreateUpdateStatementBuilder).BuildAsync((DataUpdateContext)context, cancellation),
		DataAccessMethod.Upsert => this.GetBuilder(ref _upsert, this.CreateUpsertStatementBuilder).BuildAsync((DataUpsertContext)context, cancellation),
		DataAccessMethod.Exists => this.GetBuilder(ref _exist, this.CreateExistStatementBuilder).BuildAsync((DataExistContext)context, cancellation),
		DataAccessMethod.Execute => this.GetBuilder(ref _execution, this.CreateExecutionStatementBuilder).BuildAsync((DataExecuteContext)context, cancellation),
		DataAccessMethod.Aggregate => this.GetBuilder(ref _aggregate, this.CreateAggregateStatementBuilder).BuildAsync((DataAggregateContext)context, cancellation),
		_ => throw new DataException(string.Format(Properties.Resources.DataAccess_OperationUnsupported_Message, context.Method)),
	};
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private IStatementBuilder<TContext> GetBuilder<TContext>(ref IStatementBuilder<TContext> builder, Func<IStatementBuilder<TContext>> factory) where TContext : IDataAccessContext
	{
		if(builder == null)
		{
			lock(_syncRoot)
			{
				if(builder == null)
					builder = factory();
			}
		}

		return builder;
	}
	#endregion

	#region 抽象方法
	protected abstract IStatementBuilder<DataSelectContext> CreateSelectStatementBuilder();
	protected abstract IStatementBuilder<DataDeleteContext> CreateDeleteStatementBuilder();
	protected abstract IStatementBuilder<DataInsertContext> CreateInsertStatementBuilder();
	protected abstract IStatementBuilder<DataUpdateContext> CreateUpdateStatementBuilder();
	protected abstract IStatementBuilder<DataUpsertContext> CreateUpsertStatementBuilder();

	protected abstract IStatementBuilder<DataExistContext> CreateExistStatementBuilder();
	protected abstract IStatementBuilder<DataExecuteContext> CreateExecutionStatementBuilder();
	protected abstract IStatementBuilder<DataAggregateContext> CreateAggregateStatementBuilder();
	#endregion
}
