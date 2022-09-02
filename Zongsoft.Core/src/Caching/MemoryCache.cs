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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Caching.Memory;

namespace Zongsoft.Caching
{
	public class MemoryCache : IDisposable
	{
		#region 单例字段
		public static readonly MemoryCache Shared = new();
		#endregion

		#region 成员字段
		private readonly Microsoft.Extensions.Caching.Memory.MemoryCache _cache;
		#endregion

		#region 构造函数
		public MemoryCache()
		{
			_cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(Options.Create(new MemoryCacheOptions() { }));
		}
		#endregion

		#region 公共属性
		public int Count => _cache.Count;
		#endregion

		#region 删除方法
		public void Clear() => _cache.Compact(1.0);
		public bool Remove(object key)
		{
			if(_cache.TryGetValue(key, out _))
			{
				_cache.Remove(key);
				return true;
			}

			return false;
		}
		#endregion

		#region 获取方法
		public object GetValue(object key) => _cache.Get(key);
		public TValue GetValue<TValue>(object key) => _cache.Get<TValue>(key);

		public bool TryGetValue(object key, out object value) => _cache.TryGetValue(key, out value);
		public bool TryGetValue<TValue>(object key, out TValue value) => _cache.TryGetValue(key, out value);

		public object GetOrCreate<TValue>(object key, Func<ICacheEntry, TValue> factory) => _cache.GetOrCreate(key, factory);
		public object GetOrCreate<TValue>(object key, Func<ICacheEntry, Task<TValue>> factory) => _cache.GetOrCreateAsync(key, factory);
		#endregion

		#region 设置方法
		public void SetValue<TValue>(object key, TValue value) => _cache.Set<TValue>(key, value);
		public void SetValue<TValue>(object key, TValue value, DateTime expires) => _cache.Set(key, value, expires);
		public void SetValue<TValue>(object key, TValue value, TimeSpan expires) => _cache.Set(key, value, expires);
		public void SetValue<TValue>(object key, TValue value, IChangeToken expiration) => _cache.Set(key, value, expiration);
		#endregion

		#region 处置方法
		public void Dispose()
		{
			if(object.ReferenceEquals(this, Shared))
				return;

			_cache.Dispose();
		}
		#endregion
	}
}
