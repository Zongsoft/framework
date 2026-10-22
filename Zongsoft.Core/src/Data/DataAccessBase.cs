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

/// <summary>表示数据访问的抽象基类。</summary>
[System.Reflection.DefaultMember(nameof(Filters))]
public abstract partial class DataAccessBase : IDataAccess, IAsyncDisposable
{
	#region 事件定义
	public event Func<object, DataAccessErrorEventArgs, CancellationToken, ValueTask> Error;
	public event Func<object, DataExecutedEventArgs, CancellationToken, ValueTask> Executed;
	public event Func<object, DataExecutingEventArgs, CancellationToken, ValueTask> Executing;
	public event Func<object, DataExistedEventArgs, CancellationToken, ValueTask> Existed;
	public event Func<object, DataExistingEventArgs, CancellationToken, ValueTask> Existing;
	public event Func<object, DataAggregatedEventArgs, CancellationToken, ValueTask> Aggregated;
	public event Func<object, DataAggregatingEventArgs, CancellationToken, ValueTask> Aggregating;
	public event Func<object, DataImportedEventArgs, CancellationToken, ValueTask> Imported;
	public event Func<object, DataImportingEventArgs, CancellationToken, ValueTask> Importing;
	public event Func<object, DataDeletedEventArgs, CancellationToken, ValueTask> Deleted;
	public event Func<object, DataDeletingEventArgs, CancellationToken, ValueTask> Deleting;
	public event Func<object, DataInsertedEventArgs, CancellationToken, ValueTask> Inserted;
	public event Func<object, DataInsertingEventArgs, CancellationToken, ValueTask> Inserting;
	public event Func<object, DataUpsertedEventArgs, CancellationToken, ValueTask> Upserted;
	public event Func<object, DataUpsertingEventArgs, CancellationToken, ValueTask> Upserting;
	public event Func<object, DataUpdatedEventArgs, CancellationToken, ValueTask> Updated;
	public event Func<object, DataUpdatingEventArgs, CancellationToken, ValueTask> Updating;
	public event Func<object, DataSelectedEventArgs, CancellationToken, ValueTask> Selected;
	public event Func<object, DataSelectingEventArgs, CancellationToken, ValueTask> Selecting;
	#endregion

	#region 常量定义
	private const int DISPOSED = -1;
	private const int DISPOSING = 1;
	#endregion

	#region 私有变量
	private volatile int _disposing;
	private readonly object _disposeSync = new();
	private Task _disposal;
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
			throw new InvalidOperationException(string.Format(Properties.Resources.DataAccess_NameMappingRequired_Message, type.FullName));

