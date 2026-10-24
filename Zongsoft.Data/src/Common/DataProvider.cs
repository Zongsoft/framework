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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Zongsoft.Configuration;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Common;

public class DataProvider : IDataProvider
{
	#region 事件声明
	public event Func<object, DataAccessErrorEventArgs, CancellationToken, ValueTask> Error;
	public event Func<object, DataAccessEventArgs<IDataAccessContext>, CancellationToken, ValueTask> Executing;
	public event Func<object, DataAccessEventArgs<IDataAccessContext>, CancellationToken, ValueTask> Executed;
	#endregion

	#region 成员字段
	private IDataExecutor _executor;
	private IDataMultiplexer _multiplexer;
	#endregion

	#region 构造函数
	public DataProvider(string name, IEnumerable<IConnectionSettings> settings) : this(name, new DataSourceProvider(settings)) { }
	private DataProvider(string name, IDataSourceProvider sourceProvider)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));
		if(sourceProvider == null)
			throw new ArgumentNullException(nameof(sourceProvider));

		this.Name = name.Trim();

		_executor = DataExecutor.Instance;
		_multiplexer = new DataMultiplexer(this.Name, sourceProvider);
	}
	#endregion

	#region 静态方法
	/// <summary>使用指定的数据源提供程序创建数据提供程序。</summary>
	/// <param name="name">指定数据提供程序的名称。</param>
	/// <param name="sourceProvider">指定的数据源提供程序。</param>
	/// <returns>返回创建的数据提供程序。</returns>
	public static DataProvider Create(string name, IDataSourceProvider sourceProvider) => new(name, sourceProvider);
	#endregion

	#region 公共属性
	public string Name { get; }

	public IDataExecutor Executor
	{
		get => _executor;
		set => _executor = value ?? throw new ArgumentNullException();
	}

	public IDataMultiplexer Multiplexer
	{
		get => _multiplexer;
		set => _multiplexer = value ?? throw new ArgumentNullException();
	}
	#endregion

	#region 执行方法
	public async ValueTask ExecuteAsync(IDataAccessContext context, CancellationToken cancellation)
	{
		//激发“Executing”事件
		await this.OnExecutingAsync(context, cancellation);

		try
		{
			object data = null;
			var mutation = context as IDataMutateContextBase;

			//保存当前操作的原始值
			if(mutation != null)
				data = mutation.Data;

			//进行具体的执行处理
			await this.OnExecuteAsync(context, cancellation);

			//尝试提交当前数据会话
			cancellation.ThrowIfCancellationRequested();
			await context.Session.CommitAsync(cancellation);
			DataReturning.Complete(context);

			//还原当前操作的原始值
			if(mutation != null)
				mutation.Data = data;
		}
		catch(Exception ex)
		{
			DataReturning.Discard(context);
			if(context is IDataMutateContext mutation)
				mutation.Count = 0;

			//尝试回滚当前数据会话
			await context.Session.RollbackAsync(CancellationToken.None);

			//激发“Error”事件
			var handledException = await this.OnErrorAsync(context, ex, CancellationToken.None);

			//如果“Error”事件处理完异常则退出
			if(handledException == null)
				return;

			//如果“Error”事件没有处理异常，则重抛以尽量避免异常过多嵌套
			if(object.ReferenceEquals(ex, handledException))
				throw;

			throw handledException is DataException ? handledException :
				  new DataException(Properties.Resources.DataProvider_ExecutionFailed_Message, handledException);
		}

		//激发“Executed”事件
		await this.OnExecutedAsync(context, cancellation);
	}
	#endregion

	#region 导入方法
	public async ValueTask ImportAsync(DataImportContext context, CancellationToken cancellation)
	{
		//激发“Executing”事件
		await this.OnExecutingAsync(context, cancellation);

		try
		{
			await context.Source.Driver.Importer.ImportAsync(context, cancellation);

			//尝试提交当前数据会话
			await context.Session.CommitAsync(cancellation);
		}
		catch(Exception ex)
		{
			//尝试回滚当前数据会话
			await context.Session.RollbackAsync(CancellationToken.None);

			//激发“Error”事件
			var handledException = await this.OnErrorAsync(context, ex, CancellationToken.None);

			//如果“Error”事件处理完异常则退出
			if(handledException == null)
				return;

			//如果“Error”事件没有处理异常，则重抛以尽量避免异常过多嵌套
			if(object.ReferenceEquals(ex, handledException))
				throw;

			throw handledException is DataException ? handledException :
				  new DataException(Properties.Resources.DataProvider_ExecutionFailed_Message, handledException);
		}

		//激发“Executed”事件
		await this.OnExecutedAsync(context, cancellation);
	}
	#endregion

	#region 虚拟方法
	protected virtual async ValueTask OnExecuteAsync(IDataAccessContext context, CancellationToken cancellation)
	{
		if(context is IDataMutateContext mutation && mutation.IsMultiple())
		{
			var data = mutation.Data;

			try
			{
				await foreach(var batch in GetBatchesAsync(data, cancellation).ConfigureAwait(false))
				{
					mutation.Data = batch;
					await this.ExecuteStatementsAsync(context, cancellation);
				}
			}
			finally
			{
				mutation.Data = data;
			}
		}
		else
			await this.ExecuteStatementsAsync(context, cancellation);
	}
	#endregion

	#region 激发事件
	protected virtual async ValueTask<Exception> OnErrorAsync(IDataAccessContext context, Exception exception, CancellationToken cancellation)
	{
		//通知数据驱动器发生了一个异常
		exception = await context.Source.Driver.OnErrorAsync(context, exception, cancellation);

		//如果驱动器已经处理了异常则返回空
		if(exception == null)
			return null;

		var error = this.Error;

		if(error != null)
		{
			//构建错误事件参数对象
			var args = new DataAccessErrorEventArgs(context, exception);

			//激发“Error”事件
			await this.RaiseAsync(error, args, cancellation);

			//如果异常处理已经完成则返回空，否则返回处理后的异常
			return args.ExceptionHandled ? null : args.Exception;
		}

		return exception;
	}

	protected virtual ValueTask OnExecutingAsync(IDataAccessContext context, CancellationToken cancellation) => this.RaiseAsync(this.Executing, new DataAccessEventArgs(context), cancellation);
	protected virtual ValueTask OnExecutedAsync(IDataAccessContext context, CancellationToken cancellation) => this.RaiseAsync(this.Executed, new DataAccessEventArgs(context), cancellation);
	#endregion

	#region 私有方法
	private static async IAsyncEnumerable<IList> GetBatchesAsync(object data, [EnumeratorCancellation] CancellationToken cancellation)
	{
		const int CAPACITY = 256;

		var batch = Utility.CreateList(data);

		await foreach(var item in Utility.EnumerateAsync(data).WithCancellation(cancellation).ConfigureAwait(false))
		{
			cancellation.ThrowIfCancellationRequested();

			batch.Add(item);
			if(batch.Count < CAPACITY)
				continue;

			yield return batch;
			batch = Utility.CreateList(batch);
		}

		if(batch.Count > 0)
			yield return batch;
	}

	private async ValueTask ExecuteStatementsAsync(IDataAccessContext context, CancellationToken cancellation)
	{
		//根据上下文生成对应执行语句集
		var statements = context.Source.Driver.Builder.BuildAsync(context, cancellation);

		await foreach(var statement in statements.ConfigureAwait(false))
		{
			//由执行器执行语句
			await _executor.ExecuteAsync(context, statement, cancellation);
		}
	}

	private async ValueTask RaiseAsync<TArgs>(Func<object, TArgs, CancellationToken, ValueTask> handlers, TArgs args, CancellationToken cancellation)
	{
		if(handlers != null)
		{
			foreach(Func<object, TArgs, CancellationToken, ValueTask> handler in handlers.GetInvocationList())
				await handler(this, args, cancellation);
		}
	}
	#endregion

	#region 嵌套子类
	private class DataAccessEventArgs(IDataAccessContext context) : DataAccessEventArgs<IDataAccessContext>(context)
	{
	}

	private sealed class DataExecutor : IDataExecutor
	{
		public static readonly DataExecutor Instance = new();

		public ValueTask ExecuteAsync(IDataAccessContext context, IStatementBase statement, CancellationToken cancellation = default) =>
			context.Source.Driver.Executor.ExecuteAsync(context, statement, cancellation);
	}

	private class DataMultiplexer : IDataMultiplexer
	{
		#region 成员字段
		private readonly string _name;
		private readonly IDataSourceProvider _provider;
		private Lazy<IDataSourceSelector> _selector;
		#endregion

		#region 构造函数
		public DataMultiplexer(string name, IDataSourceProvider sourceProvider)
		{
			_name = name;
			_provider = sourceProvider;
			_selector = this.CreateSelector();
		}
		#endregion

		#region 公共属性
		public IDataSourceProvider Provider => _provider;
		public IDataSourceSelector Selector
		{
			get
			{
				//尚未完成初始化时不应由该属性触发数据源解析。
				var selector = Volatile.Read(ref _selector);
				return selector.IsValueCreated ? selector.Value : null;
			}
		}
		#endregion

		#region 公共方法
		public IDataSource GetSource(IDataAccessContextBase context)
		{
			var initializer = Volatile.Read(ref _selector);
			IDataSourceSelector selector;

			try
			{
				selector = initializer.Value;
			}
			catch
			{
				//Lazy<T>会缓存初始化异常，故替换失败的实例以允许后续调用重试。
				Interlocked.CompareExchange(ref _selector, this.CreateSelector(), initializer);
				throw;
			}

			return selector.GetSource(context) ?? throw new DataException(Properties.Resources.DataProvider_SourceNotMatched_Message);
		}
		#endregion

		#region 私有方法
		private Lazy<IDataSourceSelector> CreateSelector() => new(() =>
		{
			var sources = this.Provider.GetSources(_name).ToArray();

			if(sources == null || sources.Length == 0)
				throw new DataException(string.Format(Properties.Resources.DataProvider_SourcesNotFound_Message, _name));

			return new DataSourceSelector(sources);
		}, LazyThreadSafetyMode.ExecutionAndPublication);
		#endregion
	}
	#endregion
}
