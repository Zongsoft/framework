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
	public abstract class ExecutorBase<TArgument> : IExecutor<TArgument>, IHandler<TArgument>, IFilterable<IExecutorContext>
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
		public void Execute(TArgument argument, Collections.Parameters parameters = null) => this.Execute(this.CreateContext(argument, parameters));
		protected void Execute(IExecutorContext<TArgument> context)
		{
			var task = this.ExecuteAsync(context, default);
			if(!task.IsCompletedSuccessfully)
				task.GetAwaiter().GetResult();
		}

		public ValueTask ExecuteAsync(TArgument argument, CancellationToken cancellation = default) => this.ExecuteAsync(this.CreateContext(argument, null), cancellation);
		public ValueTask ExecuteAsync(TArgument argument, Collections.Parameters parameters, CancellationToken cancellation = default) => this.ExecuteAsync(this.CreateContext(argument, parameters), cancellation);
		protected async ValueTask ExecuteAsync(IExecutorContext<TArgument> context, CancellationToken cancellation = default)
		{
			var filters = this.Filters;

			if(filters != null)
			{
				foreach(var filter in filters)
					await this.OnFiltering(filter, context, cancellation);
			}

			await this.OnExecuteAsync(context, cancellation);

			if(filters != null)
			{
				foreach(var filter in filters)
					await this.OnFiltered(filter, context, cancellation);
			}
		}
		#endregion

		#region 虚拟方法
		protected abstract IExecutorContext<TArgument> CreateContext(TArgument argument, Collections.Parameters parameters);
		protected virtual IExecutorContext<TArgument> CreateContext(object data, Collections.Parameters parameters) => data switch
		{
			IExecutorContext<TArgument> context => context,
			TArgument argument => this.CreateContext(argument, parameters),
			_ => throw new InvalidOperationException($"Unrecognized execution parameter: {data}"),
		};
		protected virtual IHandler GetHandler(IExecutorContext<TArgument> context) => this.Locator?.Locate(context);

		protected bool CanExecute(TArgument argument, Collections.Parameters parameters) => this.CanExecute(this.CreateContext(argument, parameters));
		protected bool CanExecute(IExecutorContext<TArgument> context) => this.CanExecute(context, out _);
		protected virtual bool CanExecute(IExecutorContext<TArgument> context, out IHandler handler)
		{
			if(context == null)
			{
				handler = null;
				return false;
			}

			handler = this.GetHandler(context);
			if(handler == null)
				return false;

			return handler switch
			{
				IHandler<TArgument> matched => matched.CanHandle(context.Argument, context.Parameters),
				IHandler<IExecutorContext> matched => matched.CanHandle(context, context.Parameters),
				IHandler<IExecutorContext<TArgument>> matched => matched.CanHandle(context, context.Parameters),
				IHandler matched => matched.CanHandle(context.Argument, context.Parameters),
				_ => false,
			};
		}

		protected virtual ValueTask OnExecuteAsync(IExecutorContext<TArgument> context, CancellationToken cancellation = default)
		{
			if(this.CanExecute(context, out var executor))
				return executor switch
				{
					IHandler<TArgument> handler => handler.HandleAsync(this, context.Argument, context.Parameters, cancellation),
					IHandler<IExecutorContext> handler => handler.HandleAsync(this, context, context.Parameters, cancellation),
					IHandler<IExecutorContext<TArgument>> handler => handler.HandleAsync(this, context, context.Parameters, cancellation),
					IHandler handler => handler.HandleAsync(this, context.Argument, context.Parameters, cancellation),
				};

			return ValueTask.CompletedTask;
		}

		protected virtual ValueTask OnFiltered(IFilter<IExecutorContext<TArgument>> filter, IExecutorContext<TArgument> context, CancellationToken cancellation) => filter?.OnFiltered(context, cancellation) ?? ValueTask.CompletedTask;
		protected virtual ValueTask OnFiltering(IFilter<IExecutorContext<TArgument>> filter, IExecutorContext<TArgument> context, CancellationToken cancellation) => filter?.OnFiltering(context, cancellation) ?? ValueTask.CompletedTask;
		#endregion

		#region 显式实现
		void IExecutor.Execute(object data, Collections.Parameters parameters)
		{
			if(data == null)
				this.Execute(default, parameters);

			switch(data)
			{
				case TArgument argument:
					this.Execute(argument, parameters);
					break;
				case IExecutorContext<TArgument> context:
					this.Execute(context);
					break;
			};
		}
		ValueTask IExecutor.ExecuteAsync(object data, CancellationToken cancellation) => data switch
		{
			TArgument argument => this.ExecuteAsync(argument, null, cancellation),
			IExecutorContext<TArgument> context => this.ExecuteAsync(context, cancellation),
			_ => data == null ? this.ExecuteAsync(default, null, cancellation) : throw new InvalidOperationException($"Unrecognized execution parameter: {data}"),
		};
		ValueTask IExecutor.ExecuteAsync(object data, Collections.Parameters parameters, CancellationToken cancellation) => data switch
		{
			TArgument argument => this.ExecuteAsync(argument, parameters, cancellation),
			IExecutorContext<TArgument> context => this.ExecuteAsync(context, cancellation),
			_ => data == null ? this.ExecuteAsync(default, parameters, cancellation) : throw new InvalidOperationException($"Unrecognized execution parameter: {data}"),
		};

		bool IHandler.CanHandle(object data, Collections.Parameters parameters) => data switch
		{
			TArgument argument => this.CanExecute(argument, parameters),
			IExecutorContext<TArgument> context => this.CanExecute(context),
			_ => false,
		};
		bool IHandler<TArgument>.CanHandle(TArgument argument, Collections.Parameters parameters) => this.CanExecute(argument, parameters);
		ValueTask IHandler.HandleAsync(object caller, object data, CancellationToken cancellation) => this.ExecuteAsync(this.CreateContext(data, null), cancellation);
		ValueTask IHandler.HandleAsync(object caller, object data, Collections.Parameters parameters, CancellationToken cancellation) => this.ExecuteAsync(CreateContext(data, parameters), cancellation);
		ValueTask IHandler<TArgument>.HandleAsync(object caller, TArgument argument, CancellationToken cancellation) => this.ExecuteAsync(argument, null, cancellation);
		ValueTask IHandler<TArgument>.HandleAsync(object caller, TArgument argument, Collections.Parameters parameters, CancellationToken cancellation) => this.ExecuteAsync(argument, parameters, cancellation);
		#endregion
	}
}
