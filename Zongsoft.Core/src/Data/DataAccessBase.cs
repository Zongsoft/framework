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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Extensions.Primitives;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Data;

/// <summary>
/// 表示数据访问的抽象基类。
/// </summary>
[System.Reflection.DefaultMember(nameof(Filters))]
public abstract partial class DataAccessBase : IDataAccess, IDisposable
{
	#region 事件定义
	public event EventHandler<DataAccessErrorEventArgs> Error;
	public event EventHandler<DataExecutedEventArgs> Executed;
	public event EventHandler<DataExecutingEventArgs> Executing;
	public event EventHandler<DataExistedEventArgs> Existed;
	public event EventHandler<DataExistingEventArgs> Existing;
	public event EventHandler<DataAggregatedEventArgs> Aggregated;
	public event EventHandler<DataAggregatingEventArgs> Aggregating;
	public event EventHandler<DataImportedEventArgs> Imported;
	public event EventHandler<DataImportingEventArgs> Importing;
	public event EventHandler<DataDeletedEventArgs> Deleted;
	public event EventHandler<DataDeletingEventArgs> Deleting;
	public event EventHandler<DataInsertedEventArgs> Inserted;
	public event EventHandler<DataInsertingEventArgs> Inserting;
	public event EventHandler<DataUpsertedEventArgs> Upserted;
	public event EventHandler<DataUpsertingEventArgs> Upserting;
	public event EventHandler<DataUpdatedEventArgs> Updated;
	public event EventHandler<DataUpdatingEventArgs> Updating;
	public event EventHandler<DataSelectedEventArgs> Selected;
	public event EventHandler<DataSelectingEventArgs> Selecting;
	#endregion

	#region 常量定义
	private const int DISPOSED = -1;
	private const int DISPOSING = 1;
	#endregion

	#region 私有变量
	private volatile int _disposing;
	#endregion

	#region 成员字段
	private string _name;
	private ISchemaParser _schema;
	private IDataSequencer _sequencer;
	private DataAccessFilterCollection _filters;
	#endregion

	#region 构造函数
	protected DataAccessBase(string name)
	{
		_name = name ?? string.Empty;
		_filters = new DataAccessFilterCollection();
	}
	#endregion

	#region 公共属性
	/// <summary>获取数据访问器的名称，通常为应用、模块名。</summary>
	public string Name
	{
		get => _name;
		set => _name = value ?? string.Empty;
	}

	/// <summary>获取数据模式解析器。</summary>
	public ISchemaParser Schema => _schema ??= this.CreateSchema();

