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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.ObjectPool;

namespace Zongsoft.Caching;

public class Stash<T> : IDisposable
{
	#region 成员字段
	private int _limit;
	private TimeSpan _period;
	#endregion

	#region 私有变量
	private Timer _timer;
	private List<T> _cache;
	private AutoResetEvent _semaphore;
	private Action<IReadOnlyList<T>> _flusher;
	private DefaultObjectPool<List<T>> _pool;
	#endregion

	#region 构造函数
	public Stash(Action<IReadOnlyList<T>> flusher, TimeSpan period, int limit = 0) : this(period, limit)
	{
		_flusher = flusher ?? throw new ArgumentNullException(nameof(flusher));
	}

	protected Stash(TimeSpan period, int limit = 0)
	{
		this.Period = period;
		this.Limit = limit;

		_pool = new DefaultObjectPool<List<T>>(new StashPooledPolicy(this));
		_cache = this.OnRent();
		_semaphore = new AutoResetEvent(true);
		_timer = new Timer(this.OnTick, null, _period, _period);
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前暂存区的数量。</summary>
	public int Count => _cache.Count;

	/// <summary>获取一个值，指示当前暂存区是否空了。</summary>
	public bool IsEmpty => _cache.Count == 0;

	/// <summary>获取或设置暂存数量限制，如果暂存数量超过该属性值则立即触发刷新回调。如果为零则表示忽略该限制。</summary>
	public int Limit
	{
		get => _limit;
		set => _limit = value > 0 ? value : 0;
	}

	/// <summary>获取或设置暂存的刷新周期，不能低于<c>1</c>毫秒(Millisecond)。</summary>
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
	public void Discard()
	{
		if(_cache.Count == 0)
			return;

		try
		{
			_semaphore.WaitOne();
			_cache.Clear();
		}
		finally
		{
			_semaphore.Set();
		}
	}

	public void Put(T value)
	{
		try
		{
			_semaphore.WaitOne();
			_cache.Add(value);
		}
		finally
		{
			_semaphore.Set();
		}

		if(_limit > 0 && _cache.Count >= _limit)
			this.Flush();
	}

	public bool TryTake(out T value) => this.TryTake(0, out value);
	public bool TryTake(int index, out T value)
	{
		try
		{
			_semaphore.WaitOne();

			if(index < 0)
				index = _cache.Count + index;

			if(_cache.Count == 0 || index < 0 || index > _cache.Count - 1)
			{
				value = default;
				return false;
			}

			value = _cache[index];
			_cache.RemoveAt(index);
			return true;
		}
		finally
		{
			_semaphore.Set();
		}
	}

	public void Flush()
	{
		List<T> cache;

		if(_cache == null)
			return;

		try
		{
			_semaphore.WaitOne();

			//获取当前缓存对象
			cache = _cache;

			//如果当前缓存为空，表示本实例已被处置
			if(cache == null)
				return;

			//重新租用一个新的缓存对象
			_cache = this.OnRent();
		}
		finally
		{
			_semaphore.Set();
		}

		//执行刷新回调
		this.OnFlush(cache);

		//归还回调完成的缓存对象
		this.OnReturn(cache);
	}
	#endregion

	#region 虚拟方法
	protected virtual List<T> OnRent() => _pool.Get();
	protected virtual void OnReturn(List<T> cache) => _pool.Return(cache);
	protected virtual void OnFlush(List<T> cache) => _flusher?.Invoke(cache);
	#endregion

	#region 时钟方法
	private void OnTick(object state)
	{
		//暂停计时器
		_timer.Change(Timeout.Infinite, Timeout.Infinite);

		try
		{
			//推送数据
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
			this.OnReturn(cache);

			_timer.Dispose();
			_semaphore.Dispose();
		}

		_pool = null;
		_timer = null;
		_flusher = null;
		_semaphore = null;
	}
	#endregion

	#region 嵌套子类
	private sealed class StashPooledPolicy(Stash<T> stash) : PooledObjectPolicy<List<T>>
	{
		private readonly Stash<T> _stash = stash;
		public override List<T> Create() => new(Math.Clamp(_stash.Limit, 16, 4 * 1024 * 1024));
		public override bool Return(List<T> list) { list.Clear(); return true; }
	}
	#endregion
}
