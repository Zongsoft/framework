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

namespace Zongsoft.Common;

public static class Predication
{
	#region 静态方法
	public static IPredication Predicate(Func<object, bool> predicate) => new PredicationProxy<object>(predicate);
	public static IPredication<T> Predicate<T>(Func<T, bool> predicate) => new PredicationProxy<T>(predicate);
	public static IPredication Predicate(Func<object, CancellationToken, ValueTask<bool>> predicate) => new PredicationProxy<object>(predicate);
	public static IPredication<T> Predicate<T>(Func<T, CancellationToken, ValueTask<bool>> predicate) => new PredicationProxy<T>(predicate);

	public static IPredication Predicate(Func<object, Collections.Parameters, bool> predicate) => new PredicationProxy<object>(predicate);
	public static IPredication<T> Predicate<T>(Func<T, Collections.Parameters, bool> predicate) => new PredicationProxy<T>(predicate);
	public static IPredication Predicate(Func<object, Collections.Parameters, CancellationToken, ValueTask<bool>> predicate) => new PredicationProxy<object>(predicate);
	public static IPredication<T> Predicate<T>(Func<T, Collections.Parameters, CancellationToken, ValueTask<bool>> predicate) => new PredicationProxy<T>(predicate);
	#endregion

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

		#region 公共方法
		public ValueTask<bool> PredicateAsync(TArgument argument, CancellationToken cancellation = default) => _predicator(argument, null, cancellation);
		public ValueTask<bool> PredicateAsync(TArgument argument, Collections.Parameters parameters, CancellationToken cancellation = default) => _predicator(argument, parameters, cancellation);
		public ValueTask<bool> PredicateAsync(object argument, CancellationToken cancellation = default) =>
			argument is TArgument target ? this.PredicateAsync(target, cancellation) : throw new InvalidOperationException(string.Format(Properties.Resources.Conversion_ValueType_Message, argument, typeof(TArgument)));
		public ValueTask<bool> PredicateAsync(object argument, Collections.Parameters parameters, CancellationToken cancellation = default) =>
			argument is TArgument target ? this.PredicateAsync(target, parameters, cancellation) : throw new InvalidOperationException(string.Format(Properties.Resources.Conversion_ValueType_Message, argument, typeof(TArgument)));
		#endregion
	}
}