	/// <summary>获取或设置数据序号生成程序。</summary>
	public IDataSequencer Sequencer
	{
		get => _sequencer ??= this.CreateSequencer();
		set => _sequencer = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>获取数据访问过滤器集合。</summary>
	public ICollection<object> Filters => _filters;
	#endregion

	#region 处置通知
	public bool IsDisposed => _disposing == DISPOSED;

	private CancellationTokenSource _cancellation = new();
	/// <summary>获取当前对象的处置通知令牌。</summary>
	public IChangeToken Disposed => Notification.GetToken(_cancellation);
	#endregion

	#region 虚拟方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private string GetName<T>() => this.GetName(typeof(T));
	protected virtual string GetName(Type type)
	{
		var name = Model.Naming.Get(type);

		if(string.IsNullOrEmpty(name))
			throw new InvalidOperationException($"Missing data access name mapping of the '{type.FullName}' type.");

		return name;
	}

	protected virtual void OnFiltering(IDataAccessContextBase context) => _filters.InvokeFiltering(context);
	protected virtual void OnFiltered(IDataAccessContextBase context) => _filters.InvokeFiltered(context);
	#endregion

	#region 抽象方法
	protected abstract ISchemaParser CreateSchema();
	protected abstract IDataSequencer CreateSequencer();
	protected abstract DataExecuteContextBase CreateExecuteContext(string name, bool isScalar, Type resultType, IEnumerable<Parameter> parameters, IDataExecuteOptions options);
	protected abstract DataExistContextBase CreateExistContext(string name, ICondition criteria, IDataExistsOptions options);
	protected abstract DataAggregateContextBase CreateAggregateContext(string name, DataAggregate aggregate, ICondition criteria, IDataAggregateOptions options);
	protected abstract DataImportContextBase CreateImportContext(string name, IEnumerable data, IEnumerable<string> members, IDataImportOptions options);
	protected abstract DataDeleteContextBase CreateDeleteContext(string name, ICondition criteria, ISchema schema, IDataDeleteOptions options);
	protected abstract DataInsertContextBase CreateInsertContext(string name, bool isMultiple, object data, ISchema schema, IDataInsertOptions options);
	protected abstract DataUpsertContextBase CreateUpsertContext(string name, bool isMultiple, object data, ISchema schema, IDataUpsertOptions options);
	protected abstract DataUpdateContextBase CreateUpdateContext(string name, object data, ICondition criteria, ISchema schema, IDataUpdateOptions options);
	protected abstract DataSelectContextBase CreateSelectContext(string name, Type entityType, ICondition criteria, Grouping grouping, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options);
	#endregion

	#region 激发事件
	protected virtual void OnError(DataAccessErrorEventArgs args) => this.Error?.Invoke(this, args);
	protected virtual void OnExecuted(DataExecuteContextBase context) => this.Executed?.Invoke(this, new DataExecutedEventArgs(context));
	protected virtual bool OnExecuting(DataExecuteContextBase context)
	{
		var e = this.Executing;
		if(e == null)
			return false;

		var args = new DataExecutingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnExisted(DataExistContextBase context) => this.Existed?.Invoke(this, new DataExistedEventArgs(context));
	protected virtual bool OnExisting(DataExistContextBase context)
	{
		var e = this.Existing;
		if(e == null)
			return false;

		var args = new DataExistingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnAggregated(DataAggregateContextBase context) => this.Aggregated?.Invoke(this, new DataAggregatedEventArgs(context));
	protected virtual bool OnAggregating(DataAggregateContextBase context)
	{
		var e = this.Aggregating;
		if(e == null)
			return false;

		var args = new DataAggregatingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnImported(DataImportContextBase context) => this.Imported?.Invoke(this, new DataImportedEventArgs(context));
	protected virtual bool OnImporting(DataImportContextBase context)
	{
		var e = this.Importing;
		if(e == null)
			return false;

		var args = new DataImportingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnDeleted(DataDeleteContextBase context) => this.Deleted?.Invoke(this, new DataDeletedEventArgs(context));
	protected virtual bool OnDeleting(DataDeleteContextBase context)
	{
		var e = this.Deleting;
		if(e == null)
			return false;

		var args = new DataDeletingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnInserted(DataInsertContextBase context) => this.Inserted?.Invoke(this, new DataInsertedEventArgs(context));
	protected virtual bool OnInserting(DataInsertContextBase context)
	{
		var e = this.Inserting;
		if(e == null)
			return false;

		var args = new DataInsertingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnUpserted(DataUpsertContextBase context) => this.Upserted?.Invoke(this, new DataUpsertedEventArgs(context));
	protected virtual bool OnUpserting(DataUpsertContextBase context)
	{
		var e = this.Upserting;
		if(e == null)
			return false;

		var args = new DataUpsertingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnUpdated(DataUpdateContextBase context) => this.Updated?.Invoke(this, new DataUpdatedEventArgs(context));
	protected virtual bool OnUpdating(DataUpdateContextBase context)
	{
		var e = this.Updating;
		if(e == null)
			return false;

		var args = new DataUpdatingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnSelected(DataSelectContextBase context) => this.Selected?.Invoke(this, new DataSelectedEventArgs(context));
	protected virtual bool OnSelecting(DataSelectContextBase context)
	{
		var e = this.Selecting;
		if(e == null)
			return false;

		var args = new DataSelectingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}
	#endregion

	#region 处置方法
	public void Dispose()
	{
		var disposing = Interlocked.CompareExchange(ref _disposing, DISPOSING, 0);
		if(disposing != 0)
			return;

		try
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		finally
		{
			_disposing = DISPOSED;
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		var cancellation = Interlocked.Exchange(ref _cancellation, null);

		if(cancellation != null)
		{
			cancellation.Cancel();
			cancellation.Dispose();
		}
	}
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	static int GetRecordsAffected(object value) => value == null || System.Convert.IsDBNull(value) ? 0 : (int)System.Convert.ChangeType(value, typeof(int));
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static IEnumerable<T> ToEnumerable<T>(object result) => Collections.Enumerable.Enumerate<T>(result);
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static IAsyncEnumerable<T> ToAsyncEnumerable<T>(object result, CancellationToken cancellation) => Collections.Enumerable.EnumerateAsync<T>(result, cancellation);
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private void EnsureDisposed()
	{
		if(this.IsDisposed)
			throw new ObjectDisposedException($"[{this.GetType().Name}]{_name}");
	}
	#endregion

	#region 嵌套子类
	protected abstract class DataSequencerBase(DataAccessBase accessor) : IDataSequencer
	{
		#region 成员字段
		private ISequenceBase _sequence;
		private readonly DataAccessBase _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
		#endregion

		#region 公共属性
		public string Name => _accessor.Name;
		public DataAccessBase Accessor => _accessor;
		public ISequenceBase Sequence
		{
			get => _sequence ??= Common.Sequence.Variate(this.GetSequence());
			set => _sequence = value ?? throw new ArgumentNullException(nameof(value));
		}
		#endregion

		#region 公共方法
		public long Increase(string name, string field, int interval = 1) => this.Increase(GetProperty(name, field), interval);
		public abstract long Increase(Metadata.IDataEntitySimplexProperty property, int interval = 1);

		public ValueTask<long> IncreaseAsync(string name, string field, CancellationToken cancellation = default) => this.IncreaseAsync(name, field, 1, cancellation);
		public ValueTask<long> IncreaseAsync(string name, string field, int interval, CancellationToken cancellation = default) => this.IncreaseAsync(GetProperty(name, field), interval, cancellation);

		public ValueTask<long> IncreaseAsync(Metadata.IDataEntitySimplexProperty property, CancellationToken cancellation = default) => this.IncreaseAsync(property, 1, cancellation);
		public abstract ValueTask<long> IncreaseAsync(Metadata.IDataEntitySimplexProperty property, int interval, CancellationToken cancellation = default);
		#endregion

		#region 虚拟方法
		protected virtual ISequenceBase GetSequence()
		{
			var provider = ApplicationContext.Current.Services.ResolveRequired<IServiceProvider<ISequenceBase>>();

			var sequence = provider.GetService(this.Name);
			if(sequence == null && !string.IsNullOrEmpty(this.Name))
				sequence = provider.GetService(string.Empty);

			return sequence ?? throw new InvalidOperationException($"Missing required Sequence service.");
		}
		#endregion

		#region 私有方法
		private static Metadata.IDataEntitySimplexProperty GetProperty(string name, string field)
		{
			if(!Mapping.Entities.TryGetValue(name, out var entity))
				throw new InvalidOperationException($"The entity specified with the name '{name}' does not exist.");

			if(!entity.Properties.TryGetValue(field, out var property))
				throw new InvalidOperationException($"The '{name}' entity does not contains the '{field}' property.");

			if(!property.IsSimplex)
				throw new InvalidOperationException($"This '{property.Entity.Name}.{property.Name}' property is not a simplex property and does not support sequence feature.");

			return (Metadata.IDataEntitySimplexProperty)property;
		}
		#endregion
	}
	#endregion
}
