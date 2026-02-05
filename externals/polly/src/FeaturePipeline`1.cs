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
 * This file is part of Zongsoft.Externals.Polly library.
 *
 * The Zongsoft.Externals.Polly is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Polly is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Polly library. If not, see <http://www.gnu.org/licenses/>.
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
				builder.AddStrategy(feature);

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
		return _pipeline == null ? default : _pipeline.Execute(state => executor(state), argument);
	}

	public TResult Execute(Func<TArgument, Parameters, TResult> executor, TArgument argument, Parameters parameters)
	{
		return _pipeline == null ? default : _pipeline.Execute(state => executor(state.argument, state.parameters), (argument, parameters));
	}
	#endregion

	#region 异步执行
	public ValueTask<TResult> ExecuteAsync(Func<TArgument, CancellationToken, ValueTask<TResult>> executor, TArgument argument, CancellationToken cancellation = default)
	{
		return _pipeline == null ? default : _pipeline.ExecuteAsync((state, cancellation) => executor(state, cancellation), argument, cancellation);
	}

	public ValueTask<TResult> ExecuteAsync(Func<TArgument, Parameters, CancellationToken, ValueTask<TResult>> executor, TArgument argument, Parameters parameters, CancellationToken cancellation = default)
	{
		return _pipeline == null ? default : _pipeline.ExecuteAsync((state, cancellation) => executor(state.argument, state.parameters, cancellation), (argument, parameters), cancellation);
	}
	#endregion
}
