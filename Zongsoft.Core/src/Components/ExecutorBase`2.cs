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
	public abstract class ExecutorBase<TArgument, TResult> : IExecutor<TArgument, TResult>, IHandler<TArgument, TResult>, IFilterable<IExecutorContext>
	{
		#region 构造函数
		protected ExecutorBase() => this.Filters = new List<IFilter<IExecutorContext>>();
		protected ExecutorBase(IHandlerLocator<IExecutorContext> locator)
		{
			this.Locator = locator;
			this.Filters = new List<IFilter<IExecutorContext>>();
		}
		#endregion

		#region 公共属性
		public IHandlerLocator<IExecutorContext> Locator { get; }
		public ICollection<IFilter<IExecutorContext>> Filters { get; }
		#endregion

		#region 执行方法
		public TResult Execute(TArgument argument, IEnumerable<KeyValuePair<string, object>> parameters = null) => this.Execute(this.CreateContext(argument, parameters));
		protected TResult Execute(IExecutorContext<TArgument, TResult> context)
		{
			var task = this.ExecuteAsync(context, default);
			return task.IsCompletedSuccessfully ?
				task.Result :
				task.GetAwaiter().GetResult();
		}

		public ValueTask<TResult> ExecuteAsync(TArgument argument, CancellationToken cancellation = default) => this.ExecuteAsync(this.CreateContext(argument, null), cancellation);
		public ValueTask<TResult> ExecuteAsync(TArgument argument, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default) => this.ExecuteAsync(this.CreateContext(argument, parameters), cancellation);
		protected async ValueTask<TResult> ExecuteAsync(IExecutorContext<TArgument, TResult> context, CancellationToken cancellation = default)
		{
			var filters = this.Filters;

			if(filters != null)
			{
				foreach(var filter in filters)
					await this.OnFiltering(filter, context, cancellation);
			}

			var result = await this.OnExecuteAsync(context, cancellation);

			if(filters != null)
			{
				foreach(var filter in filters)
					await this.OnFiltered(filter, context, cancellation);
			}

			return result;
		}
		#endregion

		#region 虚拟方法
		protected abstract IExecutorContext<TArgument, TResult> CreateContext(TArgument argument, IEnumerable<KeyValuePair<string, object>> parameters);
		protected virtual IExecutorContext<TArgument, TResult> CreateContext(object data, IEnumerable<KeyValuePair<string, object>> parameters) => data switch
		{
			IExecutorContext<TArgument, TResult> context => context,
			TArgument argument => this.CreateContext(argument, parameters),
			_ => throw new InvalidOperationException($"Unrecognized execution parameter: {data}"),
		};
		protected virtual IHandler GetHandler(IExecutorContext<TArgument, TResult> context) => this.Locator?.Locate(context);

		protected bool CanExecute(TArgument argument, IEnumerable<KeyValuePair<string, object>> parameters) => this.CanExecute(this.CreateContext(argument, parameters));
		protected bool CanExecute(IExecutorContext<TArgument, TResult> context) => this.CanExecute(context, out _);
		protected virtual bool CanExecute(IExecutorContext<TArgument, TResult> context, out IHandler handler)
		{
			if(context == null)
			{
				handler = null;
				return false;
			}

			handler = this.GetHandler(context);
			if(handler == null)
				return false;

			var parameters = context.HasParameters ? context.Parameters : null;

			return handler switch
			{
				IHandler<TArgument> matched => matched.CanHandle(context.Argument, parameters),
				IHandler<TArgument, TResult> matched => matched.CanHandle(context.Argument, parameters),
				IHandler<IExecutorContext> matched => matched.CanHandle(context, parameters),
				IHandler<IExecutorContext<TArgument>> matched => matched.CanHandle(context, parameters),
				IHandler<IExecutorContext<TArgument,	TArgument>> matched => matched.CanHandle(context, parameters),
				IHandler matched => matched.CanHandle(context.Argument, parameters),
				_ => false,
			};
		}

		protected virtual async ValueTask<TResult> OnExecuteAsync(IExecutorContext<TArgument, TResult> context, CancellationToken cancellation = default)
		{
			var parameters = context.HasParameters ? context.Parameters : null;

			if(this.CanExecute(context, out var executor))
			{
				switch(executor)
				{
					case IHandler<TArgument> handler:
						await handler.HandleAsync(this, context.Argument, parameters, cancellation);
						break;
					case IHandler<TArgument, TResult> handler:
						context.Result = await handler.HandleAsync(this, context.Argument, parameters, cancellation);
						break;
					case IHandler<IExecutorContext> handler:
						await handler.HandleAsync(this, context, parameters, cancellation);
						break;
					case IHandler<IExecutorContext<TArgument>> handler:
						await handler.HandleAsync(this, context, parameters, cancellation);
						break;
					case IHandler<IExecutorContext<TArgument, TResult>> handler:
						await handler.HandleAsync(this, context, parameters, cancellation);
						break;
					case IHandler handler:
						await handler.HandleAsync(this, context.Argument, parameters, cancellation);
						break;
				}
			}

			return context.Result;
		}

		protected virtual ValueTask OnFiltered(IFilter<IExecutorContext<TArgument, TResult>> filter, IExecutorContext<TArgument, TResult> context, CancellationToken cancellation) => filter?.OnFiltered(context, cancellation) ?? ValueTask.CompletedTask;
		protected virtual ValueTask OnFiltering(IFilter<IExecutorContext<TArgument, TResult>> filter, IExecutorContext<TArgument, TResult> context, CancellationToken cancellation) => filter?.OnFiltering(context, cancellation) ?? ValueTask.CompletedTask;
		#endregion

		#region 显式实现
		void IExecutor.Execute(object data, IEnumerable<KeyValuePair<string, object>> parameters)
		{
			if(data == null)
				this.Execute(default, parameters);

			switch(data)
			{
				case TArgument argument:
					this.Execute(argument, parameters);
					break;
				case IExecutorContext<TArgument, TResult> context:
					this.Execute(context);
					break;
			};
		}
		async ValueTask IExecutor.ExecuteAsync(object data, CancellationToken cancellation)
		{
			switch(data)
			{
				case TArgument argument:
					await this.ExecuteAsync(argument, null, cancellation);
					break;
				case IExecutorContext<TArgument, TResult> context:
					await this.ExecuteAsync(context, cancellation);
					break;
				default:
					if(data == null)
						await this.ExecuteAsync(default, null, cancellation);
					else
						throw new InvalidOperationException($"Unrecognized execution parameter: {data}");

					break;
			}
		}
		async ValueTask IExecutor.ExecuteAsync(object data, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation)
		{
			switch(data)
			{
				case TArgument argument:
					await this.ExecuteAsync(argument, parameters, cancellation);
					break;
				case IExecutorContext<TArgument, TResult> context:
					await this.ExecuteAsync(context, cancellation);
					break;
				default:
					if(data == null)
						await this.ExecuteAsync(default, parameters, cancellation);
					else
						throw new InvalidOperationException($"Unrecognized execution parameter: {data}");

					break;
			}
		}

		bool IHandler.CanHandle(object data, IEnumerable<KeyValuePair<string, object>> parameters) => data switch
		{
			TArgument argument => this.CanExecute(argument, parameters),
			IExecutorContext<TArgument, TResult> context => this.CanExecute(context),
			_ => false,
		};
		bool IHandler<TArgument, TResult>.CanHandle(TArgument argument, IEnumerable<KeyValuePair<string, object>> parameters) => this.CanExecute(argument, parameters);
		async ValueTask IHandler.HandleAsync(object caller, object data, CancellationToken cancellation) => await this.ExecuteAsync(this.CreateContext(data, null), cancellation);
		async ValueTask IHandler.HandleAsync(object caller, object data, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation) => await this.ExecuteAsync(CreateContext(data, parameters), cancellation);
		ValueTask<TResult> IHandler<TArgument, TResult>.HandleAsync(object caller, TArgument argument, CancellationToken cancellation) => this.ExecuteAsync(argument, null, cancellation);
		ValueTask<TResult> IHandler<TArgument, TResult>.HandleAsync(object caller, TArgument argument, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation) => this.ExecuteAsync(argument, parameters, cancellation);
		#endregion
	}
}
