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
			_cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new MemoryCacheOptions());
		}

		public MemoryCache(TimeSpan frequency, long sizeLimit = 0)
		{
			var options = new MemoryCacheOptions()
			{
				ExpirationScanFrequency = frequency,
				SizeLimit = sizeLimit > 0 ? sizeLimit : null,
			};

			_cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(options);
		}
		#endregion

		#region 公共属性
		public int Count => _cache.Count;
		#endregion

		#region 存在方法
		public bool Exists(object key) => key is not null && _cache.TryGetValue(key, out _);
		#endregion

		#region 删除方法
#if NET7_0_OR_GREATER
		public void Clear() => _cache.Clear();
#else
		public void Clear() => _cache.Compact(1.0);
#endif
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
		#endregion

		#region 获取设置
		public TValue GetOrCreate<TValue>(object key, Func<ICacheEntry, TValue> factory) => _cache.GetOrCreate(key, factory);
		public Task<TValue> GetOrCreateAsync<TValue>(object key, Func<ICacheEntry, Task<TValue>> factory) => _cache.GetOrCreateAsync(key, factory);

		public TValue GetOrCreate<TValue>(object key, Func<TValue> factory) => _cache.GetOrCreate(key, entry => factory == null ? default : factory.Invoke());
		public Task<TValue> GetOrCreateAsync<TValue>(object key, Func<Task<TValue>> factory) => _cache.GetOrCreateAsync(key, entry => factory == null ? default : factory.Invoke());

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, TimeSpan Expiration, Action<object, object, CacheEvictionReason> Evicted)> factory)
		{
			return _cache.GetOrCreate(key, entry =>
			{
				(var value, var expiration, var evicted) = factory(entry.Key);

				entry.SlidingExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason)));

				return value;
			});
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, TimeSpan Expiration, Action<object, object, CacheEvictionReason> Evicted)> factory)
		{
			return _cache.GetOrCreate(key, entry =>
			{
				(var value, var priority, var expiration, var evicted) = factory(entry.Key);

				entry.Priority = GetPriority(priority);
				entry.SlidingExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason)));

				return value;
			});
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, TimeSpan Expiration, object State, Action<object, object, CacheEvictionReason, object> Evicted)> factory)
		{
			return _cache.GetOrCreate(key, entry =>
			{
				(var value, var expiration, var state, var evicted) = factory(entry.Key);

				entry.SlidingExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason), state), state);

				return value;
			});
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, TimeSpan Expiration, object State, Action<object, object, CacheEvictionReason, object> Evicted)> factory)
		{
			return _cache.GetOrCreate(key, entry =>
			{
				(var value, var priority, var expiration, var state, var evicted) = factory(entry.Key);

				entry.Priority = GetPriority(priority);
				entry.SlidingExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason), state), state);

				return value;
			});
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, DateTimeOffset Expiration, Action<object, object, CacheEvictionReason> Evicted)> factory)
		{
			return _cache.GetOrCreate(key, entry =>
			{
				(var value, var expiration, var evicted) = factory(entry.Key);

				entry.AbsoluteExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason)));

				return value;
			});
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, DateTimeOffset Expiration, Action<object, object, CacheEvictionReason> Evicted)> factory)
		{
			return _cache.GetOrCreate(key, entry =>
			{
				(var value, var priority, var expiration, var evicted) = factory(entry.Key);

				entry.Priority = GetPriority(priority);
				entry.AbsoluteExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason)));

				return value;
			});
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, DateTimeOffset Expiration, object State, Action<object, object, CacheEvictionReason, object> Evicted)> factory)
		{
			return _cache.GetOrCreate(key, entry =>
			{
				(var value, var expiration, var state, var evicted) = factory(entry.Key);

				entry.AbsoluteExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason), state), state);

				return value;
			});
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, DateTimeOffset Expiration, object State, Action<object, object, CacheEvictionReason, object> Evicted)> factory)
		{
			return _cache.GetOrCreate(key, entry =>
			{
				(var value, var priority, var expiration, var state, var evicted) = factory(entry.Key);

				entry.Priority = GetPriority(priority);
				entry.AbsoluteExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason), state), state);

				return value;
			});
		}

		public Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, TimeSpan Expiration, Action<object, object, CacheEvictionReason> Evicted)> factory)
		{
			return _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var expiration, var evicted) = factory(entry.Key);

				entry.SlidingExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason)));

				return value;
			});
		}

		public Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, TimeSpan Expiration, Action<object, object, CacheEvictionReason> Evicted)> factory)
		{
			return _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var priority, var expiration, var evicted) = factory(entry.Key);

				entry.Priority = GetPriority(priority);
				entry.SlidingExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason)));

				return value;
			});
		}

		public Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, TimeSpan Expiration, object State, Action<object, object, CacheEvictionReason, object> Evicted)> factory)
		{
			return _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var expiration, var state, var evicted) = factory(entry.Key);

				entry.SlidingExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason), state), state);

				return value;
			});
		}

		public Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, TimeSpan Expiration, object State, Action<object, object, CacheEvictionReason, object> Evicted)> factory)
		{
			return _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var priority, var expiration, var state, var evicted) = factory(entry.Key);

				entry.Priority = GetPriority(priority);
				entry.SlidingExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason), state), state);

				return value;
			});
		}

		public Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, DateTimeOffset Expiration, Action<object, object, CacheEvictionReason> Evicted)> factory)
		{
			return _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var expiration, var evicted) = factory(entry.Key);

				entry.AbsoluteExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason)));

				return value;
			});
		}

		public Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, DateTimeOffset Expiration, Action<object, object, CacheEvictionReason> Evicted)> factory)
		{
			return _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var priority, var expiration, var evicted) = factory(entry.Key);

				entry.Priority = GetPriority(priority);
				entry.AbsoluteExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason)));

				return value;
			});
		}

		public Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, DateTimeOffset Expiration, object State, Action<object, object, CacheEvictionReason, object> Evicted)> factory)
		{
			return _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var expiration, var state, var evicted) = factory(entry.Key);

				entry.AbsoluteExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason), state), state);

				return value;
			});
		}

		public Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, DateTimeOffset Expiration, object State, Action<object, object, CacheEvictionReason, object> Evicted)> factory)
		{
			return _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var priority, var expiration, var state, var evicted) = factory(entry.Key);

				entry.Priority = GetPriority(priority);
				entry.AbsoluteExpiration = expiration;
				if(evicted != null)
					entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason), state), state);

				return value;
			});
		}
		#endregion

		#region 设置方法
		public void SetValue<TValue>(object key, TValue value, Action<object, object, CacheEvictionReason> evicted = null) => this.SetValue(key, value, CachePriority.Normal, evicted);
		public void SetValue<TValue>(object key, TValue value, CachePriority priority, Action<object, object, CacheEvictionReason> evicted = null)
		{
			using var entry = _cache.CreateEntry(key);
			entry.Value = value;
			entry.Priority = GetPriority(priority);

			if(evicted != null)
				entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason)));
		}

		public void SetValue<TValue>(object key, TValue value, object state, Action<object, object, CacheEvictionReason, object> evicted = null) => this.SetValue(key, value, CachePriority.Normal, state, evicted);
		public void SetValue<TValue>(object key, TValue value, CachePriority priority, object state, Action<object, object, CacheEvictionReason, object> evicted = null)
		{
			using var entry = _cache.CreateEntry(key);
			entry.Value = value;
			entry.Priority = GetPriority(priority);

			if(evicted != null)
				entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason), state), state);
		}

		public void SetValue<TValue>(object key, TValue value, DateTimeOffset expiration, Action<object, object, CacheEvictionReason> evicted = null) => this.SetValue(key, value, CachePriority.Normal, expiration, evicted);
		public void SetValue<TValue>(object key, TValue value, CachePriority priority, DateTimeOffset expiration, Action<object, object, CacheEvictionReason> evicted = null)
		{
			using var entry = _cache.CreateEntry(key);
			entry.Value = value;
			entry.Priority = GetPriority(priority);
			entry.AbsoluteExpiration = expiration;

			if(evicted != null)
				entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason)));
		}

		public void SetValue<TValue>(object key, TValue value, DateTimeOffset expiration, object state, Action<object, object, CacheEvictionReason, object> evicted = null) => this.SetValue(key, value, CachePriority.Normal, expiration, state, evicted);
		public void SetValue<TValue>(object key, TValue value, CachePriority priority, DateTimeOffset expiration, object state, Action<object, object, CacheEvictionReason, object> evicted = null)
		{
			using var entry = _cache.CreateEntry(key);
			entry.Value = value;
			entry.Priority = GetPriority(priority);
			entry.AbsoluteExpiration = expiration;

			if(evicted != null)
				entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason), state), state);
		}

		public void SetValue<TValue>(object key, TValue value, TimeSpan expiration, Action<object, object, CacheEvictionReason> evicted = null) => this.SetValue(key, value, CachePriority.Normal, expiration, evicted);
		public void SetValue<TValue>(object key, TValue value, CachePriority priority, TimeSpan expiration, Action<object, object, CacheEvictionReason> evicted = null)
		{
			using var entry = _cache.CreateEntry(key);
			entry.Value = value;
			entry.Priority = GetPriority(priority);
			entry.SlidingExpiration = expiration;

			if(evicted != null)
				entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason)));
		}

		public void SetValue<TValue>(object key, TValue value, TimeSpan expiration, object state, Action<object, object, CacheEvictionReason, object> evicted = null) => this.SetValue(key, value, CachePriority.Normal, expiration, state, evicted);
		public void SetValue<TValue>(object key, TValue value, CachePriority priority, TimeSpan expiration, object state, Action<object, object, CacheEvictionReason, object> evicted = null)
		{
			using var entry = _cache.CreateEntry(key);
			entry.Value = value;
			entry.Priority = GetPriority(priority);
			entry.SlidingExpiration = expiration;

			if(evicted != null)
				entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason), state), state);
		}

		public void SetValue<TValue>(object key, TValue value, IChangeToken dependency, Action<object, object, CacheEvictionReason> evicted = null) => this.SetValue(key, value, CachePriority.Normal, dependency, evicted);
		public void SetValue<TValue>(object key, TValue value, CachePriority priority, IChangeToken dependency, Action<object, object, CacheEvictionReason> evicted = null)
		{
			using var entry = _cache.CreateEntry(key);
			entry.Value = value;
			entry.Priority = GetPriority(priority);
			entry.AddExpirationToken(dependency);

			if(evicted != null)
				entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason)));
		}

		public void SetValue<TValue>(object key, TValue value, IChangeToken dependency, object state, Action<object, object, CacheEvictionReason, object> evicted = null) => this.SetValue(key, value, CachePriority.Normal, dependency, state, evicted);
		public void SetValue<TValue>(object key, TValue value, CachePriority priority, IChangeToken dependency, object state, Action<object, object, CacheEvictionReason, object> evicted = null)
		{
			using var entry = _cache.CreateEntry(key);
			entry.Value = value;
			entry.Priority = GetPriority(priority);
			entry.AddExpirationToken(dependency);

			if(evicted != null)
				entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) => evicted(key, value, GetReason(reason), state), state);
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static CacheItemPriority GetPriority(CachePriority priority) => priority switch
		{
			CachePriority.Normal => CacheItemPriority.Normal,
			CachePriority.Low => CacheItemPriority.Low,
			CachePriority.High => CacheItemPriority.High,
			CachePriority.NeverRemove => CacheItemPriority.NeverRemove,
			_ => CacheItemPriority.Normal,
		};

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static CacheEvictionReason GetReason(EvictionReason reason) => reason switch
		{
			EvictionReason.Removed => CacheEvictionReason.Removed,
			EvictionReason.Expired => CacheEvictionReason.Expired,
			EvictionReason.Capacity => CacheEvictionReason.Overfull,
			EvictionReason.Replaced => CacheEvictionReason.Replaced,
			EvictionReason.TokenExpired => CacheEvictionReason.Expired,
			_ => CacheEvictionReason.None,
		};
		#endregion

		#region 处置方法
		public void Dispose()
		{
			//确保单例的共享内存缓存实例不能被释放
			if(object.ReferenceEquals(this, Shared))
				return;

			_cache.Dispose();
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
