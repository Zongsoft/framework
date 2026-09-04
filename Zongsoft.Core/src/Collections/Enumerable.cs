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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zongsoft.Collections;

public static class Enumerable
{
	#region 公共方法
	public static IAsyncEnumerable<T> Empty<T>() => EmptyEnumerable<T>.Instance;
	public static IEnumerable Empty(Type elementType)
	{
		return elementType == null ? throw new ArgumentNullException(nameof(elementType)) :
			(IEnumerable)System.Activator.CreateInstance(typeof(EmptyEnumerable<>).MakeGenericType(elementType));
	}

	public static async ValueTask<T> First<T>(this IAsyncEnumerable<T> source, CancellationToken cancellation = default)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		var enumerator = source.GetAsyncEnumerator(cancellation);

		try
		{
			return await enumerator.MoveNextAsync().ConfigureAwait(false) ? enumerator.Current : throw new InvalidOperationException("Sequence contains no elements.");
		}
		finally
		{
			await enumerator.DisposeAsync().ConfigureAwait(false);
		}
	}

	public static async ValueTask<T> FirstOrDefault<T>(this IAsyncEnumerable<T> source, CancellationToken cancellation = default)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		var enumerator = source.GetAsyncEnumerator(cancellation);

		try
		{
			return await enumerator.MoveNextAsync().ConfigureAwait(false) ? enumerator.Current : default;
		}
		finally
		{
			await enumerator.DisposeAsync().ConfigureAwait(false);
		}
	}

	public static IEnumerable<T> Synchronize<T>(this IAsyncEnumerable<T> source, CancellationToken cancellation = default)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		var result = source.ToBlockingEnumerable(cancellation);
		return Data.Pageable.Wrap(result, source as Data.IPageable);
	}

	public static IAsyncEnumerable<T> Asynchronize<T>(this IEnumerable<T> source)
	{
		if(source == null)
			return EmptyEnumerable<T>.Instance;

		if(source is IAsyncEnumerable<T> enumerable)
			return enumerable;

		var result = new AsyncEnumerable<T>(source, default);
		return Data.Pageable.Wrap(result, source as Data.IPageable);
	}

	public static bool IsAsyncEnumerable(object source) => TryGetAsyncEnumerableElementType(source, out _);
	public static bool IsAsyncEnumerable(object source, out Type elementType) => TryGetAsyncEnumerableElementType(source, out elementType);

	public static IAsyncEnumerable<T> EnumerateAsync<T>(object source, CancellationToken cancellation = default)
	{
		if(source == null)
			return cancellation.CanBeCanceled ? ForwardAsync(EmptyEnumerable<T>.Instance, cancellation) : EmptyEnumerable<T>.Instance;

		if(source is IAsyncEnumerable<T> enumerable)
		{
			if(!cancellation.CanBeCanceled)
				return enumerable;

			var result = ForwardAsync(enumerable, cancellation);
			return Data.Pageable.Wrap(result, source as Data.IPageable);
		}

		var items = Enumerate<T>(source);
		var adapter = new AsyncEnumerable<T>(items, cancellation);
		return Data.Pageable.Wrap(adapter, source as Data.IPageable);
	}

	public static IEnumerable<T> Enumerate<T>(object source)
	{
		if(source == null)
			return [];

		if(source is IEnumerable<T> items)
			return items;
		if(source is IAsyncEnumerable<T> enumerable)
			return enumerable.Synchronize();

		return new TypedEnumerable<T>(source);
	}

	public static IEnumerable Enumerate(object source, Type elementType)
	{
		if(elementType == null)
			throw new ArgumentNullException(nameof(elementType));

		if(source == null)
			return (IEnumerable)System.Activator.CreateInstance(typeof(EmptyEnumerable<>).MakeGenericType(elementType));
		else
			return (IEnumerable)System.Activator.CreateInstance(typeof(TypedEnumerable<>).MakeGenericType(elementType), [source]);
	}

	public static IEnumerator<T> GetEnumerator<T>(T[] array)
	{
		if(array == null)
			throw new ArgumentNullException(nameof(array));

		return ((IEnumerable<T>)array).GetEnumerator();
	}

	#pragma warning disable CS8424
	public static IAsyncEnumerable<object> Cast<T>(this IAsyncEnumerable<T> source, [EnumeratorCancellation]CancellationToken cancellation = default)
	{
		if(source == null)
			return EmptyEnumerable<object>.Instance;

		var result = new AsyncCastEnumerable<T, object>(source, cancellation, TryConvert);
		return Data.Pageable.Wrap(result, source as Data.IPageable);

		static bool TryConvert(T source, out object destination)
		{
			destination = source;
			return true;
		}
	}

	public static IAsyncEnumerable<TDestination> Cast<TSource, TDestination>(this IAsyncEnumerable<TSource> source, [EnumeratorCancellation]CancellationToken cancellation = default)
	{
		if(source == null)
			return EmptyEnumerable<TDestination>.Instance;

		var result = new AsyncCastEnumerable<TSource, TDestination>(source, cancellation, TryConvert);
		return Data.Pageable.Wrap(result, source as Data.IPageable);

		static bool TryConvert(TSource source, out TDestination destination)
		{
			if(source is TDestination converted)
			{
				destination = converted;
				return true;
			}

			destination = default;
			return false;
		}
	}
	#pragma warning restore CS8424
	#endregion

	#region 私有方法
	private static bool TryGetAsyncEnumerableElementType(object source, out Type elementType)
	{
		if(source != null)
		{
			var contracts = source.GetType().GetInterfaces();

			for(int i = 0; i < contracts.Length; i++)
			{
				if(contracts[i].IsGenericType && contracts[i].GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
				{
					elementType = contracts[i].GenericTypeArguments[0];
					return true;
				}
			}
		}

		elementType = null;
		return false;
	}

	private static async IAsyncEnumerable<T> ForwardAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellation, [EnumeratorCancellation] CancellationToken iteration = default)
	{
		var token = GetCancellation(cancellation, iteration, out var registration);

		using(registration)
		{
			token.ThrowIfCancellationRequested();

			await foreach(var item in source.WithCancellation(token).ConfigureAwait(false))
			{
				token.ThrowIfCancellationRequested();
				yield return item;
			}
		}
	}

	private static CancellationToken GetCancellation(CancellationToken first, CancellationToken second, out CancellationTokenSource source)
	{
		source = null;

		if(!first.CanBeCanceled)
			return second;
		if(!second.CanBeCanceled || first == second)
			return first;

		return (source = CancellationTokenSource.CreateLinkedTokenSource(first, second)).Token;
	}
	#endregion

	#region 嵌套子类
	private sealed class EmptyEnumerable<T> : IEnumerable<T>, IOrderedEnumerable<T>, IAsyncEnumerable<T>, IAsyncEnumerator<T>
	{
		public static readonly EmptyEnumerable<T> Instance = new();

		public IEnumerator<T> GetEnumerator() { yield break; }
		IEnumerator IEnumerable.GetEnumerator() { yield break; }
		IOrderedEnumerable<T> IOrderedEnumerable<T>.CreateOrderedEnumerable<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer, bool descending) => this;
		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default) => this;

		#region 异步遍历
		T IAsyncEnumerator<T>.Current => default;
		ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync() => default;
		ValueTask IAsyncDisposable.DisposeAsync() => default;
		#endregion
	}

	private sealed class TypedEnumerable<T> : IEnumerable<T>
	{
		#region 私有变量
		private readonly Func<IEnumerator<T>> _iterator;
		#endregion

		#region 构造函数
		public TypedEnumerable(object source)
		{
			if(source is IEnumerable items && (source.GetType() != typeof(string) || typeof(T) == typeof(char)))
				_iterator = () => new MultitapEnumerator(items.GetEnumerator());
			else if(source is IAsyncEnumerable<T> enumerable)
				_iterator = () => enumerable.Synchronize().GetEnumerator();
			else
			{
				if(Zongsoft.Common.Convert.TryConvertValue<T>(source, out var element))
					_iterator = () => new SimulateEnumerator(element);
				else
					throw new InvalidOperationException($"The '{source.GetType()}' type cannot be convert to '{typeof(T)}' type.");
			}
		}
		#endregion

		#region 迭代遍历
		public IEnumerator<T> GetEnumerator() => _iterator();
		IEnumerator IEnumerable.GetEnumerator() => _iterator();
		#endregion

		#region 迭代实现
		private sealed class SimulateEnumerator(T element) : IEnumerator<T>
		{
			private int _flag;
			private readonly T _element = element;

			public T Current
			{
				get
				{
					if(_flag == 1)
						return _element;

					if(_flag == 0)
						throw new InvalidOperationException("The iterator has not yet started, please call the MoveNext() method first.");
					else
						throw new InvalidOperationException("The iterator has terminated.");
				}
			}

			object IEnumerator.Current => this.Current;

			public bool MoveNext()
			{
				if(_flag == 0)
				{
					_flag = 1;
					return true;
				}

				_flag = 2;
				return false;
			}

			public void Reset() => _flag = 0;
			public void Dispose() { }
		}

		private sealed class MultitapEnumerator(IEnumerator enumerator) : IEnumerator<T>
		{
			private readonly IEnumerator _enumerator = enumerator;

			public T Current => (T)_enumerator.Current;
			object IEnumerator.Current => _enumerator.Current;
			public bool MoveNext() => _enumerator.MoveNext();
			public void Reset() => _enumerator.Reset();

			public void Dispose()
			{
				if(_enumerator is IDisposable disposable)
					disposable.Dispose();
			}
		}
		#endregion
	}

	private sealed class AsyncEnumerable<T>(IEnumerable<T> items, CancellationToken cancellation) : IAsyncEnumerable<T>
	{
		private readonly IEnumerable<T> _items = items;
		private readonly CancellationToken _cancellation = cancellation;

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default)
		{
			var token = GetCancellation(_cancellation, cancellation, out var registration);

			try
			{
				return new AsyncEnumerator(_items.GetEnumerator(), token, registration);
			}
			catch
			{
				registration?.Dispose();
				throw;
			}
		}

		private sealed class AsyncEnumerator(IEnumerator<T> source, CancellationToken cancellation, CancellationTokenSource registration) : IAsyncEnumerator<T>
		{
			private IEnumerator<T> _source = source;
			private CancellationTokenSource _registration = registration;
			private readonly CancellationToken _cancellation = cancellation;

			public T Current => _source.Current;
			public ValueTask<bool> MoveNextAsync()
			{
				var source = _source;

				if(source == null)
					return ValueTask.FromResult(false);

				_cancellation.ThrowIfCancellationRequested();
				return ValueTask.FromResult(source.MoveNext());
			}

			public ValueTask DisposeAsync()
			{
				var source = Interlocked.Exchange(ref _source, null);
				var registration = Interlocked.Exchange(ref _registration, null);

				try
				{
					source?.Dispose();
					return ValueTask.CompletedTask;
				}
				finally
				{
					registration?.Dispose();
				}
			}
		}
	}

	private delegate bool TryConverter<TSource, TDestination>(TSource source, out TDestination destination);

	private sealed class AsyncCastEnumerable<TSource, TDestination>(IAsyncEnumerable<TSource> source, CancellationToken cancellation, TryConverter<TSource, TDestination> converter) : IAsyncEnumerable<TDestination>
	{
		private readonly IAsyncEnumerable<TSource> _source = source;
		private readonly CancellationToken _cancellation = cancellation;
		private readonly TryConverter<TSource, TDestination> _converter = converter;

		public IAsyncEnumerator<TDestination> GetAsyncEnumerator(CancellationToken cancellation = default)
		{
			var token = GetCancellation(_cancellation, cancellation, out var registration);

			try
			{
				return new AsyncCastEnumerator(_source.GetAsyncEnumerator(token), _converter, token, registration);
			}
			catch
			{
				registration?.Dispose();
				throw;
			}
		}

		private sealed class AsyncCastEnumerator(IAsyncEnumerator<TSource> source, TryConverter<TSource, TDestination> converter, CancellationToken cancellation, CancellationTokenSource registration) : IAsyncEnumerator<TDestination>
		{
			private TDestination _current;
			private IAsyncEnumerator<TSource> _source = source;
			private CancellationTokenSource _registration = registration;
			private readonly CancellationToken _cancellation = cancellation;
			private readonly TryConverter<TSource, TDestination> _converter = converter;

			public TDestination Current => _current;

			public async ValueTask<bool> MoveNextAsync()
			{
				var source = _source;

				while(source != null)
				{
					_cancellation.ThrowIfCancellationRequested();

					if(!await source.MoveNextAsync().ConfigureAwait(false))
						break;

					if(_converter(source.Current, out _current))
						return true;
				}

				_current = default;
				return false;
			}

			public ValueTask DisposeAsync()
			{
				var source = Interlocked.Exchange(ref _source, null);
				var registration = Interlocked.Exchange(ref _registration, null);
				_current = default;

				if(source == null)
				{
					registration?.Dispose();
					return ValueTask.CompletedTask;
				}

				return DisposeAsync(source, registration);

				static async ValueTask DisposeAsync(IAsyncEnumerator<TSource> source, CancellationTokenSource registration)
				{
					try
					{
						await source.DisposeAsync().ConfigureAwait(false);
					}
					finally
					{
						registration?.Dispose();
					}
				}
			}
		}
	}
	#endregion
}
