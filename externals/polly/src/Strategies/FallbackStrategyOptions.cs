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

using Polly;
using Polly.Fallback;

using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly.Strategies;

internal sealed class FallbackStrategyOptions<TArgument, TResult> : ResilienceStrategyOptions
{
	public FallbackStrategyOptions()
	{
		this.Name = "Fallback";
		this.ShouldHandle = args => new(args.HasError(out var exception) && exception is not OperationCanceledException);
	}

	public Func<Argument<TArgument, TResult>, ValueTask<bool>> ShouldHandle { get; set; }
	public Func<Argument<TArgument, TResult>, ValueTask> FallbackAction { get; set; }
	public Func<Argument<TArgument, TResult>, ValueTask> OnFallback { get; set; }
}
