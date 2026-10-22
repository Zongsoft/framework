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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zongsoft.Data;

public static class Pageable
{
	#region 委托定义
	public delegate bool FilterDelegate<T>(ref T data);
	#endregion

	#region 公共方法
	public static IEnumerable Filter(this IEnumerable source, Func<object, bool> predicate)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		if(predicate == null)
			return source;

		var result = FilterIterator(source, predicate);
		return Wrap(result, source as IPageable);
	}

	public static IEnumerable<T> Filter<T>(this IEnumerable<T> source, FilterDelegate<T> predicate)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		if(predicate == null)
			return source;

		var result = FilterIterator(source, predicate);
		return Wrap(result, source as IPageable);
	}

	public static IAsyncEnumerable<T> Filter<T>(this IAsyncEnumerable<T> source, FilterDelegate<T> predicate)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		if(predicate == null)
			return source;

		var result = FilterIterator(source, predicate);
		return Wrap(result, source as IPageable);
	}

	public static IAsyncEnumerable<T> FilterAsync<T>(this IAsyncEnumerable<T> source, Func<T, CancellationToken, ValueTask<FilterResult<T>>> predicate)
	{
		ArgumentNullException.ThrowIfNull(source);

		if(predicate == null)
			return source;

		return Wrap(FilterAsyncIterator(source, predicate), source as IPageable);
	}

	public static IEnumerable<TResult> Map<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> mapper)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		if(mapper == null)
			throw new ArgumentNullException(nameof(mapper));

		var result = MapIterator(source, mapper);
		return Wrap(result, source as IPageable);
	}

	public static IAsyncEnumerable<TResult> Map<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> mapper)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		if(mapper == null)
			throw new ArgumentNullException(nameof(mapper));

		var result = MapIterator(source, mapper);
		return Wrap(result, source as IPageable);
	}

	public static IAsyncEnumerable<TResult> MapAsync<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask<TResult>> mapper)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(mapper);

		return Wrap(MapAsyncIterator(source, mapper), source as IPageable);
	}

	internal static IEnumerable<T> Wrap<T>(IEnumerable<T> source, IPageable pageable) => pageable == null ? source : new PageableEnumerable<T>(source, pageable);
	internal static IAsyncEnumerable<T> Wrap<T>(IAsyncEnumerable<T> source, IPageable pageable) => pageable == null ? source : new PageableAsyncEnumerable<T>(source, pageable);
	#endregion

	#region 私有方法
	private static IEnumerable<object> FilterIterator(IEnumerable source, Func<object, bool> predicate)
	{
		foreach(var item in source)
		{
			if(predicate(item))
				yield return item;
		}
	}

	private static IEnumerable<T> FilterIterator<T>(IEnumerable<T> source, FilterDelegate<T> predicate)
	{
		foreach(var item in source)
		{
			var current = item;

			if(predicate(ref current))
				yield return current;
		}
	}

	private static async IAsyncEnumerable<T> FilterIterator<T>(IAsyncEnumerable<T> source, FilterDelegate<T> predicate, [EnumeratorCancellation] CancellationToken cancellation = default)
	{
		await foreach(var item in source.WithCancellation(cancellation).ConfigureAwait(false))
		{
			var current = item;

			if(predicate(ref current))
				yield return current;
		}
	}

	private static async IAsyncEnumerable<T> FilterAsyncIterator<T>(IAsyncEnumerable<T> source, Func<T, CancellationToken, ValueTask<FilterResult<T>>> predicate, [EnumeratorCancellation] CancellationToken cancellation = default)
	{
		await foreach(var item in source.WithCancellation(cancellation).ConfigureAwait(false))
		{
			var result = await predicate(item, cancellation).ConfigureAwait(false);

			if(result.Accepted)
				yield return result.Data;
		}
	}

	private static IEnumerable<TResult> MapIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> mapper)
	{
		foreach(var item in source)
			yield return mapper(item);
	}

	private static async IAsyncEnumerable<TResult> MapIterator<TSource, TResult>(IAsyncEnumerable<TSource> source, Func<TSource, TResult> mapper, [EnumeratorCancellation] CancellationToken cancellation = default)
	{
		await foreach(var item in source.WithCancellation(cancellation).ConfigureAwait(false))
			yield return mapper(item);
	}

	private static async IAsyncEnumerable<TResult> MapAsyncIterator<TSource, TResult>(IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask<TResult>> mapper, [EnumeratorCancellation] CancellationToken cancellation = default)
	{
		await foreach(var item in source.WithCancellation(cancellation).ConfigureAwait(false))
			yield return await mapper(item, cancellation).ConfigureAwait(false);
	}
	#endregion

	#region 嵌套子类
	/// <summary>表示异步筛选是否接受当前记录，以及接受后输出的记录。</summary>
	public struct FilterResult<T>
	{
		/// <summary>指示是否输出当前记录。</summary>
		public bool Accepted;
		/// <summary>指定接受后输出的记录，允许为空。</summary>
		public T Data;
	}

	private abstract class PageableWrapper(IPageable pageable) : IPageable
	{
		private readonly object _syncRoot = new();
		private readonly IPageable _pageable = pageable;
		private Func<object, PagingEventArgs, CancellationToken, ValueTask> _paginated;

		public event Func<object, PagingEventArgs, CancellationToken, ValueTask> Paginated
		{
			add
			{
				if(value == null)
					return;

				lock(_syncRoot)
				{
					if(_paginated == null)
						_pageable.Paginated += this.OnPaginatedAsync;

					_paginated += value;
				}
			}
			remove
			{
				if(value == null)
					return;

				lock(_syncRoot)
				{
					if(_paginated == null)
						return;

					_paginated -= value;

					if(_paginated == null)
						_pageable.Paginated -= this.OnPaginatedAsync;
				}
			}
		}

		public bool Suppressed => _pageable.Suppressed;
		private async ValueTask OnPaginatedAsync(object sender, PagingEventArgs args, CancellationToken cancellation)
		{
			Func<object, PagingEventArgs, CancellationToken, ValueTask> paginated;

			lock(_syncRoot)
				paginated = _paginated;

			if(paginated != null)
			{
				foreach(Func<object, PagingEventArgs, CancellationToken, ValueTask> handler in paginated.GetInvocationList())
					await handler(this, args, cancellation);
			}
		}
	}

	private sealed class PageableEnumerable<T>(IEnumerable<T> source, IPageable pageable) : PageableWrapper(pageable), IEnumerable<T>, IEnumerable
	{
		IEnumerator IEnumerable.GetEnumerator() => source.GetEnumerator();
		public IEnumerator<T> GetEnumerator() => source.GetEnumerator();
	}

	private sealed class PageableAsyncEnumerable<T>(IAsyncEnumerable<T> source, IPageable pageable) : PageableWrapper(pageable), IAsyncEnumerable<T>
	{
		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default) => source.GetAsyncEnumerator(cancellation);
	}
	#endregion
}
