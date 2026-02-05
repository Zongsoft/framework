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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Common;
using Zongsoft.Collections;

namespace Zongsoft.Components;

public static class Executor
{
	#region 公共属性
	/// <summary>获取功能构建器。</summary>
	/// <remarks>该属性始终返回空<c>null</c>，用于相关扩展方法的扩展点。</remarks>
	public static IFeatureBuilder Features { get; }

	/// <summary>获取或设置功能管线构建器。</summary>
	public static IFeaturePipelineBuilder Pipelines { get; set; }
	#endregion

	#region 处理程序
	public static IExecutor<TArgument> Build<TArgument>(IHandler<TArgument> handler) => new ExecutorProxy<TArgument>(handler, null);
	public static IExecutor<TArgument, TResult> Build<TArgument, TResult>(IHandler<TArgument, TResult> handler) => new ExecutorProxy<TArgument, TResult>(handler, null);

	public static IExecutor<TArgument> Build<TArgument>(this IFeatureBuilder builder, IHandler<TArgument> handler) => new ExecutorProxy<TArgument>(handler, builder?.Build());
	public static IExecutor<TArgument, TResult> Build<TArgument, TResult>(this IFeatureBuilder builder, IHandler<TArgument, TResult> handler) => new ExecutorProxy<TArgument, TResult>(handler, builder?.Build());
	#endregion

	#region 同步执行
	public static IExecutor<TArgument> Build<TArgument>(Action<TArgument> execute) => new ExecutorProxy<TArgument>(execute, null);
	public static IExecutor<TArgument> Build<TArgument>(Action<TArgument, Parameters> execute) => new ExecutorProxy<TArgument>(execute, null);
	public static IExecutor<TArgument, TResult> Build<TArgument, TResult>(Func<TArgument, TResult> execute) => new ExecutorProxy<TArgument, TResult>(execute, null);
	public static IExecutor<TArgument, TResult> Build<TArgument, TResult>(Func<TArgument, Parameters, TResult> execute) => new ExecutorProxy<TArgument, TResult>(execute, null);

	public static IExecutor<TArgument> Build<TArgument>(this IFeatureBuilder builder, Action<TArgument> execute) => new ExecutorProxy<TArgument>(execute, builder?.Build());
	public static IExecutor<TArgument> Build<TArgument>(this IFeatureBuilder builder, Action<TArgument, Parameters> execute) => new ExecutorProxy<TArgument>(execute, builder?.Build());
	public static IExecutor<TArgument, TResult> Build<TArgument, TResult>(this IFeatureBuilder builder, Func<TArgument, TResult> execute) => new ExecutorProxy<TArgument, TResult>(execute, builder?.Build());
	public static IExecutor<TArgument, TResult> Build<TArgument, TResult>(this IFeatureBuilder builder, Func<TArgument, Parameters, TResult> execute) => new ExecutorProxy<TArgument, TResult>(execute, builder?.Build());
	#endregion

	#region 异步执行
	public static IExecutor<TArgument> Build<TArgument>(Func<TArgument, CancellationToken, ValueTask> execute) => new ExecutorProxy<TArgument>(execute, null);
	public static IExecutor<TArgument> Build<TArgument>(Func<TArgument, Parameters, CancellationToken, ValueTask> execute) => new ExecutorProxy<TArgument>(execute, null);
	public static IExecutor<TArgument, TResult> Build<TArgument, TResult>(Func<TArgument, CancellationToken, ValueTask<TResult>> execute) => new ExecutorProxy<TArgument, TResult>(execute, null);
	public static IExecutor<TArgument, TResult> Build<TArgument, TResult>(Func<TArgument, Parameters, CancellationToken, ValueTask<TResult>> execute) => new ExecutorProxy<TArgument, TResult>(execute, null);

	public static IExecutor<TArgument> Build<TArgument>(this IFeatureBuilder builder, Func<TArgument, CancellationToken, ValueTask> execute) => new ExecutorProxy<TArgument>(execute, builder?.Build());
	public static IExecutor<TArgument> Build<TArgument>(this IFeatureBuilder builder, Func<TArgument, Parameters, CancellationToken, ValueTask> execute) => new ExecutorProxy<TArgument>(execute, builder?.Build());
	public static IExecutor<TArgument, TResult> Build<TArgument, TResult>(this IFeatureBuilder builder, Func<TArgument, CancellationToken, ValueTask<TResult>> execute) => new ExecutorProxy<TArgument, TResult>(execute, builder?.Build());
	public static IExecutor<TArgument, TResult> Build<TArgument, TResult>(this IFeatureBuilder builder, Func<TArgument, Parameters, CancellationToken, ValueTask<TResult>> execute) => new ExecutorProxy<TArgument, TResult>(execute, builder?.Build());
	#endregion

