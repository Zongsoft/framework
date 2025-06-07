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
using System.Collections.Generic;

namespace Zongsoft.Components;

public abstract class ExecutorBase<TArgument> : IExecutor<TArgument>, IHandler<TArgument>, IFilterable<IExecutorContext>
{
	#region 构造函数
	protected ExecutorBase(params IEnumerable<IFeature> features) : this(null, features) { }
	protected ExecutorBase(IHandlerLocator<IExecutorContext> locator, params IEnumerable<IFeature> features)
	{
		this.Filters = [];
		this.Locator = locator;
		this.Features = features ?? [];
	}
	#endregion

	#region 公共属性
	public IEnumerable<IFeature> Features { get; }
	public IHandlerLocator<IExecutorContext> Locator { get; }
	public ICollection<IFilter<IExecutorContext>> Filters { get; }
	#endregion

	#region 执行方法
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
	protected virtual ValueTask OnExecuteAsync(IExecutorContext<TArgument> context, CancellationToken cancellation = default) => this.GetHandler(context) switch
	{
		IHandler<TArgument> handler => handler.HandleAsync(context.Argument, context.Parameters, cancellation),
		IHandler<IExecutorContext> handler => handler.HandleAsync(context, context.Parameters, cancellation),
		IHandler<IExecutorContext<TArgument>> handler => handler.HandleAsync(context, context.Parameters, cancellation),
		IHandler handler => handler.HandleAsync(context.Argument, context.Parameters, cancellation),
		_ => ValueTask.CompletedTask,
	};

	protected virtual ValueTask OnFiltered(IFilter<IExecutorContext<TArgument>> filter, IExecutorContext<TArgument> context, CancellationToken cancellation) => filter?.OnFiltered(context, cancellation) ?? ValueTask.CompletedTask;
	protected virtual ValueTask OnFiltering(IFilter<IExecutorContext<TArgument>> filter, IExecutorContext<TArgument> context, CancellationToken cancellation) => filter?.OnFiltering(context, cancellation) ?? ValueTask.CompletedTask;
	#endregion

	#region 显式实现
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

	ValueTask IHandler.HandleAsync(object data, CancellationToken cancellation) => this.ExecuteAsync(this.CreateContext(data, null), cancellation);
	ValueTask IHandler.HandleAsync(object data, Collections.Parameters parameters, CancellationToken cancellation) => this.ExecuteAsync(this.CreateContext(data, parameters), cancellation);
	ValueTask IHandler<TArgument>.HandleAsync(TArgument argument, CancellationToken cancellation) => this.ExecuteAsync(argument, null, cancellation);
	ValueTask IHandler<TArgument>.HandleAsync(TArgument argument, Collections.Parameters parameters, CancellationToken cancellation) => this.ExecuteAsync(argument, parameters, cancellation);
	#endregion
}
