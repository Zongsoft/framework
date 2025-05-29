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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Caching;

/// <summary>提供数据缓冲功能的类。</summary>
/// <typeparam name="T">指定的缓冲数据类型。</typeparam>
public class Spooler<T> : IEnumerable<T>, IDisposable
{
	#region 成员字段
	private int _limit;
	private TimeSpan _period;
	#endregion

	#region 私有变量
	private Timer _timer;
	private ICache _cache;
	private Action<IEnumerable<T>> _flusher;
	#endregion

	#region 构造函数
	public Spooler(Action<IEnumerable<T>> flusher, TimeSpan period, int limit = 0) : this(false, period, limit) =>
		_flusher = flusher ?? throw new ArgumentNullException(nameof(flusher));
	public Spooler(Action<IEnumerable<T>> flusher, bool distinct, TimeSpan period, int limit = 0) : this(distinct, period, limit) =>
		_flusher = flusher ?? throw new ArgumentNullException(nameof(flusher));

	protected Spooler(bool distinct, TimeSpan period, int limit = 0)
	{
		this.Period = period;
		this.Limit = limit;

		_cache = distinct ? new DistinctedCache() : new Cache();
		_timer = new Timer(this.OnTick, null, _period, _period);
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前缓冲区的数量。</summary>
	public int Count => _cache.Count;

	/// <summary>获取一个值，指示当前缓冲区是否空了。</summary>
	public bool IsEmpty => _cache.IsEmpty;

	/// <summary>获取或设置缓冲数量限制，如果缓冲数量超过该属性值则立即触发刷新回调。如果为零则表示忽略该限制。</summary>
	public int Limit
	{
		get => _limit;
		set => _limit = value > 0 ? value : 0;
	}

	/// <summary>获取或设置缓冲的刷新周期，不能低于<c>1</c>毫秒(Millisecond)。</summary>
	public TimeSpan Period
	{
		get => _period;
		set
		{
			var period = value.Ticks > TimeSpan.TicksPerMillisecond ? value : TimeSpan.FromMilliseconds(1);

			if(_period == period)
				return;

			_period = period;
			_timer?.Change(period, period);
		}
	}
	#endregion

	#region 公共方法
	public void Clear() => _cache?.Clear();

	public void Put(T value)
	{
		//如果设置失败，则触发刷新后重写
		if(!_cache.Add(value))
		{
			//刷新缓冲区
			this.Flush();

			//将设置失败的值写入新缓冲区
			this.Put(value);
		}

		if(_limit > 0 && _cache.Count >= _limit)
			this.Flush();
	}

	public void Flush()
	{
		var cache = _cache;

		//如果当前缓存为空，表示本实例已被处置
		if(cache == null)
			return;

		//如果当前缓存无内容，则无需后续操作
		if(cache.IsEmpty)
			return;

		var items = cache.Flush();

		//执行刷新回调
		if(items != null)
			this.OnFlush(items);
	}
	#endregion

	#region 虚拟方法
	protected virtual void OnFlush(IEnumerable<T> items) => _flusher?.Invoke(items);
	#endregion

	#region 时钟方法
	private void OnTick(object state)
	{
		//暂停计时器
		_timer.Change(Timeout.Infinite, Timeout.Infinite);

		try
		{
			//刷新缓冲区
			this.Flush();
		}
		finally
		{
			//恢复计时器
			_timer.Change(_period, _period);
		}
	}
	#endregion

	#region 处置方法
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		var cache = Interlocked.Exchange(ref _cache, null);

		if(cache == null)
			return;

		if(disposing)
		{
			cache.Clear();
			_timer.Dispose();
		}

		_timer = null;
		_flusher = null;
	}
	#endregion

	#region 枚举遍历
	public IEnumerator<T> GetEnumerator() => _cache?.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	#endregion

	#region 嵌套子类
	private interface ICache : IEnumerable<T>
	{
		int Count { get; }
		bool IsEmpty { get; }
		void Clear();
		bool Add(T value);
		IEnumerable<T> Flush();
	}

	private class Cache : ICache
	{
#if NET9_0_OR_GREATER
		protected readonly Lock _lock;
#else
		protected readonly object _lock;
#endif

		private volatile ConcurrentBag<T> _current;
		private readonly ConcurrentBag<T> _cacheA;
		private readonly ConcurrentBag<T> _cacheB;

		public Cache()
		{
			_lock = new();
			_cacheA = new();
			_cacheB = new();
			_current = _cacheA;
		}

		public int Count => _current.Count;
		public bool IsEmpty => _current.IsEmpty;
		public void Clear() => _current.Clear();

		public bool Add(T value)
		{
			if(value is null)
				return true;

			_current.Add(value);
			return true;
		}

		public IEnumerable<T> Flush()
		{
			if(_current.IsEmpty)
				return null;

			lock(_lock)
			{
				var current = _current;

				if(object.ReferenceEquals(_current, _cacheA))
					_current = _cacheB;
				else
					_current = _cacheA;

				var result = current.ToArray();
				current.Clear();
				return result.Length > 0 ? result : null;
			}
		}

		public IEnumerator<T> GetEnumerator() => _current.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	}

	private class DistinctedCache : ICache
	{
#if NET9_0_OR_GREATER
		protected readonly Lock _lock;
#else
		protected readonly object _lock;
#endif

		private volatile ConcurrentDictionary<T, object> _current;
		private readonly ConcurrentDictionary<T, object> _cacheA;
		private readonly ConcurrentDictionary<T, object> _cacheB;

		public DistinctedCache()
		{
			_lock = new();
			_cacheA = new();
			_cacheB = new();
			_current = _cacheA;
		}

		public int Count => _current.Count;
		public bool IsEmpty => _current.IsEmpty;
		public void Clear() => _current.Clear();

		public bool Add(T value)
		{
			if(value is null)
				return true;

			return _current.TryAdd(value, null);
		}

		public IEnumerable<T> Flush()
		{
			if(_current.IsEmpty)
				return null;

			lock(_lock)
			{
				var current = _current;

				if(object.ReferenceEquals(_current, _cacheA))
					_current = _cacheB;
				else
					_current = _cacheA;

				var result = current.Keys;
				current.Clear();
				return result.Count > 0 ? result : null;
			}
		}

		public IEnumerator<T> GetEnumerator() => _current.Keys.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	}
	#endregion
}
