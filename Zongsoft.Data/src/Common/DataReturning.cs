/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Copyright (C) 2015-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Data;
using System.Runtime.CompilerServices;

using Zongsoft.Data.Features;

namespace Zongsoft.Data.Common;

internal static class DataReturning
{
	#region 静态字段
	private static readonly ConditionalWeakTable<IDataMutateContext, Result> _results = new();
	#endregion

	#region 静态方法
	/// <summary>获取调用方与当前操作合并后的返回列，并按需初始化返回结果暂存区。</summary>
	/// <param name="context">指定的写入操作上下文。</param>
	/// <param name="columns">输出合并去重后的返回列集合；未请求返回列时为空集合。</param>
	/// <returns>如果存在请求返回的列则返回真，否则返回假。</returns>
	/// <remarks>首次初始化时验证操作自身请求的返回值能力及映射字段，不修改调用方或当前操作的返回列。</remarks>
	/// <exception cref="ArgumentNullException"><paramref name="context" /> 为空。</exception>
	/// <exception cref="NotSupportedException">当前数据源不支持操作自身请求的返回值种类。</exception>
	/// <exception cref="DataArgumentException">操作自身请求的返回列未映射或不是简单属性。</exception>
	public static bool TryGet(IDataMutateContext context, out Returning.ColumnCollection columns)
	{
		var result = _results.GetValue(context, static context => new Result(context));
		columns = result.Columns;
		return columns.Count > 0;
	}

	/// <summary>准备当前写入操作的返回列和结果暂存区。</summary>
	/// <param name="context">指定的数据访问上下文。</param>
	/// <remarks>在执行数据库语句之前调用，初始化时验证操作自身的返回请求；非写入上下文不做任何处理。</remarks>
	/// <exception cref="NotSupportedException">当前数据源不支持操作自身请求的返回值种类。</exception>
	/// <exception cref="DataArgumentException">操作自身请求的返回列未映射或不是简单属性。</exception>
	public static void Prepare(IDataAccessContext context)
	{
		if(context is IDataMutateContext mutation)
			TryGet(mutation, out _);
	}

	/// <summary>将当前记录按调用方与操作各自的返回列分别填入暂存区。</summary>
	/// <param name="context">指定的数据访问上下文。</param>
	/// <param name="record">已定位到当前行的数据记录。</param>
	/// <remarks>本方法不会推进或释放读取器，也不会立即将暂存行追加到可见的返回结果；非写入上下文不做任何处理。</remarks>
	public static void Populate(IDataAccessContext context, IDataRecord record)
	{
		if(context is IDataMutateContext mutation)
			_results.GetValue(mutation, static context => new Result(context)).Populate(record);
	}

	/// <summary>验证当前操作自身请求的返回结果是否完整。</summary>
	/// <param name="context">指定的数据访问上下文。</param>
	/// <remarks>应在会话提交前调用；非写入上下文或没有暂存状态时不做任何处理。</remarks>
	/// <exception cref="DataException">操作请求了返回列且影响行数大于零，但未收集到操作返回行。</exception>
	/// <exception cref="DataArgumentException">操作返回行中缺少请求的列。</exception>
	public static void Validate(IDataAccessContext context)
	{
		if(context is IDataMutateContext mutation && _results.TryGetValue(mutation, out var result))
			result.Validate(mutation.Count);
	}

	/// <summary>将暂存的返回行分别追加到调用方与当前操作的返回结果，并移除暂存状态。</summary>
	/// <param name="context">指定的数据访问上下文。</param>
	/// <remarks>应在会话提交完成后调用；非写入上下文或没有暂存状态时不做任何处理。环境事务的最终提交仍由调用方决定。</remarks>
	public static void Complete(IDataAccessContext context)
	{
		if(context is not IDataMutateContext mutation || !_results.TryGetValue(mutation, out var result))
			return;

		try { result.Complete(); }
		finally { Discard(context); }
	}

	/// <summary>移除当前操作的返回值暂存状态，丢弃尚未追加的返回行。</summary>
	/// <param name="context">指定的数据访问上下文。</param>
	/// <remarks>不修改已追加到调用方或当前操作的返回结果；非写入上下文或没有暂存状态时不做任何处理。</remarks>
	public static void Discard(IDataAccessContext context)
	{
		if(context is IDataMutateContext mutation)
			_results.Remove(mutation);
	}
	#endregion

	#region 嵌套子类
	private sealed class Result
	{
		private readonly Returning _caller;
		private readonly Returning _operation;
		private readonly Returning.RowCollection _callerRows;
		private readonly Returning.RowCollection _operationRows;

		public Result(IDataMutateContext context)
		{
			_caller = context.Options.HasReturning(out var returning) ? returning : null;
			_operation = context.Returning;
			this.Columns = new(null, (_caller?.Columns.AsEnumerable() ?? []).Concat(_operation.Columns).Distinct());

			if(_caller != null)
				_callerRows = new(_caller.Columns);

			if(_operation.Columns.Count > 0)
			{
				var feature = context.Source.Features.Get<IReturningFeature>();

				foreach(var column in _operation.Columns)
				{
					if(feature?.Supports(context.Method, column.Kind) != true)
						throw new NotSupportedException();

					if(!context.Entity.Properties.TryGetValue(column.Name, out var property) || !property.IsSimplex)
						throw new DataArgumentException(column.Name);
				}

				_operationRows = new(_operation.Columns);
			}
		}

		public Returning.ColumnCollection Columns { get; }

		public void Populate(IDataRecord record)
		{
			//独立读取两份投影，防止调用方访问到扩展列或共享可变的值数组。
			_callerRows?.Populate(record);
			_operationRows?.Populate(record);
		}

		public void Validate(int count)
		{
			if(_operationRows == null)
				return;

			if(count > 0 && _operationRows.IsEmpty)
				throw new DataException(Properties.Resources.DataProvider_ExecutionFailed_Message);

			foreach(var row in _operationRows)
			{
				foreach(var column in _operation.Columns)
				{
					if(!row.TryGetValue(column.Name, column.Kind, out _))
						throw new DataArgumentException(column.Name);
				}
			}
		}

		public void Complete()
		{
			if(_callerRows != null)
				_caller.Rows.AddRange(_callerRows);

			if(_operationRows != null)
				_operation.Rows.AddRange(_operationRows);
		}
	}
	#endregion
}
