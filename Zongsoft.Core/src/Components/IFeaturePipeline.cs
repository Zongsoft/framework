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

namespace Zongsoft.Components;

public interface IFeaturePipeline
{
	#region 公共属性
	IEnumerable<IFeature> Features { get; }
	#endregion

	#region 公共方法
	void Execute(Action executor);
	void Execute(Action<Collections.Parameters> executor, Collections.Parameters parameters);

	ValueTask ExecuteAsync(Func<CancellationToken, ValueTask> executor, CancellationToken cancellation = default);
	ValueTask ExecuteAsync(Func<Collections.Parameters, CancellationToken, ValueTask> executor, Collections.Parameters parameters, CancellationToken cancellation = default);
	#endregion
}

public interface IFeaturePipeline<TArgument>
{
	#region 公共属性
	IEnumerable<IFeature> Features { get; }
	#endregion

	#region 公共方法
	void Execute(Action<TArgument> executor, TArgument argument);
	void Execute(Action<TArgument, Collections.Parameters> executor, TArgument argument, Collections.Parameters parameters);

	ValueTask ExecuteAsync(Func<TArgument, CancellationToken, ValueTask> executor, TArgument argument, CancellationToken cancellation = default);
	ValueTask ExecuteAsync(Func<TArgument, Collections.Parameters, CancellationToken, ValueTask> executor, TArgument argument, Collections.Parameters parameters, CancellationToken cancellation = default);
	#endregion
}

public interface IFeaturePipeline<TArgument, TResult>
{
	#region 公共属性
	IEnumerable<IFeature> Features { get; }
	#endregion

	#region 公共方法
	TResult Execute(Func<TArgument, TResult> executor, TArgument argument);
	TResult Execute(Func<TArgument, Collections.Parameters, TResult> executor, TArgument argument, Collections.Parameters parameters);

	ValueTask<TResult> ExecuteAsync(Func<TArgument, CancellationToken, ValueTask<TResult>> executor, TArgument argument, CancellationToken cancellation = default);
	ValueTask<TResult> ExecuteAsync(Func<TArgument, Collections.Parameters, CancellationToken, ValueTask<TResult>> executor, TArgument argument, Collections.Parameters parameters, CancellationToken cancellation = default);
	#endregion
}
