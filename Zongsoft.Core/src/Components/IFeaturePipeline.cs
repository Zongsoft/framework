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

namespace Zongsoft.Components;

public interface IFeaturePipeline<TArgument>
{
	IEnumerable<IFeature> Features { get; }

	void Execute(Action<TArgument> executor, TArgument argument);
	void Execute(Action<TArgument, Collections.Parameters> executor, TArgument argument, Collections.Parameters parameters);

	ValueTask ExecuteAsync(Func<TArgument, CancellationToken, ValueTask> executor, TArgument argument, CancellationToken cancellation = default);
	ValueTask ExecuteAsync(Func<TArgument, Collections.Parameters, CancellationToken, ValueTask> executor, TArgument argument, Collections.Parameters parameters, CancellationToken cancellation = default);
}

public interface IFeaturePipeline<TArgument, TResult>
{
	IEnumerable<IFeature> Features { get; }

	TResult Execute(Func<TArgument, TResult> executor, TArgument argument);
	TResult Execute(Func<TArgument, Collections.Parameters, TResult> executor, TArgument argument, Collections.Parameters parameters);

	ValueTask<TResult> ExecuteAsync(Func<TArgument, CancellationToken, ValueTask<TResult>> executor, TArgument argument, CancellationToken cancellation = default);
	ValueTask<TResult> ExecuteAsync(Func<TArgument, Collections.Parameters, CancellationToken, ValueTask<TResult>> executor, TArgument argument, Collections.Parameters parameters, CancellationToken cancellation = default);
}
