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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Components
{
	public abstract class ExecutorBase<TContext> : IExecutor<TContext>, IHandler<TContext>, IHandler where TContext : IExecutorContext
	{
		#region 同步字段
		private readonly object _syncRoot = new object();
		#endregion

		#region 成员字段
		private ICollection<IExecutionFilter> _filters;
		#endregion

		#region 构造函数
		protected ExecutorBase() { }
		protected ExecutorBase(IHandler handler)
		{
			this.Handler = handler;
		}
		#endregion

		#region 公共属性
		public IHandler Handler { get; protected set; }
		public ICollection<IExecutionFilter> Filters
		{
			get
			{
				if(_filters == null)
				{
					lock(_syncRoot)
					{
						_filters ??= this.CreateFilters();
					}
				}

				return _filters;
			}
		}
		#endregion

		#region 执行方法
		public object Execute(TContext context)
		{
			var filters = _filters;

			if(filters != null)
			{
				foreach(var filter in filters)
					this.OnFiltering(filter, context);
			}

			var task = this.OnExecuteAsync(context);
			if(!task.IsCompleted)
				task.AsTask().Wait();

			if(filters != null)
			{
				foreach(var filter in filters)
					this.OnFiltered(filter, context);
			}

			return context.Result;
		}

		public async ValueTask<object> ExecuteAsync(TContext context, CancellationToken cancellation = default)
		{
			var filters = _filters;

			if(filters != null)
			{
				foreach(var filter in filters)
					this.OnFiltering(filter, context);
			}

			await this.OnExecuteAsync(context, cancellation);

			if(filters != null)
			{
				foreach(var filter in filters)
					this.OnFiltered(filter, context);
			}

			return context.Result;
		}
		#endregion

		#region 虚拟方法
		protected virtual bool CanExecute(TContext context) => context != null && (this.Handler?.CanHandle(context) ?? false);
		protected virtual ValueTask OnExecuteAsync(TContext context, CancellationToken cancellation = default)
		{
			var handler = this.Handler;

			if(handler != null && this.CanExecute(context))
				return handler.HandleAsync(this, context, context.HasParameters ? context.Parameters : null, cancellation);

			return ValueTask.FromCanceled(cancellation);
		}

		protected virtual void OnFiltered(IExecutionFilter filter, object context) => filter?.OnFiltered(context);
		protected virtual void OnFiltering(IExecutionFilter filter, object context) => filter?.OnFiltering(context);
		protected virtual ICollection<IExecutionFilter> CreateFilters() => new List<IExecutionFilter>();

		protected virtual TContext Convert(object request, IEnumerable<KeyValuePair<string, object>> parameters)
		{
			if(request is TContext context)
				return FillParameters(context, parameters);

			throw new ArgumentException($"The specified request parameter cannot be converted to '{typeof(TContext).FullName}' type.", nameof(request));
		}
		#endregion

		#region 显式实现
		object IExecutor.Execute(object context) => this.Execute(Convert(context, null));
		ValueTask<object> IExecutor.ExecuteAsync(object context, CancellationToken cancellation) => this.ExecuteAsync(Convert(context, null), cancellation);

		bool IHandler.CanHandle(object request, IEnumerable<KeyValuePair<string, object>> parameters) => this.CanExecute(Convert(request, parameters));
		bool IHandler<TContext>.CanHandle(TContext request, IEnumerable<KeyValuePair<string, object>> parameters) => this.CanExecute(FillParameters(request, parameters));
		async ValueTask IHandler.HandleAsync(object caller, object request, CancellationToken cancellation) => await this.ExecuteAsync(Convert(request, null), cancellation);
		async ValueTask IHandler.HandleAsync(object caller, object request, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation) => await this.ExecuteAsync(Convert(request, parameters), cancellation);
		async ValueTask IHandler<TContext>.HandleAsync(object caller, TContext request, CancellationToken cancellation) => await this.ExecuteAsync(request, cancellation);
		async ValueTask IHandler<TContext>.HandleAsync(object caller, TContext request, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation) => await this.ExecuteAsync(request, cancellation);
		#endregion

		#region 私有方法
		private static TContext FillParameters(TContext context, IEnumerable<KeyValuePair<string, object>> parameters)
		{
			if(context != null && parameters == null && parameters.Any())
			{
				foreach(var parameter in parameters)
					context.Parameters[parameter.Key] = parameter.Value;
			}

			return context;
		}
		#endregion
	}
}
