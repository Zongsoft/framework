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

		await using var enumerator = source.GetAsyncEnumerator(cancellation);
		return await enumerator.MoveNextAsync() ? enumerator.Current : throw new InvalidOperationException($"Sequence contains no elements.");
	}

	public static async ValueTask<T> FirstOrDefault<T>(this IAsyncEnumerable<T> source, CancellationToken cancellation = default)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		await using var enumerator = source.GetAsyncEnumerator(cancellation);
		return await enumerator.MoveNextAsync() ? enumerator.Current : default;
	}

	public static IEnumerable<T> Synchronize<T>(this IAsyncEnumerable<T> source, CancellationToken cancellation = default)
	{
		#if NET7_0_OR_GREATER
		return source.ToBlockingEnumerable(cancellation);
		#else
		var iterator = source.GetAsyncEnumerator(cancellation);

		while(true)
		{
			var task = iterator.MoveNextAsync();
			var succeed = task.IsCompletedSuccessfully ? task.Result : task.AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

			if(succeed)
				yield return iterator.Current;
			else
				break;
		}

		Dispose(iterator);

		static async void Dispose(IAsyncEnumerator<T> enumerator)
		{
			try
			{
				await enumerator.DisposeAsync();
			}
			catch { }
		}
		#endif
	}

	public static IAsyncEnumerable<T> Asynchronize<T>(this IEnumerable<T> source) =>
		source is IAsyncEnumerable<T> enumerable ? enumerable : (source == null ? EmptyEnumerable<T>.Instance : new AsyncEnumerable<T>(source));

	public static bool IsAsyncEnumerable(object source)
	{
		if(source == null)
			return false;

		var contracts = source.GetType().GetInterfaces();
		for(int i = 0; i < contracts.Length; i++)
		{
			if(contracts[i].IsGenericType && contracts[i].GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
				return true;
		}

		return false;
	}

	public static bool IsAsyncEnumerable(object source, out Type elementType)
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

	public static IAsyncEnumerable<T> EnumerateAsync<T>(object source, CancellationToken cancellation = default)
	{
		if(source == null)
			return EmptyEnumerable<T>.Instance;

		if(source is IAsyncEnumerable<T> enumerable)
			return enumerable;

		if(source is IEnumerable<T> e)
			return new AsyncEnumerable<T>(e);

		else if(source is T item)
			return new AsyncEnumerable<T>([item]);

		throw new InvalidOperationException($"The '{source.GetType()}' type cannot be convert to '{typeof(IAsyncEnumerable<T>)}' type.");
	}

	public static IEnumerable<T> Enumerate<T>(object source)
	{
		if(source == null)
			return [];

		if(source is IEnumerable<T> items)
			return items;

		return (IEnumerable<T>)System.Activator.CreateInstance(typeof(TypedEnumerable<>).MakeGenericType(typeof(T)), [source]);
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

		return new ArrayEnumerator<T>(array);
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

	#region 嵌套子类
	private sealed class ArrayEnumerator<T>(T[] array) : IEnumerator<T>
	{
		private int _index = -1;
		private T[] _array = array ?? throw new ArgumentNullException(nameof(array));

		public T Current => _index >= 0 && _index < _array.Length ? _array[_index] : default;
		object IEnumerator.Current => this.Current;
		public bool MoveNext() => ++_index < _array.Length;
		public void Reset() => _index = -1;
		public void Dispose() => _array = null;
	}

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
			else
			{
				if(Zongsoft.Common.Convert.TryConvertValue<T>(source, out var element))
					_iterator = () => new SimulateEnumerator(element);
				else if(IsAsyncEnumerable(source))
					_iterator = () => ((IAsyncEnumerable<T>)source).Synchronize().GetEnumerator();
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
		private class SimulateEnumerator(T element) : IEnumerator<T>
		{
			private volatile int _flag;
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
				var original = Interlocked.CompareExchange(ref _flag, 1, 0);

				if(original == 0)
					return true;

				Interlocked.CompareExchange(ref _flag, 2, 1);
				return false;
			}

			public void Reset() => _flag = 0;

			public void Dispose()
			{
				if(_element is IDisposable disposable)
					disposable.Dispose();
			}
		}

		private class MultitapEnumerator(IEnumerator enumerator) : IEnumerator<T>
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

	private sealed class AsyncEnumerable<T>(IEnumerable<T> items) : IAsyncEnumerable<T>
	{
		private readonly IEnumerable<T> _items = items;

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default) => new AsyncEnumerator(_items.GetEnumerator(), cancellation);

		private class AsyncEnumerator(IEnumerator<T> source, CancellationToken cancellation) : IAsyncEnumerator<T>
		{
			private readonly IEnumerator<T> _source = source;
			private readonly CancellationToken _cancellation = cancellation;

			public T Current => _source.Current;
			public ValueTask<bool> MoveNextAsync() => _cancellation.IsCancellationRequested ? ValueTask.FromResult(false) : ValueTask.FromResult(_source.MoveNext());
			public ValueTask DisposeAsync()
			{
				_source.Dispose();
				return ValueTask.CompletedTask;
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
			var token = GetCancellation(_cancellation, cancellation, out var source);
			return new AsyncCastEnumerator(_source.GetAsyncEnumerator(token), _converter, source);
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

		private sealed class AsyncCastEnumerator(IAsyncEnumerator<TSource> source, TryConverter<TSource, TDestination> converter, CancellationTokenSource cancellation) : IAsyncEnumerator<TDestination>
		{
			private TDestination _current;
			private IAsyncEnumerator<TSource> _source = source;
			private CancellationTokenSource _cancellation = cancellation;
			private readonly TryConverter<TSource, TDestination> _converter = converter;

			public TDestination Current => _current;

			public async ValueTask<bool> MoveNextAsync()
			{
				var source = _source;

				while(source != null && await source.MoveNextAsync().ConfigureAwait(false))
				{
					if(_converter(source.Current, out _current))
						return true;
				}

				return false;
			}

			public ValueTask DisposeAsync()
			{
				var source = Interlocked.Exchange(ref _source, null);
				var cancellation = Interlocked.Exchange(ref _cancellation, null);

				if(source == null)
				{
					cancellation?.Dispose();
					return ValueTask.CompletedTask;
				}

				return DisposeAsync(source, cancellation);

				static async ValueTask DisposeAsync(IAsyncEnumerator<TSource> source, CancellationTokenSource cancellation)
				{
					try
					{
						await source.DisposeAsync().ConfigureAwait(false);
					}
					finally
					{
						cancellation?.Dispose();
					}
				}
			}
		}
	}
	#endregion
}
