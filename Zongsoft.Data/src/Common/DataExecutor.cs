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

using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Common;

/// <summary>提供数据语句的默认执行分派及从属语句执行。</summary>
public class DataExecutor : IDataExecutor
{
	#region 单例字段
	/// <summary>表示默认的数据语句执行器。</summary>
	public static readonly DataExecutor Instance = new();
	#endregion

	#region 成员字段
	private readonly object _syncRoot = new();
	private IDataExecutor<SelectStatement> _select;
	private IDataExecutor<DeleteStatement> _delete;
	private IDataExecutor<InsertStatement> _insert;
	private IDataExecutor<UpdateStatement> _update;
	private IDataExecutor<UpsertStatement> _upsert;
	private IDataExecutor<ExistStatement> _exist;
	private IDataExecutor<ExecutionStatement> _execution;
	private IDataExecutor<AggregateStatement> _aggregate;
	#endregion

	#region 构造函数
	/// <summary>初始化数据语句执行器。</summary>
	protected DataExecutor() { }
	#endregion

	#region 公共方法
	/// <summary>执行指定语句，并按执行结果继续处理从属语句。</summary>
	/// <param name="context">当前数据访问上下文。</param>
	/// <param name="statement">待执行的数据语句。</param>
	/// <param name="cancellation">指定的异步取消标记。</param>
	/// <returns>表示异步执行操作的任务。</returns>
	public async ValueTask ExecuteAsync(IDataAccessContext context, IStatementBase statement, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(statement);
		cancellation.ThrowIfCancellationRequested();

		var continued = statement switch
		{
			SelectStatement select => await this.GetExecutor(ref _select, this.CreateSelectExecutor).ExecuteAsync(context, select, cancellation),
			InsertStatement insert => await this.GetExecutor(ref _insert, this.CreateInsertExecutor).ExecuteAsync(context, insert, cancellation),
			UpdateStatement update => await this.GetExecutor(ref _update, this.CreateUpdateExecutor).ExecuteAsync(context, update, cancellation),
			UpsertStatement upsert => await this.GetExecutor(ref _upsert, this.CreateUpsertExecutor).ExecuteAsync(context, upsert, cancellation),
			DeleteStatement delete => await this.GetExecutor(ref _delete, this.CreateDeleteExecutor).ExecuteAsync(context, delete, cancellation),
			ExistStatement exist => await this.GetExecutor(ref _exist, this.CreateExistExecutor).ExecuteAsync(context, exist, cancellation),
			AggregateStatement aggregate => await this.GetExecutor(ref _aggregate, this.CreateAggregateExecutor).ExecuteAsync(context, aggregate, cancellation),
			ExecutionStatement execution => await this.GetExecutor(ref _execution, this.CreateExecuteExecutor).ExecuteAsync(context, execution, cancellation),
			_ => await ExecuteCommandAsync(context, statement, cancellation),
		};

		if(continued && statement.HasSlaves)
		{
			foreach(var slave in statement.Slaves)
				await this.ExecuteAsync(context, slave, cancellation);
		}
	}
	#endregion

	#region 保护方法
	/// <summary>创建查询语句执行器。</summary>
	/// <returns>返回由当前执行器缓存和并发复用的查询语句执行器。</returns>
	protected virtual IDataExecutor<SelectStatement> CreateSelectExecutor() => new DataSelectExecutor();

	/// <summary>创建删除语句执行器。</summary>
	/// <returns>返回由当前执行器缓存和并发复用的删除语句执行器。</returns>
	protected virtual IDataExecutor<DeleteStatement> CreateDeleteExecutor() => new DataDeleteExecutor();

	/// <summary>创建新增语句执行器。</summary>
	/// <returns>返回由当前执行器缓存和并发复用的新增语句执行器。</returns>
	protected virtual IDataExecutor<InsertStatement> CreateInsertExecutor() => new DataInsertExecutor();

	/// <summary>创建更新语句执行器。</summary>
	/// <returns>返回由当前执行器缓存和并发复用的更新语句执行器。</returns>
	protected virtual IDataExecutor<UpdateStatement> CreateUpdateExecutor() => new DataUpdateExecutor();

	/// <summary>创建增改语句执行器。</summary>
	/// <returns>返回由当前执行器缓存和并发复用的增改语句执行器。</returns>
	protected virtual IDataExecutor<UpsertStatement> CreateUpsertExecutor() => new DataUpsertExecutor();

	/// <summary>创建存在性检测语句执行器。</summary>
	/// <returns>返回由当前执行器缓存和并发复用的存在性检测语句执行器。</returns>
	protected virtual IDataExecutor<ExistStatement> CreateExistExecutor() => new DataExistExecutor();

	/// <summary>创建命令语句执行器。</summary>
	/// <returns>返回由当前执行器缓存和并发复用的命令语句执行器。</returns>
	protected virtual IDataExecutor<ExecutionStatement> CreateExecuteExecutor() => new DataExecuteExecutor();

	/// <summary>创建聚合语句执行器。</summary>
	/// <returns>返回由当前执行器缓存和并发复用的聚合语句执行器。</returns>
	protected virtual IDataExecutor<AggregateStatement> CreateAggregateExecutor() => new DataAggregateExecutor();
	#endregion

	#region 私有方法
	private IDataExecutor<TStatement> GetExecutor<TStatement>(ref IDataExecutor<TStatement> executor, Func<IDataExecutor<TStatement>> factory) where TStatement : IStatementBase
	{
		var result = Volatile.Read(ref executor);
		if(result != null)
			return result;

		lock(_syncRoot)
		{
			if(executor == null)
				Volatile.Write(ref executor, factory());

			return executor;
		}
	}

	private static async ValueTask<bool> ExecuteCommandAsync(IDataAccessContext context, IStatementBase statement, CancellationToken cancellation)
	{
		await using var command = context.Session.Build(context, statement);
		return await command.ExecuteNonQueryAsync(cancellation) > 0;
	}
	#endregion
}
