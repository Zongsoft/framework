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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;

namespace Zongsoft.Caching;

public class MemoryCacheScanner : IDisposable
{
	#region 成员字段
	private readonly Timer _timer;
	private MemoryCache _cache;
	#endregion

	#region 构造函数
	public MemoryCacheScanner(MemoryCache cache)
	{
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		_timer = new(
			static state => Volatile.Read(ref ((MemoryCacheScanner)state)._cache)?.Scan(),
			this, Timeout.InfiniteTimeSpan, cache.Options.ScanFrequency);
	}
	#endregion

	#region 公共方法
	public void Start()
	{
		lock(_timer)
		{
			ObjectDisposedException.ThrowIf(_cache == null, this);
			_timer.Change(TimeSpan.Zero, _cache.Options.ScanFrequency);
		}
	}

	public void Stop()
	{
		lock(_timer)
		{
			if(_cache != null)
				_timer.Change(Timeout.Infinite, Timeout.Infinite);
		}
	}
	#endregion

	#region 处置方法
	public void Dispose()
	{
		lock(_timer)
		{
			Volatile.Write(ref _cache, null);
			_timer.Dispose();
		}

		//禁止垃圾回收期调用终结方法
		GC.SuppressFinalize(this);
	}
	#endregion
}