	#region 嵌套子类
	private sealed class ExecutorProxy<TArgument> : ExecutorBase<TArgument>
	{
		private readonly IHandler<TArgument> _handler;
		private readonly IFeaturePipeline<TArgument> _pipeline;

		public ExecutorProxy(IHandler<TArgument> handler, IEnumerable<IFeature> features)
		{
			_handler = handler ?? throw new ArgumentNullException(nameof(handler));
			_pipeline = Pipelines?.Build<TArgument>(features);
		}

		public ExecutorProxy(Action<TArgument> handler, IEnumerable<IFeature> features)
		{
			_handler = Handler.Handle(handler);
			_pipeline = Pipelines?.Build<TArgument>(features);
		}

		public ExecutorProxy(Action<TArgument, Parameters> handler, IEnumerable<IFeature> features)
		{
			_handler = Handler.Handle(handler);
			_pipeline = Pipelines?.Build<TArgument>(features);
		}

		public ExecutorProxy(Func<TArgument, CancellationToken, ValueTask> handler, IEnumerable<IFeature> features)
		{
			_handler = Handler.Handle(handler);
			_pipeline = Pipelines?.Build<TArgument>(features);
		}

		public ExecutorProxy(Func<TArgument, Parameters, CancellationToken, ValueTask> handler, IEnumerable<IFeature> features)
		{
			_handler = Handler.Handle(handler);
			_pipeline = Pipelines?.Build<TArgument>(features);
		}

		protected override IHandler GetHandler(IExecutorContext<TArgument> context) => _handler;
		protected override IExecutorContext<TArgument> CreateContext(TArgument argument, Parameters parameters) => new ExecutorContext<TArgument>(this, argument, parameters);
		protected override ValueTask OnExecuteAsync(IExecutorContext<TArgument> context, CancellationToken cancellation = default)
		{
			var pipeline = _pipeline;

			return pipeline == null ?
				base.OnExecuteAsync(context, cancellation) :
				pipeline.ExecuteAsync((_, _, cancellation) => base.OnExecuteAsync(context, cancellation), context.Argument, context.Parameters, cancellation);
		}
	}

	private sealed class ExecutorProxy<TArgument, TResult> : ExecutorBase<TArgument, TResult>
	{
		private readonly IHandler<TArgument, TResult> _handler;
		private readonly IFeaturePipeline<TArgument, TResult> _pipeline;

		public ExecutorProxy(IHandler<TArgument, TResult> handler, IEnumerable<IFeature> features)
		{
			_handler = handler ?? throw new ArgumentNullException(nameof(handler));
			_pipeline = Pipelines?.Build<TArgument, TResult>(features);
		}

		public ExecutorProxy(Func<TArgument, TResult> handler, IEnumerable<IFeature> features)
		{
			_handler = Handler.Handle(handler);
			_pipeline = Pipelines?.Build<TArgument, TResult>(features);
		}

		public ExecutorProxy(Func<TArgument, Parameters, TResult> handler, IEnumerable<IFeature> features)
		{
			_handler = Handler.Handle(handler);
			_pipeline = Pipelines?.Build<TArgument, TResult>(features);
		}

		public ExecutorProxy(Func<TArgument, CancellationToken, ValueTask<TResult>> handler, IEnumerable<IFeature> features)
		{
			_handler = Handler.Handle(handler);
			_pipeline = Pipelines?.Build<TArgument, TResult>(features);
		}

		public ExecutorProxy(Func<TArgument, Parameters, CancellationToken, ValueTask<TResult>> handler, IEnumerable<IFeature> features)
		{
			_handler = Handler.Handle(handler);
			_pipeline = Pipelines?.Build<TArgument, TResult>(features);
		}

		protected override IHandler GetHandler(IExecutorContext<TArgument, TResult> context) => _handler;
		protected override IExecutorContext<TArgument, TResult> CreateContext(TArgument argument, Parameters parameters) => new ExecutorContext<TArgument, TResult>(this, argument, parameters);
		protected override ValueTask<TResult> OnExecuteAsync(IExecutorContext<TArgument, TResult> context, CancellationToken cancellation = default)
		{
			var pipeline = _pipeline;

			return pipeline == null ?
				base.OnExecuteAsync(context, cancellation) :
				pipeline.ExecuteAsync((_, _, cancellation) => base.OnExecuteAsync(context, cancellation), context.Argument, context.Parameters, cancellation);
		}
	}
	#endregion
}
