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

public sealed class FeaturePipeline : IFeaturePipeline
{
	#region 成员字段
	private readonly ResiliencePipeline _pipeline;
	#endregion

	#region 构造函数
	public FeaturePipeline(IEnumerable<IFeature> features)
	{
		if(features != null)
		{
			var builder = new ResiliencePipelineBuilder();

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
	public void Execute(Action executor) => _pipeline?.Execute(executor);
	public void Execute(Action<Parameters> executor, Parameters parameters) => _pipeline?.Execute(executor, parameters);
	#endregion

	#region 异步执行
	public ValueTask ExecuteAsync(Func<CancellationToken, ValueTask> executor, CancellationToken cancellation = default)
	{
		return _pipeline == null ? default : _pipeline.ExecuteAsync(executor, cancellation);
	}

	public ValueTask ExecuteAsync(Func<Parameters, CancellationToken, ValueTask> executor, Parameters parameters, CancellationToken cancellation = default)
	{
		return _pipeline == null ? default : _pipeline.ExecuteAsync(executor, parameters, cancellation);
	}
	#endregion
}