		return name;
	}

	protected virtual ValueTask OnFilteringAsync(IDataAccessContextBase context, CancellationToken cancellation) => _filters.InvokeFilteringAsync(context, cancellation);
	protected virtual ValueTask OnFilteredAsync(IDataAccessContextBase context, CancellationToken cancellation) => _filters.InvokeFilteredAsync(context, cancellation);
	#endregion

	#region 抽象方法
	protected abstract ISchemaParser CreateSchema();
	protected abstract IDataSequencer CreateSequencer();
	protected abstract DataExecuteContextBase CreateExecuteContext(string name, bool isScalar, Type resultType, IEnumerable<Parameter> parameters, IDataExecuteOptions options);
	protected abstract DataExistContextBase CreateExistContext(string name, ICondition criteria, IDataExistsOptions options);
	protected abstract DataAggregateContextBase CreateAggregateContext(string name, DataAggregate aggregate, ICondition criteria, IDataAggregateOptions options);
	protected abstract DataImportContextBase CreateImportContext(string name, object data, IEnumerable<string> members, IDataImportOptions options);
	protected abstract DataDeleteContextBase CreateDeleteContext(string name, ICondition criteria, ISchema schema, IDataDeleteOptions options);
	protected abstract DataInsertContextBase CreateInsertContext(string name, bool isMultiple, object data, ISchema schema, IDataInsertOptions options);
	protected abstract DataUpsertContextBase CreateUpsertContext(string name, bool isMultiple, object data, ISchema schema, IDataUpsertOptions options);
	protected abstract DataUpdateContextBase CreateUpdateContext(string name, object data, ICondition criteria, ISchema schema, IDataUpdateOptions options);
	protected abstract DataSelectContextBase CreateSelectContext(string name, Type modelType, ICondition criteria, Grouping grouping, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options);
	#endregion

	#region 激发事件
	protected virtual ValueTask OnErrorAsync(DataAccessErrorEventArgs args, CancellationToken cancellation) => this.RaiseAsync(this.Error, args, cancellation);
	protected virtual ValueTask OnExecutedAsync(DataExecuteContextBase context, CancellationToken cancellation) => this.RaiseAsync(this.Executed, new DataExecutedEventArgs(context), cancellation);
	protected virtual async ValueTask<bool> OnExecutingAsync(DataExecuteContextBase context, CancellationToken cancellation)
	{
		var args = new DataExecutingEventArgs(context);
		await this.RaiseAsync(this.Executing, args, cancellation);
		return args.Cancel;
	}

	protected virtual ValueTask OnExistedAsync(DataExistContextBase context, CancellationToken cancellation) => this.RaiseAsync(this.Existed, new DataExistedEventArgs(context), cancellation);
	protected virtual async ValueTask<bool> OnExistingAsync(DataExistContextBase context, CancellationToken cancellation)
	{
		var args = new DataExistingEventArgs(context);
		await this.RaiseAsync(this.Existing, args, cancellation);
		return args.Cancel;
	}

	protected virtual ValueTask OnAggregatedAsync(DataAggregateContextBase context, CancellationToken cancellation) => this.RaiseAsync(this.Aggregated, new DataAggregatedEventArgs(context), cancellation);
	protected virtual async ValueTask<bool> OnAggregatingAsync(DataAggregateContextBase context, CancellationToken cancellation)
	{
		var args = new DataAggregatingEventArgs(context);
		await this.RaiseAsync(this.Aggregating, args, cancellation);
		return args.Cancel;
	}

	protected virtual ValueTask OnImportedAsync(DataImportContextBase context, CancellationToken cancellation) => this.RaiseAsync(this.Imported, new DataImportedEventArgs(context), cancellation);
	protected virtual async ValueTask<bool> OnImportingAsync(DataImportContextBase context, CancellationToken cancellation)
	{
		var args = new DataImportingEventArgs(context);
		await this.RaiseAsync(this.Importing, args, cancellation);
		return args.Cancel;
	}

	protected virtual ValueTask OnDeletedAsync(DataDeleteContextBase context, CancellationToken cancellation) => this.RaiseAsync(this.Deleted, new DataDeletedEventArgs(context), cancellation);
	protected virtual async ValueTask<bool> OnDeletingAsync(DataDeleteContextBase context, CancellationToken cancellation)
	{
		var args = new DataDeletingEventArgs(context);
		await this.RaiseAsync(this.Deleting, args, cancellation);
		return args.Cancel;
	}

	protected virtual ValueTask OnInsertedAsync(DataInsertContextBase context, CancellationToken cancellation) => this.RaiseAsync(this.Inserted, new DataInsertedEventArgs(context), cancellation);
	protected virtual async ValueTask<bool> OnInsertingAsync(DataInsertContextBase context, CancellationToken cancellation)
	{
		var args = new DataInsertingEventArgs(context);
		await this.RaiseAsync(this.Inserting, args, cancellation);
		return args.Cancel;
	}

	protected virtual ValueTask OnUpsertedAsync(DataUpsertContextBase context, CancellationToken cancellation) => this.RaiseAsync(this.Upserted, new DataUpsertedEventArgs(context), cancellation);
	protected virtual async ValueTask<bool> OnUpsertingAsync(DataUpsertContextBase context, CancellationToken cancellation)
	{
		var args = new DataUpsertingEventArgs(context);
		await this.RaiseAsync(this.Upserting, args, cancellation);
		return args.Cancel;
	}

	protected virtual ValueTask OnUpdatedAsync(DataUpdateContextBase context, CancellationToken cancellation) => this.RaiseAsync(this.Updated, new DataUpdatedEventArgs(context), cancellation);
	protected virtual async ValueTask<bool> OnUpdatingAsync(DataUpdateContextBase context, CancellationToken cancellation)
	{
		var args = new DataUpdatingEventArgs(context);
		await this.RaiseAsync(this.Updating, args, cancellation);
		return args.Cancel;
	}

	protected virtual ValueTask OnSelectedAsync(DataSelectContextBase context, CancellationToken cancellation) => this.RaiseAsync(this.Selected, new DataSelectedEventArgs(context), cancellation);
	protected virtual async ValueTask<bool> OnSelectingAsync(DataSelectContextBase context, CancellationToken cancellation)
	{
		var args = new DataSelectingEventArgs(context);
		await this.RaiseAsync(this.Selecting, args, cancellation);
		return args.Cancel;
	}
	#endregion

	#region 处置方法
	public ValueTask DisposeAsync()
	{
		TaskCompletionSource completion;

		lock(_disposeSync)
		{
			if(_disposal != null)
				return new ValueTask(_disposal);

			_disposing = DISPOSING;
			completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			_disposal = completion.Task;
		}

		_ = this.CompleteDisposeAsync(completion);
		return new ValueTask(completion.Task);
	}

	protected virtual async ValueTask DisposeAsyncCore()
	{
		using var cancellation = Interlocked.Exchange(ref _cancellation, null);

		if(cancellation != null)
			await cancellation.CancelAsync();
	}

	private async Task CompleteDisposeAsync(TaskCompletionSource completion)
	{
		try
		{
			await this.DisposeAsyncCore();
			GC.SuppressFinalize(this);
			_disposing = DISPOSED;
			completion.TrySetResult();
		}
		catch(Exception exception)
		{
			_disposing = DISPOSED;
			completion.TrySetException(exception);
		}
	}
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private void EnsureDisposed() => ObjectDisposedException.ThrowIf(this.IsDisposed, this);

	private async ValueTask RaiseAsync<TArgs>(Func<object, TArgs, CancellationToken, ValueTask> handlers, TArgs args, CancellationToken cancellation)
	{
		if(handlers == null)
			return;

		foreach(Func<object, TArgs, CancellationToken, ValueTask> handler in handlers.GetInvocationList())
			await handler(this, args, cancellation);
	}

	private static IAsyncEnumerable<T> ToAsyncEnumerable<T>(object result, CancellationToken cancellation)
	{
		if(result is IAsyncEnumerable<T> enumerable)
			return enumerable;

		if(result != null && (Collections.Enumerable.IsAsyncEnumerable(result) || result is not IEnumerable))
			throw new InvalidOperationException(string.Format(Properties.Resources.Conversion_Type_Message, result.GetType(), typeof(IAsyncEnumerable<T>)));

		return Collections.Enumerable.EnumerateAsync<T>(result, cancellation);
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

			return sequence ?? throw new InvalidOperationException(Properties.Resources.DataAccess_MissingSequence_Message);
		}
		#endregion

		#region 私有方法
		private static Metadata.IDataEntitySimplexProperty GetProperty(string name, string field)
		{
			if(!Mapping.Entities.TryGetValue(name, out var entity))
				throw new InvalidOperationException(string.Format(Properties.Resources.DataEntity_NotFound_Message, name));

			if(!entity.Properties.TryGetValue(field, out var property))
				throw new InvalidOperationException(string.Format(Properties.Resources.DataEntity_PropertyNotFound_Message, name, field));

			if(!property.IsSimplex)
				throw new InvalidOperationException(string.Format(Properties.Resources.DataEntity_SequenceRequiresSimplex_Message, property.Entity.Name, property.Name));

			return (Metadata.IDataEntitySimplexProperty)property;
		}
		#endregion
	}
	#endregion
}
