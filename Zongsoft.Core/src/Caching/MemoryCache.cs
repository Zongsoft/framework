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

using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Caching.Memory;

namespace Zongsoft.Caching
{
	public class MemoryCache : IDisposable
	{
		#region 单例字段
		public static readonly MemoryCache Shared = new();
		#endregion

		#region 事件声明
		public event EventHandler<CacheLimitedEventArgs> Limited;
		public event EventHandler<CacheEvictedEventArgs> Evicted;
		#endregion

		#region 成员字段
		private readonly MemoryCacheOptions _options;
		private Microsoft.Extensions.Caching.Memory.MemoryCache _cache;
		#endregion

		#region 构造函数
		public MemoryCache() : this(TimeSpan.FromSeconds(60), 0) { }
		public MemoryCache(TimeSpan frequency, int limit = 0)
		{
			_options = new MemoryCacheOptions(frequency, limit);
			_cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()
			{
				ExpirationScanFrequency = _options.ScanFrequency,
			});
		}
		#endregion

		#region 公共属性
		public int Count => _cache.Count;
		public MemoryCacheOptions Options => _options;
		#endregion

		#region 存在方法
		public bool Exists(object key) => key is not null && _cache.TryGetValue(key, out _);
		#endregion

		#region 压缩方法
		public void Compact(double percentage) => _cache.Compact(percentage);
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
		public TValue GetOrCreate<TValue>(object key, Func<TValue> factory)
		{
			var result = _cache.GetOrCreate(key, entry => factory == null ? default : factory.Invoke());

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, TimeSpan Expiration)> factory)
		{
			var result = _cache.GetOrCreate(key, entry =>
			{
				(var value, var expiration) = factory(entry.Key);

				if(expiration > TimeSpan.Zero)
				{
					entry.SlidingExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, TimeSpan Expiration)> factory)
		{
			var result = _cache.GetOrCreate(key, entry =>
			{
				(var value, var priority, var expiration) = factory(entry.Key);
				entry.Priority = GetPriority(priority);

				if(expiration > TimeSpan.Zero)
				{
					entry.SlidingExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, TimeSpan Expiration, object State)> factory)
		{
			var result = _cache.GetOrCreate(key, entry =>
			{
				(var value, var expiration, var state) = factory(entry.Key);

				if(expiration > TimeSpan.Zero)
				{
					entry.SlidingExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted, state);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, TimeSpan Expiration, object State)> factory)
		{
			var result = _cache.GetOrCreate(key, entry =>
			{
				(var value, var priority, var expiration, var state) = factory(entry.Key);
				entry.Priority = GetPriority(priority);

				if(expiration > TimeSpan.Zero)
				{
					entry.SlidingExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted, state);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, DateTimeOffset Expiration)> factory)
		{
			var result = _cache.GetOrCreate(key, entry =>
			{
				(var value, var expiration) = factory(entry.Key);

				if(expiration > DateTimeOffset.MinValue)
				{
					entry.AbsoluteExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, DateTimeOffset Expiration)> factory)
		{
			var result = _cache.GetOrCreate(key, entry =>
			{
				(var value, var priority, var expiration) = factory(entry.Key);
				entry.Priority = GetPriority(priority);

				if(expiration > DateTimeOffset.MinValue)
				{
					entry.AbsoluteExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, DateTimeOffset Expiration, object State)> factory)
		{
			var result = _cache.GetOrCreate(key, entry =>
			{
				(var value, var expiration, var state) = factory(entry.Key);

				if(expiration > DateTimeOffset.MinValue)
				{
					entry.AbsoluteExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted, state);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, DateTimeOffset Expiration, object State)> factory)
		{
			var result = _cache.GetOrCreate(key, entry =>
			{
				(var value, var priority, var expiration, var state) = factory(entry.Key);
				entry.Priority = GetPriority(priority);

				if(expiration > DateTimeOffset.MinValue)
				{
					entry.AbsoluteExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted, state);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, IChangeToken Dependency)> factory)
		{
			var result = _cache.GetOrCreate(key, entry =>
			{
				(var value, var dependency) = factory(entry.Key);

				if(dependency != null)
				{
					entry.AddExpirationToken(dependency);
					entry.RegisterPostEvictionCallback(this.OnEvicted);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, IChangeToken Dependency)> factory)
		{
			var result = _cache.GetOrCreate(key, entry =>
			{
				(var value, var priority, var dependency) = factory(entry.Key);
				entry.Priority = GetPriority(priority);

				if(dependency != null)
				{
					entry.AddExpirationToken(dependency);
					entry.RegisterPostEvictionCallback(this.OnEvicted);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, IChangeToken Dependency, object State)> factory)
		{
			var result = _cache.GetOrCreate(key, entry =>
			{
				(var value, var dependency, var state) = factory(entry.Key);

				if(dependency != null)
				{
					entry.AddExpirationToken(dependency);
					entry.RegisterPostEvictionCallback(this.OnEvicted, state);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, IChangeToken Dependency, object State)> factory)
		{
			var result = _cache.GetOrCreate(key, entry =>
			{
				(var value, var priority, var dependency, var state) = factory(entry.Key);
				entry.Priority = GetPriority(priority);

				if(dependency != null)
				{
					entry.AddExpirationToken(dependency);
					entry.RegisterPostEvictionCallback(this.OnEvicted, state);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public async Task<TValue> GetOrCreateAsync<TValue>(object key, Func<Task<TValue>> factory)
		{
			var result = await _cache.GetOrCreateAsync(key, entry => factory == null ? default : factory.Invoke());

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public async Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, TimeSpan Expiration)> factory)
		{
			var result = await _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var expiration) = factory(entry.Key);

				if(expiration > TimeSpan.Zero)
				{
					entry.SlidingExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public async Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, TimeSpan Expiration)> factory)
		{
			var result = await _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var priority, var expiration) = factory(entry.Key);
				entry.Priority = GetPriority(priority);

				if(expiration > TimeSpan.Zero)
				{
					entry.SlidingExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public async Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, TimeSpan Expiration, object State)> factory)
		{
			var result = await _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var expiration, var state) = factory(entry.Key);

				if(expiration > TimeSpan.Zero)
				{
					entry.SlidingExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted, state);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public async Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, TimeSpan Expiration, object State)> factory)
		{
			var result = await _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var priority, var expiration, var state) = factory(entry.Key);
				entry.Priority = GetPriority(priority);

				if(expiration > TimeSpan.Zero)
				{
					entry.SlidingExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted, state);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public async Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, DateTimeOffset Expiration)> factory)
		{
			var result = await _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var expiration) = factory(entry.Key);

				if(expiration > DateTimeOffset.MinValue)
				{
					entry.AbsoluteExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public async Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, DateTimeOffset Expiration)> factory)
		{
			var result = await _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var priority, var expiration) = factory(entry.Key);
				entry.Priority = GetPriority(priority);

				if(expiration > DateTimeOffset.MinValue)
				{
					entry.AbsoluteExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public async Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, DateTimeOffset Expiration, object State)> factory)
		{
			var result = await _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var expiration, var state) = factory(entry.Key);

				if(expiration > DateTimeOffset.MinValue)
				{
					entry.AbsoluteExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted, state);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public async Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, DateTimeOffset Expiration, object State)> factory)
		{
			var result = await _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var priority, var expiration, var state) = factory(entry.Key);
				entry.Priority = GetPriority(priority);

				if(expiration > DateTimeOffset.MinValue)
				{
					entry.AbsoluteExpiration = expiration;
					entry.RegisterPostEvictionCallback(this.OnEvicted, state);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public async Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, IChangeToken Dependency)> factory)
		{
			var result = await _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var dependency) = factory(entry.Key);

				if(dependency != null)
				{
					entry.AddExpirationToken(dependency);
					entry.RegisterPostEvictionCallback(this.OnEvicted);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public async Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, IChangeToken Dependency)> factory)
		{
			var result = await _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var priority, var dependency) = factory(entry.Key);
				entry.Priority = GetPriority(priority);

				if(dependency != null)
				{
					entry.AddExpirationToken(dependency);
					entry.RegisterPostEvictionCallback(this.OnEvicted);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public async Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, IChangeToken Dependency, object State)> factory)
		{
			var result = await _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var dependency, var state) = factory(entry.Key);

				if(dependency != null)
				{
					entry.AddExpirationToken(dependency);
					entry.RegisterPostEvictionCallback(this.OnEvicted, state);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}

		public async Task<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, IChangeToken Dependency, object State)> factory)
		{
			var result = await _cache.GetOrCreateAsync(key, entry =>
			{
				(var value, var priority, var dependency, var state) = factory(entry.Key);
				entry.Priority = GetPriority(priority);

				if(dependency != null)
				{
					entry.AddExpirationToken(dependency);
					entry.RegisterPostEvictionCallback(this.OnEvicted, state);
				}

				return value;
			});

			if(_options.IsLimit(out var limit) && _cache.Count > limit)
				this.OnLimited(_cache.Count - limit);

			return result;
		}
		#endregion

		#region 设置方法
		public void SetValue<TValue>(object key, TValue value, object state = null) => this.SetValue(key, value, CachePriority.Normal, state);
		public void SetValue<TValue>(object key, TValue value, CachePriority priority, object state = null)
		{
			using var entry = _cache.CreateEntry(key);
			entry.Value = value;
			entry.Priority = GetPriority(priority);

			if(state != null)
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);

			if(_options.IsLimit(out var limit) && _cache.Count >= limit)
				this.OnLimited(_cache.Count - limit + 1, _cache.Count + 1);
		}

		public void SetValue<TValue>(object key, TValue value, TimeSpan expiration, object state = null) => this.SetValue(key, value, CachePriority.Normal, expiration, state);
		public void SetValue<TValue>(object key, TValue value, CachePriority priority, TimeSpan expiration, object state = null)
		{
			using var entry = _cache.CreateEntry(key);
			entry.Value = value;
			entry.Priority = GetPriority(priority);

			if(expiration > TimeSpan.Zero)
			{
				entry.SlidingExpiration = expiration;
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);
			}

			if(_options.IsLimit(out var limit) && _cache.Count >= limit)
				this.OnLimited(_cache.Count - limit + 1, _cache.Count + 1);
		}

		public void SetValue<TValue>(object key, TValue value, DateTimeOffset expiration, object state = null) => this.SetValue(key, value, CachePriority.Normal, expiration, state);
		public void SetValue<TValue>(object key, TValue value, CachePriority priority, DateTimeOffset expiration, object state = null)
		{
			using var entry = _cache.CreateEntry(key);
			entry.Value = value;
			entry.Priority = GetPriority(priority);

			if(expiration > DateTimeOffset.MinValue)
			{
				entry.AbsoluteExpiration = expiration;
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);
			}

			if(_options.IsLimit(out var limit) && _cache.Count >= limit)
				this.OnLimited(_cache.Count - limit + 1, _cache.Count + 1);
		}

		public void SetValue<TValue>(object key, TValue value, IChangeToken dependency, object state = null) => this.SetValue(key, value, CachePriority.Normal, dependency, state);
		public void SetValue<TValue>(object key, TValue value, CachePriority priority, IChangeToken dependency, object state = null)
		{
			using var entry = _cache.CreateEntry(key);
			entry.Value = value;
			entry.Priority = GetPriority(priority);

			if(dependency != null)
			{
				entry.AddExpirationToken(dependency);
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);
			}

			if(_options.IsLimit(out var limit) && _cache.Count >= limit)
				this.OnLimited(_cache.Count - limit + 1, _cache.Count + 1);
		}
		#endregion

		#region 事件触发
		private void OnLimited(int limit, int count = 0) => this.OnLimited(new CacheLimitedEventArgs(limit, count > 0 ? count : _cache.Count));
		protected virtual void OnLimited(CacheLimitedEventArgs args) => this.Limited?.Invoke(this, args);

		private void OnEvicted(object key, object value, EvictionReason reason, object state) => this.OnEvicted(new CacheEvictedEventArgs(key, value, GetReason(reason), state));
		protected virtual void OnEvicted(CacheEvictedEventArgs args) => this.Evicted?.Invoke(this, args);
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
		private static CacheEvictedReason GetReason(EvictionReason reason) => reason switch
		{
			EvictionReason.Removed => CacheEvictedReason.Removed,
			EvictionReason.Expired => CacheEvictedReason.Expired,
			EvictionReason.Capacity => CacheEvictedReason.Overfull,
			EvictionReason.Replaced => CacheEvictedReason.Replaced,
			EvictionReason.TokenExpired => CacheEvictedReason.Expired,
			_ => CacheEvictedReason.None,
		};
		#endregion

		#region 处置方法
		public void Dispose()
		{
			//确保单例的共享内存缓存实例不能被释放
			if(object.ReferenceEquals(this, Shared))
				return;

			var cache = Interlocked.Exchange(ref _cache, null);

			if(cache != null)
			{
				//移除“Limited”事件的所有处理函数
				var handlers = this.Limited.GetInvocationList();
				for(int i = 0; i < handlers.Length; i++)
					this.Limited -= (EventHandler<CacheLimitedEventArgs>)handlers[i];

				//移除“Evicted”事件的所有处理函数
				handlers = this.Evicted.GetInvocationList();
				for(int i = 0; i < handlers.Length; i++)
					this.Evicted -= (EventHandler<CacheEvictedEventArgs>)handlers[i];

				cache.Dispose();
				GC.SuppressFinalize(this);
			}
		}
		#endregion
	}
}
