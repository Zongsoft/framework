﻿/*
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

namespace Zongsoft.Runtime.Caching
{
	/// <summary>
	/// 表示缓存容器的接口。
	/// </summary>
	public interface ICache
	{
		#region 事件定义
		/// <summary>
		/// 表示缓存发生改变的事件。
		/// </summary>
		event EventHandler<CacheChangedEventArgs> Changed;
		#endregion

		#region 属性定义
		/// <summary>
		/// 获取当前缓存容器的名字。
		/// </summary>
		string Name
		{
			get;
		}
		#endregion

		#region 常用方法
		/// <summary>
		/// 获取缓存容器中的记录总数。
		/// </summary>
		/// <returns>返回的记录总数。</returns>
		long GetCount();

		/// <summary>
		/// 获取缓存容器中的记录总数。
		/// </summary>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回表示异步操作的任务对象。</returns>
		Task<long> GetCountAsync(CancellationToken cancellation = default);

		/// <summary>
		/// 检测指定键的缓存项是否存在。
		/// </summary>
		/// <param name="key">指定要检测的键。</param>
		/// <returns>如果存在则返回真(True)，否则返回假(False)。</returns>
		bool Exists(string key);

		/// <summary>
		/// 检测指定键的缓存项是否存在。
		/// </summary>
		/// <param name="key">指定要检测的键。</param>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回表示异步操作的任务对象。</returns>
		Task<bool> ExistsAsync(string key, CancellationToken cancellation = default);
		#endregion

		#region 期限方法
		/// <summary>
		/// 获取指定键的缓存项的剩下的生存时长。
		/// </summary>
		/// <param name="key">指定要设置的键。</param>
		/// <returns>返回指定缓存项的生成时长，如果为空则表示该缓存项为永久缓存项。</returns>
		TimeSpan? GetExpiry(string key);

		/// <summary>
		/// 获取指定键的缓存项的剩下的生存时长。
		/// </summary>
		/// <param name="key">指定要设置的键。</param>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回表示异步操作的任务对象。</returns>
		Task<TimeSpan?> GetExpiryAsync(string key, CancellationToken cancellation = default);

		/// <summary>
		/// 设置指定键的缓存项的生存时长。
		/// </summary>
		/// <param name="key">指定要设置的键。</param>
		/// <param name="duration">指定要设置的生存时长，如果为零则将该缓存项设置成永不过期。</param>
		void SetExpiry(string key, TimeSpan duration);

		/// <summary>
		/// 设置指定键的缓存项的生存时长。
		/// </summary>
		/// <param name="key">指定要设置的键。</param>
		/// <param name="duration">指定要设置的生存时长，如果为零则将该缓存项设置成永不过期。</param>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回表示异步操作的任务对象。</returns>
		Task SetExpiryAsync(string key, TimeSpan duration, CancellationToken cancellation = default);
		#endregion

		#region 删除方法
		/// <summary>
		/// 清空缓存字典中的所有数据。
		/// </summary>
		void Clear();

		/// <summary>
		/// 清空缓存字典中的所有数据。
		/// </summary>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回表示异步操作的任务对象。</returns>
		Task ClearAsync(CancellationToken cancellation = default);

		/// <summary>
		/// 从缓存字典中删除指定键的缓存项。
		/// </summary>
		/// <param name="key">指定要删除的键。</param>
		/// <returns>如果指定的键存在则返回真(True)，否则返回假(False)。</returns>
		bool Remove(string key);

		/// <summary>
		/// 从缓存字典中删除指定键的缓存项。
		/// </summary>
		/// <param name="key">指定要删除的键。</param>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回表示异步操作的任务对象。</returns>
		Task<bool> RemoveAsync(string key, CancellationToken cancellation = default);

		/// <summary>
		/// 修改指定键的缓存项的键名。
		/// </summary>
		/// <param name="oldKey">指定要更名的键，即待更名的现有键。</param>
		/// <param name="newKey">更改后的新键。</param>
		/// <returns>如果设置成功则返回真(True)，否则返回假(False)。</returns>
		bool Rename(string oldKey, string newKey);

		/// <summary>
		/// 修改指定键的缓存项的键名。
		/// </summary>
		/// <param name="oldKey">指定要更名的键，即待更名的现有键。</param>
		/// <param name="newKey">更改后的新键。</param>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回表示异步操作的任务对象。</returns>
		Task<bool> RenameAsync(string oldKey, string newKey, CancellationToken cancellation = default);
		#endregion

		#region 读取方法
		/// <summary>
		/// 从缓存字典中获取指定键的缓存值。
		/// </summary>
		/// <param name="key">指定要获取的键名。</param>
		/// <returns>返回指定键名对应的缓存项值，如果指定的键名不存在则返回空(null)。</returns>
		object GetValue(string key);

		/// <summary>
		/// 从缓存字典中获取指定键的缓存值。
		/// </summary>
		/// <param name="key">指定要获取的键。</param>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回表示异步操作的任务对象。</returns>
		Task<object> GetValueAsync(string key, CancellationToken cancellation = default);
		#endregion

		#region 设置方法
		/// <summary>
		/// 设置指定的值保存到缓存字典中。
		/// </summary>
		/// <param name="key">指定要保存的键。</param>
		/// <param name="value">指定要保存的值。</param>
		/// <param name="requires">指定设置的必须条件，默认为<see cref="CacheRequires.None"/>。</param>
		void SetValue(string key, object value, CacheRequires requires = CacheRequires.None);

		/// <summary>
		/// 设置指定的值保存到缓存字典中。
		/// </summary>
		/// <param name="key">指定要保存的键。</param>
		/// <param name="value">指定要保存的值。</param>
		/// <param name="requires">指定设置的必须条件，默认为<see cref="CacheRequires.None"/>。</param>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回表示异步操作的任务对象。</returns>
		Task SetValueAsync(string key, object value, CacheRequires requires = CacheRequires.None, CancellationToken cancellation = default);

		/// <summary>
		/// 设置指定的值保存到缓存字典中。
		/// </summary>
		/// <param name="key">指定要保存的键。</param>
		/// <param name="value">指定要保存的值。</param>
		/// <param name="duration">指定缓存项的生存时长，如果为零则表示永不过期。</param>
		/// <param name="requires">指定设置的必须条件，默认为<see cref="CacheRequires.None"/>。</param>
		void SetValue(string key, object value, TimeSpan duration, CacheRequires requires = CacheRequires.None);

		/// <summary>
		/// 设置指定的值保存到缓存字典中。
		/// </summary>
		/// <param name="key">指定要保存的键。</param>
		/// <param name="value">指定要保存的值。</param>
		/// <param name="duration">指定缓存项的生存时长，如果为零则表示永不过期。</param>
		/// <param name="requires">指定设置的必须条件，默认为<see cref="CacheRequires.None"/>。</param>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回表示异步操作的任务对象。</returns>
		Task SetValueAsync(string key, object value, TimeSpan duration, CacheRequires requires = CacheRequires.None, CancellationToken cancellation = default);

		/// <summary>
		/// 设置指定的值保存到缓存字典中。
		/// </summary>
		/// <param name="key">指定要保存的键。</param>
		/// <param name="value">指定要保存的值。</param>
		/// <param name="expires">指定缓存项的过期时间，如果小于当前时间则表示永不过期。</param>
		/// <param name="requires">指定设置的必须条件，默认为<see cref="CacheRequires.None"/>。</param>
		void SetValue(string key, object value, DateTime expires, CacheRequires requires = CacheRequires.None);

		/// <summary>
		/// 设置指定的值保存到缓存字典中。
		/// </summary>
		/// <param name="key">指定要保存的键。</param>
		/// <param name="value">指定要保存的值。</param>
		/// <param name="expires">指定缓存项的过期时间，如果小于当前时间则表示永不过期。</param>
		/// <param name="requires">指定设置的必须条件，默认为<see cref="CacheRequires.None"/>。</param>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回表示异步操作的任务对象。</returns>
		Task SetValueAsync(string key, object value, DateTime expires, CacheRequires requires = CacheRequires.None, CancellationToken cancellation = default);
		#endregion
	}
}
