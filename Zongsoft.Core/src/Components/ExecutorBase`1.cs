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
	public abstract class ExecutorBase<TContext, TRequest> : IExecutor<TRequest>, IHandler<TRequest> where TContext : IExecutorContext<TRequest>
	{
		#region 构造函数
		protected ExecutorBase() => this.Filters = new List<IExecutionFilter>();
		protected ExecutorBase(IHandler handler)
		{
			this.Handler = handler;
			this.Filters = new List<IExecutionFilter>();
		}
		#endregion

		#region 公共属性
		public IHandler Handler { get; protected set; }
		public ICollection<IExecutionFilter> Filters { get; }
		#endregion

		#region 执行方法
		public void Execute(TRequest request, IEnumerable<KeyValuePair<string, object>> parameters = null) => this.Execute(this.CreateContext(request, parameters));
		protected void Execute(TContext context)
		{
			var filters = this.Filters;

			if(filters != null)
			{
				foreach(var filter in filters)
					this.OnFiltering(filter, context);
			}

			var task = this.OnExecuteAsync(context);
			if(!task.IsCompletedSuccessfully)
				task.GetAwaiter().GetResult();

			if(filters != null)
			{
				foreach(var filter in filters)
					this.OnFiltered(filter, context);
			}
		}

		public ValueTask ExecuteAsync(TRequest request, CancellationToken cancellation = default) => this.ExecuteAsync(this.CreateContext(request, null), cancellation);
		public ValueTask ExecuteAsync(TRequest request, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default) => this.ExecuteAsync(this.CreateContext(request, parameters), cancellation);
		protected async ValueTask ExecuteAsync(TContext context, CancellationToken cancellation = default)
		{
			var filters = this.Filters;

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
		}
		#endregion

		#region 虚拟方法
		protected abstract TContext CreateContext(TRequest request, IEnumerable<KeyValuePair<string, object>> parameters);
		protected virtual TContext CreateContext(object data, IEnumerable<KeyValuePair<string, object>> parameters) => data switch
		{
			TContext context => context,
			TRequest request => this.CreateContext(request, parameters),
			_ => throw new InvalidOperationException($"Unrecognized execution parameter: {data}"),
		};

		protected bool CanExecute(TRequest request, IEnumerable<KeyValuePair<string, object>> parameters) => this.CanExecute(this.CreateContext(request, parameters));
		protected virtual bool CanExecute(TContext context)
		{
			if(context == null || this.Handler == null)
				return false;

			var parameters = context.HasParameters ? context.Parameters : null;

			return this.Handler switch
			{
				IHandler<TRequest> handler => handler.CanHandle(context.Request, parameters),
				IHandler<TContext> handler => handler.CanHandle(context, parameters),
				IHandler<IExecutorContext> handler => handler.CanHandle(context, parameters),
				IHandler<IExecutorContext<TRequest>> handler => handler.CanHandle(context, parameters),
				IHandler handler => handler.CanHandle(context.Request, parameters),
				_ => false,
			};
		}

		protected virtual ValueTask OnExecuteAsync(TContext context, CancellationToken cancellation = default)
		{
			var parameters = context.HasParameters ? context.Parameters : null;

			if(this.CanExecute(context))
				return this.Handler switch
				{
					IHandler<TRequest> handler => handler.HandleAsync(this, context.Request, parameters, cancellation),
					IHandler<TContext> handler => handler.HandleAsync(this, context, parameters, cancellation),
					IHandler<IExecutorContext> handler => handler.HandleAsync(this, context, parameters, cancellation),
					IHandler<IExecutorContext<TRequest>> handler => handler.HandleAsync(this, context, parameters, cancellation),
					IHandler handler => handler.HandleAsync(this, context.Request, parameters, cancellation),
				};

			return ValueTask.CompletedTask;
		}

		protected virtual void OnFiltered(IExecutionFilter filter, TContext context) => filter?.OnFiltered(context);
		protected virtual void OnFiltering(IExecutionFilter filter, TContext context) => filter?.OnFiltering(context);
		#endregion

		#region 显式实现
		void IExecutor.Execute(object data, IEnumerable<KeyValuePair<string, object>> parameters)
		{
			if(data == null)
				this.Execute(default, parameters);

			switch(data)
			{
				case TRequest request:
					this.Execute(request, parameters);
					break;
				case TContext context:
					this.Execute(context);
					break;
			};
		}
		ValueTask IExecutor.ExecuteAsync(object data, CancellationToken cancellation) => data switch
		{
			TRequest request => this.ExecuteAsync(request, null, cancellation),
			TContext context => this.ExecuteAsync(context, cancellation),
			_ => data == null ? this.ExecuteAsync(default, null, cancellation) : throw new InvalidOperationException($"Unrecognized execution parameter: {data}"),
		};
		ValueTask IExecutor.ExecuteAsync(object data, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation) => data switch
		{
			TRequest request => this.ExecuteAsync(request, parameters, cancellation),
			TContext context => this.ExecuteAsync(context, cancellation),
			_ => data == null ? this.ExecuteAsync(default, parameters, cancellation) : throw new InvalidOperationException($"Unrecognized execution parameter: {data}"),
		};

		bool IHandler.CanHandle(object data, IEnumerable<KeyValuePair<string, object>> parameters) => data switch
		{
			TContext context => this.CanExecute(context),
			TRequest request => this.CanExecute(request, parameters),
			_ => false,
		};
		bool IHandler<TRequest>.CanHandle(TRequest request, IEnumerable<KeyValuePair<string, object>> parameters) => this.CanExecute(request, parameters);
		ValueTask IHandler.HandleAsync(object caller, object data, CancellationToken cancellation) => this.ExecuteAsync(this.CreateContext(data, null), cancellation);
		ValueTask IHandler.HandleAsync(object caller, object data, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation) => this.ExecuteAsync(CreateContext(data, parameters), cancellation);
		ValueTask IHandler<TRequest>.HandleAsync(object caller, TRequest request, CancellationToken cancellation) => this.ExecuteAsync(request, null, cancellation);
		ValueTask IHandler<TRequest>.HandleAsync(object caller, TRequest request, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation) => this.ExecuteAsync(request, parameters, cancellation);
		#endregion
	}
}
