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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
	private List<T> _cache;
	private AutoResetEvent _semaphore;
	private Action<IEnumerable<T>> _accessor;
	#endregion

	#region 构造函数
	public Stash(Action<IEnumerable<T>> accessor, TimeSpan period, int limit = 0)
	{
		_accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
		_period = period.Ticks > TimeSpan.TicksPerSecond ? period : TimeSpan.FromSeconds(1);
		_limit = limit > 0 ? limit : 0;

		_cache = new(Math.Max(limit, 16));
		_timer = new Timer(this.OnTick, null, _period, _period);
		_semaphore = new AutoResetEvent(true);
	}
	#endregion

	#region 公共属性
	public int Count => _cache.Count;
	#endregion

	#region 公共方法
	public void Put(T item)
	{
		try
		{
			_semaphore.WaitOne();
			_cache.Add(item);
		}
		finally
		{
			_semaphore.Set();
		}

		if(_limit > 0 && _cache.Count > _limit)
			this.InvokeAccessor();
	}
	#endregion

	#region 时钟方法
	private void OnTick(object state)
	{
		//暂停计时器
		_timer.Change(Timeout.Infinite, Timeout.Infinite);

		//执行访问器
		this.InvokeAccessor();

		//恢复计时器
		_timer.Change(_period, _period);
	}
	#endregion

	#region 私有方法
	private void InvokeAccessor()
	{
		List<T> cache;

		if(_cache.Count == 0)
			return;

		try
		{
			_semaphore.WaitOne();
			cache = _cache;
			_cache = this.CreateCache();
		}
		finally
		{
			_semaphore.Set();
		}

		_accessor?.Invoke(cache);
		cache.Clear();
	}
	#endregion

	#region 私有方法
	private List<T> CreateCache() => new(Math.Max(_limit, 16));
	#endregion

	#region 处置方法
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		var timer = Interlocked.Exchange(ref _timer, null);

		if(timer != null)
		{
			if(disposing)
			{
				_cache.Clear();
				timer.Dispose();

				_semaphore.Dispose();
			}

			_accessor = null;
			_semaphore = null;
		}
	}
	#endregion
}
