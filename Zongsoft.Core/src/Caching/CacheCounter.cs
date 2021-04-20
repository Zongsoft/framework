/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Caching
{
	public abstract class CacheCounter<TKey> : IDisposable where TKey : IEquatable<TKey>
	{
		#region 成员字段
		private Timer _timer;
		private readonly int _limit;
		private readonly TimeSpan _duration;
		private readonly ConcurrentDictionary<TKey, Entry> _entries;
		#endregion

		#region 构造函数
		protected CacheCounter() : this(null, TimeSpan.Zero) { }

		protected CacheCounter(TimeSpan duration, int limit = 256) : this(null, duration, limit) { }

		protected CacheCounter(IEqualityComparer<TKey> comparer, TimeSpan duration, int limit = 256)
		{
			_limit = Math.Max(10, limit);
			_duration = duration == TimeSpan.Zero ? TimeSpan.FromSeconds(60) : duration;
			_timer = new Timer(this.OnTick, null, _duration, _duration);
			_entries = new ConcurrentDictionary<TKey, Entry>(comparer ?? EqualityComparer<TKey>.Default);
		}
		#endregion

		#region 公共方法
		public int Increase(TKey key, int interval = 1)
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));

			var entry = _entries.GetOrAdd(key, (key, value) => new Entry(key, 0), interval);
			var value = entry.Increase(interval);

			if(_entries.Count >= _limit)
				this.Flush();

			return value;
		}

		public void Flush()
		{
			if(!_entries.IsEmpty)
			{
				var entries = new List<Entry>(_entries.Count);

				foreach(var key in _entries.Keys)
				{
					if(_entries.TryRemove(key, out var entry))
						entries.Add(entry);
				}

				this.OnFlush(entries);
			}
		}
		#endregion

		#region 抽象方法
		protected abstract void OnFlush(ICollection<Entry> entries);
		#endregion

		#region 定时回调
		private void OnTick(object state)
		{
			this.Flush();
		}
		#endregion

		#region 处置方法
		protected virtual void Dispose(bool disposing)
		{
			var timer = Interlocked.Exchange(ref _timer, null);

			if(timer != null)
			{
				if(disposing)
				{
					timer.Change(Timeout.Infinite, Timeout.Infinite);
					timer.Dispose();

					//持久化缓存
					this.Flush();

					//清空缓存
					_entries?.Clear();
				}
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		#region 嵌套子类
		protected class Entry
		{
			private readonly TKey _key;
			private int _value;

			internal Entry(TKey key, int value = 0)
			{
				_key = key;
				_value = value;
			}

			public TKey Key => _key;
			public int Value => _value;

			public void Reset() => _value = 0;
			internal int Increase(int interval) => interval == 1 ? Interlocked.Increment(ref _value) : Interlocked.Add(ref _value, interval);

			public override string ToString() => $"{_key}:{_value}";
		}
		#endregion
	}
}
