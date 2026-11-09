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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Polly;

using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Externals.Polly;

public sealed class FeaturePipeline<TArgument, TResult> : IFeaturePipeline<TArgument, TResult>
{
	#region 成员字段
	private readonly ResiliencePipeline<TResult> _pipeline;
	#endregion

	#region 构造函数
	public FeaturePipeline(IEnumerable<IFeature> features)
	{
		if(features != null)
		{
			var builder = new ResiliencePipelineBuilder<TResult>();

			foreach(var feature in features)
				builder.AddStrategy<TArgument, TResult>(feature);

			_pipeline = builder.Build();
		}

		this.Features = features ?? [];
	}
	#endregion

	#region 公共属性
	public IEnumerable<IFeature> Features { get; }
	#endregion

	#region 同步执行
	public TResult Execute(Func<TArgument, TResult> executor, TArgument argument)
	{
		return _pipeline == null ? default : _pipeline.Execute(executor, argument);
	}

	public TResult Execute(Func<TArgument, Parameters, TResult> executor, TArgument argument, Parameters parameters)
	{
		return _pipeline == null ? default : _pipeline.Execute(state => executor(state.argument, state.parameters), (argument, parameters));
	}
	#endregion

	#region 异步执行
	public ValueTask<TResult> ExecuteAsync(Func<TArgument, CancellationToken, ValueTask<TResult>> executor, TArgument argument, CancellationToken cancellation = default)
	{
		return _pipeline == null ? default : _pipeline.ExecuteAsync(executor, argument, cancellation);
	}

	public ValueTask<TResult> ExecuteAsync(Func<TArgument, Parameters, CancellationToken, ValueTask<TResult>> executor, TArgument argument, Parameters parameters, CancellationToken cancellation = default)
	{
		return _pipeline == null ? default : _pipeline.ExecuteAsync((state, cancellation) => executor(state.argument, state.parameters, cancellation), (argument, parameters), cancellation);
	}
	#endregion
}
