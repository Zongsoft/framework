﻿/*
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

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

		if(source is IPageable)
			return new FilteredEnumerable(source, predicate);
		else
			return source.Cast<object>().Where(predicate);
	}

	public static IEnumerable<T> Filter<T>(this IEnumerable<T> source, FilterDelegate<T> predicate)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		if(predicate == null)
			return source;

		if(source is not IPageable)
			return source.Where(p => predicate(ref p));

		var collectionType = source is IPageable ?
			typeof(FilteredEnumerable<>).MakeGenericType(typeof(T)) :
			typeof(AsyncEnumerable<>).MakeGenericType(typeof(T));

		return (IEnumerable<T>)Activator.CreateInstance(collectionType, [source, predicate]);
	}

	public static IAsyncEnumerable<T> Filter<T>(this IAsyncEnumerable<T> source, FilterDelegate<T> predicate)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		if(predicate == null)
			return source;

		var collectionType = source is IPageable ?
			typeof(FilteredAsyncEnumerable<>).MakeGenericType(typeof(T)) :
			typeof(AsyncEnumerable<>).MakeGenericType(typeof(T));

		return (IAsyncEnumerable<T>)Activator.CreateInstance(collectionType, [source, predicate]);
	}

	public static IEnumerable<TResult> Map<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> mapper)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		if(mapper == null)
			throw new ArgumentNullException(nameof(mapper));

		if(source is not IPageable)
			return source.Select(mapper);

		var collectionType = typeof(MappedEnumerable<,>).MakeGenericType(typeof(TSource), typeof(TResult));
		return (IEnumerable<TResult>)Activator.CreateInstance(collectionType, [source, mapper]);
	}

	public static IAsyncEnumerable<TResult> Map<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> mapper)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		if(mapper == null)
			throw new ArgumentNullException(nameof(mapper));

		var collectionType = source is IPageable ?
			typeof(MappedAsyncEnumerable<,>).MakeGenericType(typeof(TSource), typeof(TResult)) :
			typeof(AsyncEnumerable<,>).MakeGenericType(typeof(TSource), typeof(TResult));

		return (IAsyncEnumerable<TResult>)Activator.CreateInstance(collectionType, [source, mapper]);
	}
	#endregion

	#region 嵌套子类
	private class FilteredEnumerable : IEnumerable, IPageable
	{
		#region 事件声明
		public event EventHandler<PagingEventArgs> Paginated;
		#endregion

		#region 私有变量
		private readonly IEnumerable _source;
		private readonly Func<object, bool> _filter;
		#endregion

		#region 构造函数
		public FilteredEnumerable(IEnumerable source, Func<object, bool> filter)
		{
			_filter = filter;
			_source = source;
			((IPageable)_source).Paginated += this.OnPaginated;
		}
		#endregion

		#region 公共属性
		public bool Suppressed => ((IPageable)_source).Suppressed;
		#endregion

		#region 枚举遍历
		public IEnumerator GetEnumerator() => new FilteredIterator(_source.GetEnumerator(), _filter, this.OnExit);
		#endregion

		#region 私有方法
		private void OnPaginated(object sender, PagingEventArgs args) => this.Paginated?.Invoke(sender, args);
		private void OnExit() => ((IPageable)_source).Paginated -= this.OnPaginated;
		#endregion

		#region 数据迭代
		private class FilteredIterator(IEnumerator iterator, Func<object, bool> filter, Action exit) : IEnumerator, IDisposable
		{
			private readonly IEnumerator _iterator = iterator;
			private readonly Func<object, bool> _filter = filter;
			private readonly Action _exit = exit;
			private object _current;

			public object Current => _current;

			public bool MoveNext()
			{
				while(_iterator.MoveNext())
				{
					var current = _iterator.Current;

					if(_filter(_current))
					{
						_current = current;
						return true;
					}
				}

				return false;
			}

			public void Reset() => _iterator.Reset();
			public void Dispose()
			{
				if(_iterator is IDisposable disposable)
					disposable.Dispose();

				_exit();
			}
		}
		#endregion
	}

	private class FilteredEnumerable<T> : IEnumerable<T>, IEnumerable, IPageable
	{
		#region 事件声明
		public event EventHandler<PagingEventArgs> Paginated;
		#endregion

		#region 私有变量
		private readonly IEnumerable<T> _source;
		private readonly FilterDelegate<T> _filter;
		#endregion

		#region 构造函数
		public FilteredEnumerable(IEnumerable<T> source, FilterDelegate<T> filter)
		{
			_filter = filter;
			_source = source;
			((IPageable)_source).Paginated += this.OnPaginated;
		}
		#endregion

		#region 公共属性
		public bool Suppressed => ((IPageable)_source).Suppressed;
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<T> GetEnumerator() => new FilteredIterator(_source.GetEnumerator(), _filter, this.OnExit);
		#endregion

		#region 私有方法
		private void OnPaginated(object sender, PagingEventArgs args) => this.Paginated?.Invoke(sender, args);
		private void OnExit() => ((IPageable)_source).Paginated -= this.OnPaginated;
		#endregion

		#region 数据迭代
		private class FilteredIterator(IEnumerator<T> iterator, FilterDelegate<T> filter, Action exit) : IEnumerator<T>, IEnumerator, IDisposable
		{
			private readonly IEnumerator<T> _iterator = iterator;
			private readonly FilterDelegate<T> _filter = filter;
			private readonly Action _exit = exit;
			private T _current;

			public T Current => _current;
			object IEnumerator.Current => _current;

			public bool MoveNext()
			{
				while(_iterator.MoveNext())
				{
					var current = _iterator.Current;

					if(_filter(ref _current))
					{
						_current = current;
						return true;
					}
				}

				return false;
			}

			public void Reset() => _iterator.Reset();
			public void Dispose()
			{
				if(_iterator is IDisposable disposable)
					disposable.Dispose();

				_exit();
			}
		}
		#endregion
	}

	private class FilteredAsyncEnumerable<T> : IAsyncEnumerable<T>, IPageable
	{
		#region 事件声明
		public event EventHandler<PagingEventArgs> Paginated;
		#endregion

		#region 私有变量
		private readonly IAsyncEnumerable<T> _source;
		private readonly FilterDelegate<T> _filter;
		#endregion

		#region 构造函数
		public FilteredAsyncEnumerable(IAsyncEnumerable<T> source, FilterDelegate<T> filter)
		{
			_filter = filter;
			_source = source;
			((IPageable)_source).Paginated += this.OnPaginated;
		}
		#endregion

		#region 公共属性
		public bool Suppressed => ((IPageable)_source).Suppressed;
		#endregion

		#region 枚举遍历
		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation) => new FilteredIterator(_source.GetAsyncEnumerator(cancellation), _filter, this.OnExit);
		#endregion

		#region 私有方法
		private void OnPaginated(object sender, PagingEventArgs args) => this.Paginated?.Invoke(sender, args);
		private void OnExit() => ((IPageable)_source).Paginated -= this.OnPaginated;
		#endregion

		#region 数据迭代
		private class FilteredIterator(IAsyncEnumerator<T> iterator, FilterDelegate<T> filter, Action exit) : IAsyncEnumerator<T>, IAsyncDisposable
		{
			private readonly IAsyncEnumerator<T> _iterator = iterator;
			private readonly FilterDelegate<T> _filter = filter;
			private readonly Action _exit = exit;
			private T _current;

			public T Current => _current;

			public async ValueTask<bool> MoveNextAsync()
			{
				if(await _iterator.MoveNextAsync())
				{
					_current = _iterator.Current;
					return _filter(ref _current);
				}

				return false;
			}

			public ValueTask DisposeAsync()
			{
				_exit();
				return _iterator.DisposeAsync();
			}
		}
		#endregion
	}

	private class MappedEnumerable<TSource, TResult> : IEnumerable<TResult>, IEnumerable, IPageable
	{
		#region 事件声明
		public event EventHandler<PagingEventArgs> Paginated;
		#endregion

		#region 私有变量
		private readonly IEnumerable<TSource> _source;
		private readonly Func<TSource, TResult> _mapper;
		#endregion

		#region 构造函数
		public MappedEnumerable(IEnumerable<TSource> source, Func<TSource, TResult> mapper)
		{
			_mapper = mapper;
			_source = source;
			((IPageable)_source).Paginated += this.OnPaginated;
		}
		#endregion

		#region 公共属性
		public bool Suppressed => ((IPageable)_source).Suppressed;
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<TResult> GetEnumerator() => new MappedIterator(_source.GetEnumerator(), _mapper, this.OnExit);
		#endregion

		#region 私有方法
		private void OnPaginated(object sender, PagingEventArgs args) => this.Paginated?.Invoke(sender, args);
		private void OnExit() => ((IPageable)_source).Paginated -= this.OnPaginated;
		#endregion

		#region 数据迭代
		private class MappedIterator(IEnumerator<TSource> iterator, Func<TSource, TResult> mapper, Action exit) : IEnumerator<TResult>, IEnumerator, IDisposable
		{
			private readonly IEnumerator<TSource> _iterator = iterator;
			private readonly Func<TSource, TResult> _mapper = mapper;
			private readonly Action _exit = exit;

			object IEnumerator.Current => _mapper(_iterator.Current);
			public TResult Current => _mapper(_iterator.Current);
			public bool MoveNext() => _iterator.MoveNext();
			public void Reset() => _iterator.Reset();

			public void Dispose()
			{
				_iterator.Dispose();
				_exit();
			}
		}
		#endregion
	}

	private class MappedAsyncEnumerable<TSource, TResult> : IAsyncEnumerable<TResult>, IPageable
	{
		#region 事件声明
		public event EventHandler<PagingEventArgs> Paginated;
		#endregion

		#region 私有变量
		private readonly IAsyncEnumerable<TSource> _source;
		private readonly Func<TSource, TResult> _mapper;
		#endregion

		#region 构造函数
		public MappedAsyncEnumerable(IAsyncEnumerable<TSource> source, Func<TSource, TResult> mapper)
		{
			_mapper = mapper;
			_source = source;
			((IPageable)_source).Paginated += this.OnPaginated;
		}
		#endregion

		#region 公共属性
		public bool Suppressed => ((IPageable)_source).Suppressed;
		#endregion

		#region 枚举遍历
		public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellation) => new MappedAsyncIterator(_source.GetAsyncEnumerator(cancellation), _mapper, this.OnExit);
		#endregion

		#region 私有方法
		private void OnPaginated(object sender, PagingEventArgs args) => this.Paginated?.Invoke(sender, args);
		private void OnExit() => ((IPageable)_source).Paginated -= this.OnPaginated;
		#endregion

		#region 数据迭代
		private class MappedAsyncIterator(IAsyncEnumerator<TSource> iterator, Func<TSource, TResult> mapper, Action exit) : IAsyncEnumerator<TResult>, IAsyncDisposable
		{
			private readonly IAsyncEnumerator<TSource> _iterator = iterator;
			private readonly Func<TSource, TResult> _mapper = mapper;
			private readonly Action _exit = exit;

			public TResult Current => _mapper(_iterator.Current);
			public ValueTask<bool> MoveNextAsync() => _iterator.MoveNextAsync();

			public ValueTask DisposeAsync()
			{
				_exit();
				return _iterator.DisposeAsync();
			}
		}
		#endregion
	}

	private sealed class AsyncEnumerable<T>(IAsyncEnumerable<T> source, FilterDelegate<T> predicator) : IAsyncEnumerable<T>
	{
		#region 私有变量
		private readonly IAsyncEnumerable<T> _source = source;
		private readonly FilterDelegate<T> _predicator = predicator;
		#endregion

		#region 枚举遍历
		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation) => new AsyncIterator(_source.GetAsyncEnumerator(cancellation), _predicator);
		#endregion

		#region 数据迭代
		private class AsyncIterator(IAsyncEnumerator<T> iterator, FilterDelegate<T> predicator) : IAsyncEnumerator<T>, IAsyncDisposable
		{
			private readonly IAsyncEnumerator<T> _iterator = iterator;
			private readonly FilterDelegate<T> _predicator = predicator;

			public T Current { get; private set; }
			public async ValueTask<bool> MoveNextAsync()
			{
				await _iterator.MoveNextAsync();

				while(await _iterator.MoveNextAsync())
				{
					var current = _iterator.Current;
					if(_predicator(ref current))
					{
						this.Current = current;
						return true;
					}
				}

				return false;
			}

			public ValueTask DisposeAsync() => _iterator.DisposeAsync();
		}
		#endregion
	}

	private sealed class AsyncEnumerable<TSource, TResult>(IAsyncEnumerable<TSource> source, Func<TSource, TResult> mapper) : IAsyncEnumerable<TResult>
	{
		#region 私有变量
		private readonly IAsyncEnumerable<TSource> _source = source;
		private readonly Func<TSource, TResult> _mapper = mapper;
		#endregion

		#region 枚举遍历
		public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellation) => new AsyncIterator(_source.GetAsyncEnumerator(cancellation), _mapper);
		#endregion

		#region 数据迭代
		private class AsyncIterator(IAsyncEnumerator<TSource> iterator, Func<TSource, TResult> mapper) : IAsyncEnumerator<TResult>, IAsyncDisposable
		{
			private readonly IAsyncEnumerator<TSource> _iterator = iterator;
			private readonly Func<TSource, TResult> _mapper = mapper;

			public TResult Current => _mapper(_iterator.Current);
			public ValueTask<bool> MoveNextAsync() => _iterator.MoveNextAsync();
			public ValueTask DisposeAsync() => _iterator.DisposeAsync();
		}
		#endregion
	}
	#endregion
}
