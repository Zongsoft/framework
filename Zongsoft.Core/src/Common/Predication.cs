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

namespace Zongsoft.Common;

public static class Predication
{
	public static IPredication Predicate(Func<object, bool> predicate) => new PredicationProxy<object>(predicate);
	public static IPredication<T> Predicate<T>(Func<T, bool> predicate) => new PredicationProxy<T>(predicate);
	public static IPredication Predicate(Func<object, CancellationToken, ValueTask<bool>> predicate) => new PredicationProxy<object>(predicate);
	public static IPredication<T> Predicate<T>(Func<T, CancellationToken, ValueTask<bool>> predicate) => new PredicationProxy<T>(predicate);

	public static IPredication Predicate(Func<object, Collections.Parameters, bool> predicate) => new PredicationProxy<object>(predicate);
	public static IPredication<T> Predicate<T>(Func<T, Collections.Parameters, bool> predicate) => new PredicationProxy<T>(predicate);
	public static IPredication Predicate(Func<object, Collections.Parameters, CancellationToken, ValueTask<bool>> predicate) => new PredicationProxy<object>(predicate);
	public static IPredication<T> Predicate<T>(Func<T, Collections.Parameters, CancellationToken, ValueTask<bool>> predicate) => new PredicationProxy<T>(predicate);

	private sealed class PredicationProxy<TArgument> : IPredication<TArgument>
	{
		private readonly Func<TArgument, Collections.Parameters, CancellationToken, ValueTask<bool>> _predicator;

		public PredicationProxy(Func<TArgument, CancellationToken, ValueTask<bool>> predicator)
		{
			if(predicator == null)
				throw new ArgumentNullException(nameof(predicator));

			_predicator = (argument, _, cancellation) => predicator(argument, cancellation);
		}
		public PredicationProxy(Func<TArgument, bool> predicator)
		{
			if(predicator == null)
				throw new ArgumentNullException(nameof(predicator));

			_predicator = (argument, _, cancellation) => cancellation.IsCancellationRequested ?
				ValueTask.FromCanceled<bool>(cancellation) :
				ValueTask.FromResult(predicator(argument));
		}

		public PredicationProxy(Func<TArgument, Collections.Parameters, CancellationToken, ValueTask<bool>> predicator) => _predicator = predicator ?? throw new ArgumentNullException(nameof(predicator));
		public PredicationProxy(Func<TArgument, Collections.Parameters, bool> predicator)
		{
			if(predicator == null)
				throw new ArgumentNullException(nameof(predicator));

			_predicator = (argument, parameters, cancellation) => cancellation.IsCancellationRequested ?
				ValueTask.FromCanceled<bool>(cancellation) :
				ValueTask.FromResult(predicator(argument, parameters));
		}

		public ValueTask<bool> PredicateAsync(TArgument argument, CancellationToken cancellation = default) => _predicator(argument, null, cancellation);
		public ValueTask<bool> PredicateAsync(TArgument argument, Collections.Parameters parameters, CancellationToken cancellation = default) => _predicator(argument, parameters, cancellation);
		public ValueTask<bool> PredicateAsync(object argument, CancellationToken cancellation = default) =>
			argument is TArgument target ? this.PredicateAsync(target, cancellation) : throw new InvalidOperationException($"Unable to convert '{argument}' argument value to '{typeof(TArgument)}' type.");
		public ValueTask<bool> PredicateAsync(object argument, Collections.Parameters parameters, CancellationToken cancellation = default) =>
			argument is TArgument target ? this.PredicateAsync(target, parameters, cancellation) : throw new InvalidOperationException($"Unable to convert '{argument}' argument value to '{typeof(TArgument)}' type.");
	}
}
