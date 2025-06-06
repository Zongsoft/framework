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

using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Externals.Polly;

public sealed class FeaturePipeline : IFeaturePipeline
{
	private IEnumerable<IFeature> _features;
	private ICollection<ResilienceStrategy> _strategies;

	public void Execute<TArgument>(Action<TArgument> executor, TArgument argument)
	{
		var builder = new ResiliencePipelineBuilder();
		var pipeline = builder.Build();
	}

	public void Execute<TArgument>(Action<TArgument, Parameters> executor, TArgument argument, Parameters parameters) => throw new NotImplementedException();
	public TResult Execute<TArgument, TResult>(Func<TArgument, TResult> executor, TArgument argument) => throw new NotImplementedException();
	public TResult Execute<TArgument, TResult>(Func<TArgument, Parameters, TResult> executor, TArgument argument, Parameters parameters) => throw new NotImplementedException();
	public ValueTask ExecuteAsync<TArgument>(Func<TArgument, CancellationToken, ValueTask> executor, TArgument argument, CancellationToken cancellation = default) => throw new NotImplementedException();
	public ValueTask ExecuteAsync<TArgument>(Func<TArgument, Parameters, CancellationToken, ValueTask> executor, TArgument argument, Parameters parameters, CancellationToken cancellation = default) => throw new NotImplementedException();
	public ValueTask<TResult> ExecuteAsync<TArgument, TResult>(Func<TArgument, CancellationToken, ValueTask<TResult>> executor, TArgument argument, CancellationToken cancellation = default) => throw new NotImplementedException();
	public ValueTask<TResult> ExecuteAsync<TArgument, TResult>(Func<TArgument, Parameters, CancellationToken, ValueTask<TResult>> executor, TArgument argument, Parameters parameters, CancellationToken cancellation = default) => throw new NotImplementedException();

	private static ResilienceStrategy GetStrategy(IFeature feature)
	{
		if(feature == null)
			return null;

	}
}
