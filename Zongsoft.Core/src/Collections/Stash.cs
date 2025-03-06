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

namespace Zongsoft.Collections;

public class Stash<T> : IDisposable
{
	#region 成员字段
	private int _limit;
	private TimeSpan _period;
	#endregion

	#region 私有变量
	private Timer _timer;
	private IList<T> _cache;
	private AutoResetEvent _semaphore;
	private Action<IEnumerable<T>> _accessor;
	#endregion

	#region 构造函数
	public Stash(Action<IEnumerable<T>> accessor, TimeSpan period, int limit = 0)
	{
		_accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));

		this.Period = period;
		this.Limit = limit;

		_cache = this.RentCache();
		_semaphore = new AutoResetEvent(true);
		_timer = new Timer(this.OnTick, null, _period, _period);
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前暂存区的数量。</summary>
	public int Count => _cache.Count;

	/// <summary>获取一个值，指示当前暂存区是否空了。</summary>
	public bool IsEmpty => _cache.Count == 0;

	/// <summary>获取或设置暂存数量限制，如果暂存数量超过该属性值则立即触发回调。如果为零则表示忽略该限制。</summary>
	public int Limit
	{
		get => _limit;
		set => _limit = value > 0 ? value : 0;
	}

	/// <summary>获取或设置暂存的触发周期，不能低于<c>1</c>毫秒(Millisecond)。</summary>
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
		IList<T> cache;

		if(_cache == null || _cache.Count == 0)
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
			_cache = this.RentCache();
		}
		finally
		{
			_semaphore.Set();
		}

		//执行访问器回调
		this.OnFlush(cache);

		//归还回调完成的缓存对象
		this.ReturnCache(cache);
	}
	#endregion

	#region 虚拟方法
	protected virtual IList<T> RentCache() => new List<T>(Math.Max(_limit, 16));
	protected virtual void ReturnCache(IList<T> cache) => cache?.Clear();
	protected virtual void OnFlush(IList<T> cache) => _accessor?.Invoke(cache);
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
			this.ReturnCache(cache);

			_timer.Dispose();
			_semaphore.Dispose();
		}

		_timer = null;
		_accessor = null;
		_semaphore = null;
	}
	#endregion
}
