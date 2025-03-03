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

namespace Zongsoft.Caching;

public class MemoryCacheScanner : IDisposable
{
	#region 成员字段
	private Timer _timer;
	private MemoryCache _cache;
	#endregion

	#region 构造函数
	public MemoryCacheScanner(MemoryCache cache)
	{
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		_timer = new(OnTick, _cache, Timeout.InfiniteTimeSpan, cache.Options.ScanFrequency);
	}
	#endregion

	#region 公共方法
	public void Start() => _timer.Change(TimeSpan.Zero, _cache.Options.ScanFrequency);
	public void Stop() => _timer.Change(Timeout.Infinite, Timeout.Infinite);
	#endregion

	#region 时钟方法
	private static void OnTick(object state)
	{
		if(state is MemoryCache cache)
			cache.Compact(0);
	}
	#endregion

	#region 处置方法
	public void Dispose()
	{
		var timer = Interlocked.Exchange(ref _timer, null);
		if(timer != null)
		{
			timer.Change(Timeout.Infinite, Timeout.Infinite);
			timer.Dispose();

			_cache = null;
		}

		//禁止垃圾回收期调用终结方法
		GC.SuppressFinalize(this);
	}
	#endregion
}
