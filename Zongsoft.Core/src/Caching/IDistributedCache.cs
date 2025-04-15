﻿/*
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
using System.Collections.Generic;

namespace Zongsoft.Caching;

/// <summary>
/// 表示分布式缓存容器的接口。
/// </summary>
public interface IDistributedCache
{
	#region 事件定义
	/// <summary>表示缓存发生改变的事件。</summary>
	event EventHandler<CacheChangedEventArgs> Changed;
	#endregion

	#region 属性定义
	/// <summary>获取当前缓存容器的名字。</summary>
	string Name { get; }
	#endregion

	#region 常用方法
	/// <summary>获取缓存容器中的记录总数。</summary>
	/// <returns>返回的记录总数。</returns>
	long GetCount();

	/// <summary>获取缓存容器中的记录总数。</summary>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	ValueTask<long> GetCountAsync(CancellationToken cancellation = default);

	/// <summary>检测指定键的缓存项是否存在。</summary>
	/// <param name="key">指定要检测的键。</param>
	/// <returns>如果存在则返回真(True)，否则返回假(False)。</returns>
	bool Exists(string key);

	/// <summary>检测指定键的缓存项是否存在。</summary>
	/// <param name="key">指定要检测的键。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	ValueTask<bool> ExistsAsync(string key, CancellationToken cancellation = default);

	/// <summary>查找指定模式的缓存键集合。</summary>
	/// <param name="pattern">指定的查找模式。</param>
	/// <returns>返回匹配查找模式的缓存键集合。</returns>
	IEnumerable<string> Find(string pattern);

	/// <summary>查找指定模式的缓存键集合。</summary>
	/// <param name="pattern">指定的查找模式。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回匹配查找模式的缓存键集合。</returns>
	IAsyncEnumerable<string> FindAsync(string pattern, CancellationToken cancellation = default);
	#endregion

	#region 期限方法
	/// <summary>获取指定键的缓存项的剩下的生存时长。</summary>
	/// <param name="key">指定要设置的键。</param>
	/// <returns>返回指定缓存项的生成时长，如果为空则表示该缓存项为永久缓存项。</returns>
	TimeSpan? GetExpiry(string key);

	/// <summary>获取指定键的缓存项的剩下的生存时长。</summary>
	/// <param name="key">指定要设置的键。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	ValueTask<TimeSpan?> GetExpiryAsync(string key, CancellationToken cancellation = default);

	/// <summary>设置指定键的缓存项的生存时长。</summary>
	/// <param name="key">指定要设置的键。</param>
	/// <param name="expiry">指定要设置的生存时长，如果为零则将该缓存项设置成永不过期。</param>
	/// <returns>如果设置成功则返回真(True)，否则返回假(False)。</returns>
	bool SetExpiry(string key, TimeSpan expiry);

	/// <summary>设置指定键的缓存项的生存时长。</summary>
	/// <param name="key">指定要设置的键。</param>
	/// <param name="expiry">指定要设置的生存时长，如果为零则将该缓存项设置成永不过期。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	ValueTask<bool> SetExpiryAsync(string key, TimeSpan expiry, CancellationToken cancellation = default);
	#endregion

	#region 删除方法
	/// <summary>清空缓存中的所有数据。</summary>
	void Clear();

	/// <summary>清空缓存中的所有数据。</summary>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	ValueTask ClearAsync(CancellationToken cancellation = default);

	/// <summary>从缓存中删除指定键的缓存项。</summary>
	/// <param name="key">指定要删除的键。</param>
	/// <returns>如果指定的键存在则返回真(True)，否则返回假(False)。</returns>
	bool Remove(string key);

	/// <summary>从缓存中删除并获取指定键的缓存项。</summary>
	/// <param name="key">指定要删除的键。</param>
	/// <param name="value">如果删除成功则返回该删除项的值。</param>
	/// <returns>如果指定的键存在则返回真(True)，否则返回假(False)。</returns>
	bool Remove(string key, out object value);

	/// <summary>从缓存中删除多个缓存项。</summary>
	/// <param name="keys">指定要删除的键名集。</param>
	/// <returns>返回删除成功的数量。</returns>
	int Remove(IEnumerable<string> keys);

	/// <summary>从缓存中删除指定键的缓存项。</summary>
	/// <param name="key">指定要删除的键。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	ValueTask<bool> RemoveAsync(string key, CancellationToken cancellation = default);

	/// <summary>从缓存中删除多个缓存项。</summary>
	/// <param name="keys">指定要删除的键名集。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	ValueTask<int> RemoveAsync(IEnumerable<string> keys, CancellationToken cancellation = default);

	/// <summary>修改指定键的缓存项的键名。</summary>
	/// <param name="oldKey">指定要更名的键，即待更名的现有键。</param>
	/// <param name="newKey">更改后的新键。</param>
	/// <returns>如果设置成功则返回真(True)，否则返回假(False)。</returns>
	bool Rename(string oldKey, string newKey);

	/// <summary>修改指定键的缓存项的键名。</summary>
	/// <param name="oldKey">指定要更名的键，即待更名的现有键。</param>
	/// <param name="newKey">更改后的新键。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	ValueTask<bool> RenameAsync(string oldKey, string newKey, CancellationToken cancellation = default);
	#endregion

	#region 读取方法
	/// <summary>从缓存中获取指定键的缓存值。</summary>
	/// <param name="key">指定要获取的键名。</param>
	/// <returns>返回指定键名对应的缓存项值，如果指定的键名不存在则返回空(null)。</returns>
	object GetValue(string key);

	/// <summary>从缓存中获取指定键的缓存值。</summary>
	/// <typeparam name="T">指定的缓存项类型。</typeparam>
	/// <param name="key">指定要获取的键名。</param>
	/// <returns>返回指定键名对应的缓存项值，如果指定的键名不存在则返回指定类型的默认值。</returns>
	T GetValue<T>(string key);

	/// <summary>从缓存中获取指定键的缓存值。</summary>
	/// <param name="key">指定要获取的键名。</param>
	/// <param name="expiry">输出参数，表示指定缓存项的剩余有效期。</param>
	/// <returns>返回指定键名对应的缓存项值，如果指定的键名不存在则返回空(null)。</returns>
	object GetValue(string key, out TimeSpan? expiry);

	/// <summary>从缓存中获取指定键的缓存值。</summary>
	/// <typeparam name="T">指定的缓存项类型。</typeparam>
	/// <param name="key">指定要获取的键名。</param>
	/// <param name="expiry">输出参数，表示指定缓存项的剩余有效期。</param>
	/// <returns>返回指定键名对应的缓存项值，如果指定的键名不存在则返回空(null)。</returns>
	T GetValue<T>(string key, out TimeSpan? expiry);

	/// <summary>从缓存中获取指定键的缓存值。</summary>
	/// <param name="key">指定要获取的键。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	ValueTask<object> GetValueAsync(string key, CancellationToken cancellation = default);

	/// <summary>从缓存中获取指定键的缓存值。</summary>
	/// <typeparam name="T">指定的缓存项类型。</typeparam>
	/// <param name="key">指定要获取的键。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	ValueTask<T> GetValueAsync<T>(string key, CancellationToken cancellation = default);

	/// <summary>从缓存中获取指定键的缓存值。</summary>
	/// <param name="key">指定要获取的键。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	ValueTask<(object Value, TimeSpan? Expiry)> GetValueExpiryAsync(string key, CancellationToken cancellation = default);

	/// <summary>从缓存中获取指定键的缓存值。</summary>
	/// <typeparam name="T">指定的缓存项类型。</typeparam>
	/// <param name="key">指定要获取的键。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	ValueTask<(T Value, TimeSpan? Expiry)> GetValueExpiryAsync<T>(string key, CancellationToken cancellation = default);

	/// <summary>尝试从缓存中获取指定键的缓存值。</summary>
	/// <typeparam name="T">指定的缓存项类型。</typeparam>
	/// <param name="key">指定要获取的键名。</param>
	/// <param name="value">输出参数，表示获取到的缓存项内容。</param>
	/// <returns>如果获取成功则返回真(True)，否则返回假(False)。</returns>
	bool TryGetValue<T>(string key, out T value);

	/// <summary>尝试从缓存中获取指定键的缓存值。</summary>
	/// <typeparam name="T">指定的缓存项类型。</typeparam>
	/// <param name="key">指定要获取的键名。</param>
	/// <param name="value">输出参数，表示获取到的缓存项内容。</param>
	/// <param name="expiry">输出参数，表示获取到的缓存项有效期。</param>
	/// <returns>如果获取成功则返回真(True)，否则返回假(False)。</returns>
	bool TryGetValue<T>(string key, out T value, out TimeSpan? expiry);

	/// <summary>尝试从缓存中获取指定键的缓存值。</summary>
	/// <param name="key">指定要获取的键名。</param>
	/// <param name="cancellation">异步操作取消令牌。</param>
	/// <returns>返回的异步操作任务结果。</returns>
	ValueTask<(bool result, object value)> TryGetValueAsync(string key, CancellationToken cancellation = default);

	/// <summary>尝试从缓存中获取指定键的缓存值。</summary>
	/// <typeparam name="T">指定的缓存项类型。</typeparam>
	/// <param name="key">指定要获取的键名。</param>
	/// <param name="cancellation">异步操作取消令牌。</param>
	/// <returns>返回的异步操作任务结果。</returns>
	ValueTask<(bool result, T value)> TryGetValueAsync<T>(string key, CancellationToken cancellation = default);
	#endregion

	#region 设置方法
	/// <summary>设置指定的值保存到缓存中。</summary>
	/// <param name="key">指定要保存的键。</param>
	/// <param name="value">指定要保存的值。</param>
	/// <param name="requisite">指定设置的必须条件，默认为<see cref="CacheRequisite.Always"/>。</param>
	/// <returns>如果设置成功则返回真(True)，否则返回假(False)。</returns>
	bool SetValue(string key, object value, CacheRequisite requisite = CacheRequisite.Always);

	/// <summary>设置指定的值保存到缓存中。</summary>
	/// <param name="key">指定要保存的键。</param>
	/// <param name="value">指定要保存的值。</param>
	/// <param name="requisite">指定设置的必须条件，默认为<see cref="CacheRequisite.Always"/>。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	ValueTask<bool> SetValueAsync(string key, object value, CacheRequisite requisite = CacheRequisite.Always, CancellationToken cancellation = default);

	/// <summary>设置指定的值保存到缓存中。</summary>
	/// <param name="key">指定要保存的键。</param>
	/// <param name="value">指定要保存的值。</param>
	/// <param name="expiry">指定缓存项的生存时长，如果为零则表示永不过期。</param>
	/// <param name="requisite">指定设置的必须条件，默认为<see cref="CacheRequisite.Always"/>。</param>
	/// <returns>如果设置成功则返回真(True)，否则返回假(False)。</returns>
	bool SetValue(string key, object value, TimeSpan expiry, CacheRequisite requisite = CacheRequisite.Always);

	/// <summary>设置指定的值保存到缓存中。</summary>
	/// <param name="key">指定要保存的键。</param>
	/// <param name="value">指定要保存的值。</param>
	/// <param name="expiry">指定缓存项的生存时长，如果为零则表示永不过期。</param>
	/// <param name="requisite">指定设置的必须条件，默认为<see cref="CacheRequisite.Always"/>。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	ValueTask<bool> SetValueAsync(string key, object value, TimeSpan expiry, CacheRequisite requisite = CacheRequisite.Always, CancellationToken cancellation = default);
	#endregion
}
