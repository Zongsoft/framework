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
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Caching
{
	public class MemoryCache : ICache, IEnumerable<KeyValuePair<string, object>>
	{
		#region 事件声明
		public event EventHandler<CacheChangedEventArgs> Changed;
		#endregion

		#region 成员字段
		private readonly ConcurrentDictionary<string, CacheEntry> _cache;
		private readonly Expirator _expirator;
		#endregion

		#region 构造函数
		public MemoryCache()
		{
			this.Name = typeof(MemoryCache).FullName + "#" + Environment.TickCount64.ToString("X");
			_cache = new ConcurrentDictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
			_expirator = new Expirator(OnExpired, _cache);
		}

		public MemoryCache(string name)
		{
			this.Name = name;
			_cache = new ConcurrentDictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
			_expirator = new Expirator(OnExpired, _cache);
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get;
		}

		public bool IsEmpty
		{
			get => _cache.IsEmpty;
		}

		public int Count
		{
			get => _cache.Count;
		}
		#endregion

		#region 公共方法
		public void Clear()
		{
			var isEmpty = _cache.IsEmpty;

			_cache.Clear();
			_expirator.Clear();

			if(!isEmpty && this.Changed != null)
				this.OnChanged(new CacheChangedEventArgs(CacheChangedReason.Removed, null, null));
		}

		public Task ClearAsync(CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			this.Clear();
			return Task.CompletedTask;
		}

		public bool Exists(string key)
		{
			if(key == null)
				return false;

			return _cache.ContainsKey(key);
		}

		public Task<bool> ExistsAsync(string key, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			return Task.FromResult(key == null ? false : _cache.ContainsKey(key));
		}

		public long GetCount()
		{
			return _cache.Count;
		}

		public Task<long> GetCountAsync(CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			return Task.FromResult((long)_cache.Count);
		}

		public TimeSpan? GetExpiry(string key)
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));

			if(_cache.TryGetValue(key, out var entry))
				return entry.Expiry;

			return null;
		}

		public Task<TimeSpan?> GetExpiryAsync(string key, CancellationToken cancellation = default)
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			return Task.FromResult(this.GetExpiry(key));
		}

		public object GetValue(string key)
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));

			if(_cache.TryGetValue(key, out var entry))
				return entry.Value;

			return null;
		}

		public T GetValue<T>(string key)
		{
			return (T)this.GetValue(key);
		}

		public object GetValue(string key, out TimeSpan? expiry)
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));

			if(_cache.TryGetValue(key, out var entry))
			{
				expiry = entry.Expiry;
				return entry.Value;
			}

			expiry = null;
			return null;
		}

		public T GetValue<T>(string key, out TimeSpan? expiry)
		{
			return (T)this.GetValue(key, out expiry);
		}

		public Task<object> GetValueAsync(string key, CancellationToken cancellation = default)
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();

			if(_cache.TryGetValue(key, out var entry))
				return Task.FromResult(entry.Value);

			return Task.FromResult<object>(null);
		}

		public async Task<T> GetValueAsync<T>(string key, CancellationToken cancellation = default)
		{
			return (T)await this.GetValueAsync(key, cancellation);
		}

		public Task<(object Value, TimeSpan? Expiry)> GetValueExpiryAsync(string key, CancellationToken cancellation = default)
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();

			if(_cache.TryGetValue(key, out var entry))
				return Task.FromResult((entry.Value, entry.Expiry));

			return Task.FromResult<(object, TimeSpan?)>((null, null));
		}

		public async Task<(T Value, TimeSpan? Expiry)> GetValueExpiryAsync<T>(string key, CancellationToken cancellation = default)
		{
			var (value, expiry) = await this.GetValueExpiryAsync(key, cancellation);
			return ((T)value, expiry);
		}

		public bool Remove(string key)
		{
			if(key != null && _cache.TryRemove(key, out var entry))
			{
				if(entry.Expiry.HasValue)
					_expirator.Persist(key);

				this.OnChanged(CacheChangedEventArgs.Removed(key, entry.Value));
				return true;
			}

			return false;
		}

		public int Remove(IEnumerable<string> keys)
		{
			if(keys == null)
				return 0;

			int count = 0;

			foreach(var key in keys)
			{
				if(_cache.TryRemove(key, out var entry))
				{
					count++;

					if(entry.Expiry.HasValue)
						_expirator.Persist(key);

					this.OnChanged(CacheChangedEventArgs.Removed(key, entry.Value));
				}
			}

			return count;
		}

		public Task<bool> RemoveAsync(string key, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			return Task.FromResult(this.Remove(key));
		}

		public Task<int> RemoveAsync(IEnumerable<string> keys, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			return Task.FromResult(this.Remove(keys));
		}

		public bool Rename(string oldKey, string newKey)
		{
			if(oldKey == null)
				throw new ArgumentNullException(nameof(oldKey));
			if(newKey == null)
				throw new ArgumentNullException(nameof(newKey));

			if(_cache.TryGetValue(oldKey, out var entry) && _cache.TryAdd(newKey, entry))
			{
				if(_cache.TryRemove(oldKey, out _))
					return true;

				_cache.TryRemove(newKey, out _);
			}

			return false;
		}

		public Task<bool> RenameAsync(string oldKey, string newKey, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			return Task.FromResult(this.Rename(oldKey, newKey));
		}

		public bool SetExpiry(string key, TimeSpan expiry)
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));

			if(_cache.TryGetValue(key, out var entry))
			{
				entry.Expiry = expiry;

				if(expiry == TimeSpan.Zero)
					_expirator.Persist(key);
				else
					_expirator.Expire(key, expiry);

				return true;
			}

			return false;
		}

		public Task<bool> SetExpiryAsync(string key, TimeSpan expiry, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			return Task.FromResult(this.SetExpiry(key, expiry));
		}

		public bool SetValue(string key, object value, CacheRequisite requisite = CacheRequisite.Always)
		{
			return this.SetValue(key, value, TimeSpan.Zero, requisite);
		}

		public bool SetValue(string key, object value, TimeSpan expiry, CacheRequisite requisite = CacheRequisite.Always)
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));

			CacheEntry entry = null;

			switch(requisite)
			{
				case CacheRequisite.Exists:
					while(_cache.TryGetValue(key, out var original))
					{
						if(entry == null)
							entry = new CacheEntry(value, expiry);

						if(_cache.TryUpdate(key, entry, original))
						{
							_expirator.Expire(key, expiry);
							this.OnChanged(CacheChangedEventArgs.Updated(key, value, original.Value));
							return true;
						}
					}

					return false;
				case CacheRequisite.NotExists:
					if(_cache.TryAdd(key, new CacheEntry(value, expiry)))
					{
						_expirator.Expire(key, expiry);
						return true;
					}

					return false;
			}

			_cache.AddOrUpdate(key,
				_ => new CacheEntry(value, expiry),
				(_, original) =>
				{
					entry = original;
					return new CacheEntry(value, expiry);
				});

			_expirator.Expire(key, expiry);

			if(entry != null)
				this.OnChanged(CacheChangedEventArgs.Updated(key, value, entry.Value));

			return true;
		}

		public Task<bool> SetValueAsync(string key, object value, CacheRequisite requisite = CacheRequisite.Always, CancellationToken cancellation = default)
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			return Task.FromResult(this.SetValue(key, value, TimeSpan.Zero, requisite));
		}

		public Task<bool> SetValueAsync(string key, object value, TimeSpan expiry, CacheRequisite requisite = CacheRequisite.Always, CancellationToken cancellation = default)
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));

			cancellation.ThrowIfCancellationRequested();
			return Task.FromResult(this.SetValue(key, value, expiry, requisite));
		}
		#endregion

		#region 过期回调
		private void OnExpired(string key, object value)
		{
			this.OnChanged(CacheChangedEventArgs.Expired(key, value));
		}
		#endregion

		#region 激发事件
		protected virtual void OnChanged(CacheChangedEventArgs args)
		{
			this.Changed?.Invoke(this, args);
		}
		#endregion

		#region 遍历迭代
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			foreach(var entry in _cache)
				yield return new KeyValuePair<string, object>(entry.Key, entry.Value.Value);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		#endregion

		#region 嵌套子类
		private class CacheEntry : IEquatable<CacheEntry>
		{
			public readonly object Value;
			public TimeSpan? Expiry;

			public CacheEntry(object value, TimeSpan? expiry)
			{
				this.Value = value;

				if(expiry.HasValue && expiry.Value > TimeSpan.Zero)
					this.Expiry = expiry;
			}

			public bool Equals(CacheEntry other)
			{
				if(other == null)
					return false;

				return object.Equals(this.Value, other.Value);
			}

			public override bool Equals(object obj)
			{
				if(obj == null || obj.GetType() != typeof(CacheEntry))
					return false;

				return this.Equals((CacheEntry)obj);
			}

			public override int GetHashCode()
			{
				if(this.Value == null)
					return 0;
				else
					return this.Value.GetHashCode();
			}

			public override string ToString()
			{
				if(this.Expiry.HasValue)
					return "[" + this.Expiry.Value.ToString() + "] " + this.Value;
				else
					return this.Value?.ToString();
			}
		}

		private class Expirator
		{
			private readonly Action<string, object> _expired;
			private readonly ConcurrentDictionary<string, CacheEntry> _cache;
			private readonly ISet<string> _keys;

			private long _expiration;
			private CancellationTokenSource _cancellation;
			private AutoResetEvent _semaphore;

			public Expirator(Action<string, object> expired, ConcurrentDictionary<string, CacheEntry> cache)
			{
				_expired = expired ?? throw new ArgumentNullException(nameof(expired));
				_cache = cache ?? throw new ArgumentNullException(nameof(cache));

				_keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				_semaphore = new AutoResetEvent(true);
				_expiration = long.MaxValue;
			}

			public void Clear()
			{
				_cancellation?.Cancel();
				_keys.Clear();
			}

			public void Expire(string key, TimeSpan expiry)
			{
				if(key == null)
					return;

				if(expiry == TimeSpan.Zero)
				{
					this.Persist(key);
					return;
				}

				var expiration = GetExpiration(expiry);

				_semaphore.WaitOne();

				try
				{
					if(expiration == _expiration)
					{
						_keys.Add(key);
					}
					else if(expiration < _expiration)
					{
						_cancellation?.Cancel();
						_cancellation = new CancellationTokenSource();

						_keys.Clear();
						_keys.Add(key);
						_expiration = expiration;

						Task.Delay(expiry, _cancellation.Token).ContinueWith(DoWork);
					}
				}
				finally
				{
					_semaphore.Set();
				}
			}

			public void Persist(string key)
			{
				if(key == null)
					return;

				_semaphore.WaitOne();

				try
				{
					if(_keys.Remove(key) && _keys.Count == 0)
					{
						_cancellation.Cancel();
						this.Scan();
					}
				}
				finally
				{
					_semaphore.Set();
				}
			}

			private void DoWork(Task task)
			{
				if(task.IsCanceled)
					return;

				_semaphore.WaitOne();

				try
				{
					if(task.IsCanceled)
						return;

					if(_keys.Count > 0)
					{
						foreach(var key in _keys)
						{
							if(_cache.TryRemove(key, out var entry))
								Task.Run(() => _expired(key, entry.Value));
						}

						_keys.Clear();
					}

					this.Scan();
				}
				finally
				{
					_semaphore.Set();
				}
			}

			private void Scan()
			{
				var minimum = TimeSpan.MaxValue;

				foreach(var entry in _cache)
				{
					if(entry.Value.Expiry == null || entry.Value.Expiry == TimeSpan.Zero)
						continue;

					if(entry.Value.Expiry < minimum)
					{
						minimum = entry.Value.Expiry.Value;
						_keys.Add(entry.Key);
					}
					else if(entry.Value.Expiry == minimum)
					{
						_keys.Add(entry.Key);
					}
				}

				_expiration = long.MaxValue;

				if(minimum < TimeSpan.MaxValue)
				{
					_cancellation = new CancellationTokenSource();
					_expiration = GetExpiration(minimum);
					Task.Delay(minimum, _cancellation.Token).ContinueWith(DoWork);
				}
			}

			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private static long GetExpiration(TimeSpan duration)
			{
				return (long)(Environment.TickCount64 + duration.TotalMilliseconds) / 1000;
			}
		}
		#endregion
	}
}
