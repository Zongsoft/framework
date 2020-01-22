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
using System.Threading.Tasks;

namespace Zongsoft.Runtime.Caching
{
    public abstract class CacheBase : ICache
    {
		#region 事件声明
		public event EventHandler<CacheChangedEventArgs> Changed;
		#endregion

		#region 构造函数
		protected CacheBase(string name)
        {
            if(string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            this.Name = name;
        }
		#endregion

		#region 公共属性
		public string Name
        {
            get;
        }

        public long Count
        {
            get => this.GetCount();
        }
		#endregion

		#region 常用方法
		public abstract long GetCount();

        public virtual Task<long> GetCountAsync()
        {
            return Task.FromResult(this.GetCount());
        }

        public abstract bool Exists(string key);

        public virtual Task<bool> ExistsAsync(string key)
        {
            return Task.FromResult(this.Exists(key));
        }
		#endregion

		#region 期限方法
		public abstract TimeSpan? GetExpiry(string key);

        public virtual Task<TimeSpan?> GetExpiryAsync(string key)
        {
            return Task.FromResult(this.GetExpiry(key));
        }

        public abstract void SetExpiry(string key, TimeSpan duration);

        public virtual Task SetExpiryAsync(string key, TimeSpan duration)
        {
            return Task.Run(() => this.SetExpiry(key, duration));
        }
		#endregion

		#region 删除方法
		public abstract void Clear();

        public virtual Task ClearAsync()
        {
            return Task.Run(() => this.Clear());
        }

        public abstract bool Remove(string key);

        public virtual Task<bool> RemoveAsync(string key)
        {
            return Task.FromResult(this.Remove(key));
        }

        public abstract bool Rename(string oldKey, string newKey);

        public virtual Task<bool> RenameAsync(string oldKey, string newKey)
        {
            return Task.FromResult(this.Rename(oldKey, newKey));
        }
		#endregion

		#region 读取方法
		public abstract object GetValue(string key);

        public virtual T GetValue<T>(string key)
        {
            return Common.Convert.ConvertValue<T>(this.GetValue(key));
        }

        public Task<object> GetValueAsync(string key)
        {
            return Task.FromResult(this.GetValue(key));
        }

        public virtual Task<T> GetValueAsync<T>(string key)
        {
            return Task.FromResult(this.GetValue<T>(key));
        }
		#endregion

		#region 写入方法
		public void SetValue(string key, object value, ICacheDependency dependency = null)
        {
            this.SetValue(key, value, TimeSpan.Zero, dependency);
        }

        public abstract void SetValue(string key, object value, TimeSpan duration, ICacheDependency dependency = null);

        public virtual void SetValue(string key, object value, DateTime expires, ICacheDependency dependency = null)
        {
            var duration = expires.ToUniversalTime() - DateTime.UtcNow;

            if(duration > TimeSpan.Zero)
                this.SetValue(key, value, duration, dependency);
            else
                this.SetValue(key, value, TimeSpan.Zero, dependency);
        }

        public Task SetValueAsync(string key, object value, ICacheDependency dependency = null)
        {
            return this.SetValueAsync(key, value, TimeSpan.Zero, dependency);
        }

        public virtual Task SetValueAsync(string key, object value, TimeSpan duration, ICacheDependency dependency = null)
        {
            return Task.Run(() => this.SetValue(key, value, duration, dependency));
        }

        public virtual Task SetValueAsync(string key, object value, DateTime expires, ICacheDependency dependency = null)
        {
            return Task.Run(() => this.SetValue(key, value, expires, dependency));
        }

        public bool TrySetValue(string key, object value, ICacheDependency dependency = null)
        {
            return this.TrySetValue(key, value, TimeSpan.Zero, dependency);
        }

        public abstract bool TrySetValue(string key, object value, TimeSpan duration, ICacheDependency dependency = null);

        public virtual bool TrySetValue(string key, object value, DateTime expires, ICacheDependency dependency = null)
        {
            var duration = expires.ToUniversalTime() - DateTime.UtcNow;

            return (duration > TimeSpan.Zero) ?
                this.TrySetValue(key, value, duration, dependency) :
                this.TrySetValue(key, value, TimeSpan.Zero, dependency);
        }

        public Task<bool> TrySetValueAsync(string key, object value, ICacheDependency dependency = null)
        {
            return this.TrySetValueAsync(key, value, TimeSpan.Zero, dependency);
        }

        public virtual Task<bool> TrySetValueAsync(string key, object value, TimeSpan duration, ICacheDependency dependency = null)
        {
            return Task.FromResult(this.TrySetValue(key, value, duration, dependency));
        }

        public virtual Task<bool> TrySetValueAsync(string key, object value, DateTime expires, ICacheDependency dependency = null)
        {
            return Task.FromResult(this.TrySetValue(key, value, expires, dependency));
        }
        #endregion

        #region 激发事件
        protected void OnChanged(CacheChangedEventArgs args)
        {
            this.Changed?.Invoke(this, args);
        }
		#endregion
	}
}
