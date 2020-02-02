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
using System.Collections.Generic;

namespace Zongsoft.Data.Common.Expressions
{
	public abstract class StatementBuilderBase : IStatementBuilder
	{
		#region 私有变量
		private readonly object _syncRoot;
		#endregion

		#region 成员字段
		private IStatementBuilder<DataCountContext> _count;
		private IStatementBuilder<DataExistContext> _exist;
		private IStatementBuilder<DataExecuteContext> _execution;
		private IStatementBuilder<DataIncrementContext> _increment;

		private IStatementBuilder<DataSelectContext> _select;
		private IStatementBuilder<DataDeleteContext> _delete;
		private IStatementBuilder<DataInsertContext> _insert;
		private IStatementBuilder<DataUpdateContext> _update;
		private IStatementBuilder<DataUpsertContext> _upsert;
		#endregion

		#region 构造函数
		protected StatementBuilderBase()
		{
			_syncRoot = new object();
		}
		#endregion

		#region 公共方法
		public virtual IEnumerable<IStatementBase> Build(IDataAccessContext context)
		{
			switch(context.Method)
			{
				case DataAccessMethod.Select:
					return this.GetBuilder(ref _select, () => this.CreateSelectStatementBuilder()).Build((DataSelectContext)context);
				case DataAccessMethod.Delete:
					return this.GetBuilder(ref _delete, () => this.CreateDeleteStatementBuilder()).Build((DataDeleteContext)context);
				case DataAccessMethod.Insert:
					return this.GetBuilder(ref _insert, () => this.CreateInsertStatementBuilder()).Build((DataInsertContext)context);
				case DataAccessMethod.Update:
					return this.GetBuilder(ref _update, () => this.CreateUpdateStatementBuilder()).Build((DataUpdateContext)context);
				case DataAccessMethod.Upsert:
					return this.GetBuilder(ref _upsert, () => this.CreateUpsertStatementBuilder()).Build((DataUpsertContext)context);
				case DataAccessMethod.Count:
					return this.GetBuilder(ref _count, () => this.CreateCountStatementBuilder()).Build((DataCountContext)context);
				case DataAccessMethod.Exists:
					return this.GetBuilder(ref _exist, () => this.CreateExistStatementBuilder()).Build((DataExistContext)context);
				case DataAccessMethod.Execute:
					return this.GetBuilder(ref _execution, () => this.CreateExecutionStatementBuilder()).Build((DataExecuteContext)context);
				case DataAccessMethod.Increment:
					return this.GetBuilder(ref _increment, () => this.CreateIncrementStatementBuilder()).Build((DataIncrementContext)context);
				default:
					throw new DataException($"Unsupported data access '{context.Method}' operation.");
			}
		}
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

		protected abstract IStatementBuilder<DataCountContext> CreateCountStatementBuilder();
		protected abstract IStatementBuilder<DataExistContext> CreateExistStatementBuilder();
		protected abstract IStatementBuilder<DataExecuteContext> CreateExecutionStatementBuilder();

		protected virtual IStatementBuilder<DataIncrementContext> CreateIncrementStatementBuilder()
		{
			return new IncrementStatementBuilder();
		}
		#endregion
	}
}
