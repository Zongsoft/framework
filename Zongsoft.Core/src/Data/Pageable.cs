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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	public static class Pageable
	{
		#region 委托定义
		public delegate bool FilterDelegate(ref object data);
		#endregion

		#region 静态字段
		private static readonly MethodInfo FilterMethod = typeof(Pageable).GetMethod(
				nameof(Filter),
				BindingFlags.Public | BindingFlags.Static,
				null,
				CallingConventions.Standard,
				new Type[] { typeof(IPageable), typeof(FilterDelegate) },
				null);
		#endregion

		#region 公共方法
		public static IEnumerable Filter(this IPageable source, Type elementType, FilterDelegate predicate)
		{
			if(elementType == null)
				throw new ArgumentNullException(nameof(elementType));

			var method = FilterMethod.MakeGenericMethod(elementType);
			return (IEnumerable)method.Invoke(null, new object[] { source, predicate });
		}

		public static IEnumerable<T> Filter<T>(this IPageable source, FilterDelegate predicate)
		{
			if(source == null)
				throw new ArgumentNullException(nameof(source));

			if(predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			return new FilteredCollection<T>(source, predicate);
		}

		public static IEnumerable<TResult> Map<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> map)
		{
			if(source == null)
				throw new ArgumentNullException(nameof(source));

			if(map == null)
				throw new ArgumentNullException(nameof(map));

			var pageable = source as IPageable;

			if(pageable == null)
				return source.Select(map);

			var collectionType = typeof(MappedCollection<,>).MakeGenericType(typeof(TSource), typeof(TResult));
			return (IEnumerable<TResult>)Activator.CreateInstance(collectionType, new object[] { source, map });
		}
		#endregion

		#region 嵌套子类
		private class FilteredCollection<T> : IEnumerable<T>, IEnumerable, IPageable
		{
			#region 事件声明
			public event EventHandler<PagingEventArgs> Paginated;
			#endregion

			#region 私有变量
			private IEnumerable _source;
			private FilterDelegate _filter;
			#endregion

			#region 构造函数
			public FilteredCollection(IEnumerable source, FilterDelegate filter)
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
			public IEnumerator<T> GetEnumerator()
			{
				return new FilteredIterator(_source.GetEnumerator(), _filter, this.OnExit);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}
			#endregion

			#region 私有方法
			private void OnPaginated(object sender, PagingEventArgs e)
			{
				this.Paginated?.Invoke(sender, e);
			}

			private void OnExit()
			{
				((IPageable)_source).Paginated -= this.OnPaginated;
			}
			#endregion

			#region 数据迭代
			private class FilteredIterator : IEnumerator<T>, IEnumerator, IDisposable
			{
				private IEnumerator _iterator;
				private FilterDelegate _filter;
				private Action _exit;
				private object _current;

				public FilteredIterator(IEnumerator iterator, FilterDelegate filter, Action exit)
				{
					_iterator = iterator;
					_filter = filter;
					_exit = exit;
				}

				public T Current
				{
					get => (T)_current;
				}

				object IEnumerator.Current
				{
					get => _current;
				}

				public bool MoveNext()
				{
					if(_iterator.MoveNext())
					{
						_current = _iterator.Current;
						return _filter(ref _current);
					}

					return false;
				}

				public void Reset()
				{
					_iterator.Reset();
				}

				public void Dispose()
				{
					if(_iterator is IDisposable disposable)
						disposable.Dispose();

					_exit();
				}
			}
			#endregion
		}

		private class MappedCollection<TSource, TResult> : IEnumerable<TResult>, IEnumerable, IPageable
		{
			#region 事件声明
			public event EventHandler<PagingEventArgs> Paginated;
			#endregion

			#region 私有变量
			private readonly IEnumerable<TSource> _source;
			private readonly Func<TSource, TResult> _map;
			#endregion

			#region 构造函数
			public MappedCollection(IEnumerable<TSource> source, Func<TSource, TResult> map)
			{
				_map = map;
				_source = source;
				((IPageable)_source).Paginated += this.OnPaginated;
			}
			#endregion

			#region 公共属性
			public bool Suppressed => ((IPageable)_source).Suppressed;
			#endregion

			#region 枚举遍历
			public IEnumerator<TResult> GetEnumerator()
			{
				return new MappedIterator(_source.GetEnumerator(), _map, this.OnExit);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}
			#endregion

			#region 私有方法
			private void OnPaginated(object sender, PagingEventArgs e)
			{
				this.Paginated?.Invoke(sender, e);
			}

			private void OnExit()
			{
				((IPageable)_source).Paginated -= this.OnPaginated;
			}
			#endregion

			#region 数据迭代
			private class MappedIterator : IEnumerator<TResult>, IEnumerator, IDisposable
			{
				private IEnumerator<TSource> _iterator;
				private Func<TSource, TResult> _map;
				private Action _exit;

				public MappedIterator(IEnumerator<TSource> iterator, Func<TSource, TResult> map, Action exit)
				{
					_iterator = iterator;
					_map = map;
					_exit = exit;
				}

				public TResult Current
				{
					get => (TResult)_map(_iterator.Current);
				}

				public bool MoveNext()
				{
					return _iterator.MoveNext();
				}

				public void Reset()
				{
					_iterator.Reset();
				}

				public void Dispose()
				{
					_iterator.Dispose();
					_exit();
				}

				object IEnumerator.Current
				{
					get => _map(_iterator.Current);
				}
			}
			#endregion
		}
		#endregion
	}
}
