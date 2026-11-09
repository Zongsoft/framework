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
using System.Collections.Generic;

using Zongsoft.Components;

namespace Zongsoft.Externals.Polly;

public sealed class FeaturePipelineBuilder : IFeaturePipelineBuilder
{
	#region 单例字段
	public static readonly FeaturePipelineBuilder Instance = new();
	#endregion

	#region 私有构造
	private FeaturePipelineBuilder() { }
	#endregion

	#region 公共方法
	public IFeaturePipeline Build(IEnumerable<IFeature> features) => features == null ? null : new FeaturePipeline(features);
	public IFeaturePipeline<TArgument> Build<TArgument>(IEnumerable<IFeature> features) => features == null ? null : new FeaturePipeline<TArgument>(features);
	public IFeaturePipeline<TArgument, TResult> Build<TArgument, TResult>(IEnumerable<IFeature> features) => features == null ? null : new FeaturePipeline<TArgument, TResult>(features);
	#endregion
}
