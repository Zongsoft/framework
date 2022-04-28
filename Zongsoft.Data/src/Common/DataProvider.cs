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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Metadata.Profiles;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Common
{
	public class DataProvider : IDataProvider
	{
		#region 事件声明
		public event EventHandler<DataAccessErrorEventArgs> Error;
		public event EventHandler<DataAccessEventArgs<IDataAccessContext>> Executing;
		public event EventHandler<DataAccessEventArgs<IDataAccessContext>> Executed;
		#endregion

		#region 成员字段
		private IDataExecutor _executor;
		private IDataMetadataManager _metadata;
		private IDataMultiplexer _multiplexer;
		#endregion

		#region 构造函数
		public DataProvider(string name)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = name.Trim();

			_executor = DataExecutor.Instance;
			_metadata = new MetadataFileManager(this.Name);
			_multiplexer = new DataMultiplexer(this.Name);
		}
		#endregion

		#region 公共属性
		public string Name { get; }

		public IDataExecutor Executor
		{
			get => _executor;
			set => _executor = value ?? throw new ArgumentNullException();
		}

		public IDataMetadataManager Metadata
		{
			get => _metadata;
			set => _metadata = value ?? throw new ArgumentNullException();
		}

		public IDataMultiplexer Multiplexer
		{
			get => _multiplexer;
			set => _multiplexer = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 执行方法
		public void Execute(IDataAccessContext context)
		{
			//激发“Executing”事件
			this.OnExecuting(context);

			try
			{
				object data = null;
				var mutation = context as IDataMutateContextBase;

				//保存当前操作的原始值
				if(mutation != null)
					data = mutation.Data;

				//进行具体的执行处理
				this.OnExecute(context);

				//尝试提交当前数据会话
				context.Session.Commit();

				//还原当前操作的原始值
				if(mutation != null)
					mutation.Data = data;
			}
			catch(Exception ex)
			{
				//尝试回滚当前数据会话
				context.Session.Rollback();

				//激发“Error”事件
				var handledException = this.OnError(context, ex);

				//如果“Error”事件处理完异常则退出
				if(handledException == null)
					return;

				//如果“Error”事件没有处理异常，则重抛以尽量避免异常过多嵌套
				if(object.ReferenceEquals(ex, handledException))
					throw;

				throw handledException is DataException ? handledException :
				      new DataException("The data execution error has occurred.", handledException);
			}

			//激发“Executed”事件
			this.OnExecuted(context);
		}
		#endregion

		#region 导入方法
		public void Import(DataImportContext context)
		{
			using var importer = context.Source.Driver.CreateImporter(context);
			importer.Import(context);
		}

		public ValueTask ImportAsync(DataImportContext context, CancellationToken cancellation)
		{
			using var importer = context.Source.Driver.CreateImporter(context);
			return importer.ImportAsync(context, cancellation);
		}
		#endregion

		#region 虚拟方法
		protected virtual void OnExecute(IDataAccessContext context)
		{
			//根据上下文生成对应执行语句集
			var statements = context.Source.Driver.Builder.Build(context);

			foreach(var statement in statements)
			{
				//由执行器执行语句
				_executor.Execute(context, statement);
			}
		}
		#endregion

		#region 激发事件
		protected virtual Exception OnError(IDataAccessContext context, Exception exception)
		{
			//通知数据驱动器发生了一个异常
			exception = context.Source.Driver.OnError(exception);

			//如果驱动器已经处理了异常则返回空
			if(exception == null)
				return null;

			var error = this.Error;

			if(error != null)
			{
				//构建错误事件参数对象
				var args = new DataAccessErrorEventArgs(context, exception);

				//激发“Error”事件
				error(this, args);

				//如果异常处理已经完成则返回空，否则返回处理后的异常
				return args.ExceptionHandled ? null : args.Exception;
			}

			return exception;
		}

		protected virtual void OnExecuting(IDataAccessContext context)
		{
			this.Executing?.Invoke(this, new DataAccessEventArgs(context));
		}

		protected virtual void OnExecuted(IDataAccessContext context)
		{
			this.Executed?.Invoke(this, new DataAccessEventArgs(context));
		}
		#endregion

		#region 嵌套子类
		private class DataAccessEventArgs : DataAccessEventArgs<IDataAccessContext>
		{
			public DataAccessEventArgs(IDataAccessContext context) : base(context) { }
		}

		private class DataExecutor : IDataExecutor
		{
			#region 单例字段
			public static readonly DataExecutor Instance = new DataExecutor();
			#endregion

			#region 成员字段
			private readonly DataSelectExecutor _select;
			private readonly DataDeleteExecutor _delete;
			private readonly DataInsertExecutor _insert;
			private readonly DataUpdateExecutor _update;
			private readonly DataUpsertExecutor _upsert;

			private readonly DataExistExecutor _exist;
			private readonly DataExecuteExecutor _execution;
			private readonly DataAggregateExecutor _aggregate;
			#endregion

			#region 私有构造
			private DataExecutor()
			{
				_select = new DataSelectExecutor();
				_delete = new DataDeleteExecutor();
				_insert = new DataInsertExecutor();
				_update = new DataUpdateExecutor();
				_upsert = new DataUpsertExecutor();

				_exist = new DataExistExecutor();
				_execution = new DataExecuteExecutor();
				_aggregate = new DataAggregateExecutor();
			}
			#endregion

			#region 执行方法
			public void Execute(IDataAccessContext context, IStatementBase statement)
			{
				var continued = statement switch
				{
					SelectStatement select => _select.Execute(context, select),
					DeleteStatement delete => _delete.Execute(context, delete),
					InsertStatement insert => _insert.Execute(context, insert),
					UpdateStatement update => _update.Execute(context, update),
					UpsertStatement upsert => _upsert.Execute(context, upsert),
					ExistStatement exist => _exist.Execute(context, exist),
					AggregateStatement aggregate => _aggregate.Execute(context, aggregate),
					ExecutionStatement execution => _execution.Execute(context, execution),
					_ => context.Session.Build(context, statement).ExecuteNonQuery() > 0,
				};

				if(continued && statement.HasSlaves)
				{
					foreach(var slave in statement.Slaves)
						this.Execute(context, slave);
				}
			}
			#endregion
		}

		private class DataMultiplexer : IDataMultiplexer
		{
			#region 成员字段
			private readonly string _name;
			private IDataSourceSelector _selector;
			#endregion

			#region 构造函数
			public DataMultiplexer(string name) => _name = name;
			#endregion

			#region 公共属性
			public IDataSourceProvider Provider => DataSourceProvider.Default;
			public IDataSourceSelector Selector { get; }
			#endregion

			#region 公共方法
			public IDataSource GetSource(IDataAccessContextBase context)
			{
				if(_selector == null)
				{
					var sources = this.Provider.GetSources(_name);

					if(sources == null || !sources.Any())
						throw new DataException($"No data sources for the '{_name}' data provider was found.");

					_selector = new DataSourceSelector(sources);
				}

				return _selector.GetSource(context) ?? throw new DataException("No matched data source for this data operation.");
			}
			#endregion
		}
		#endregion
	}
}
