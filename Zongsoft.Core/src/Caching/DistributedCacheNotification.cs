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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Caching;

/// <summary>表示分布式缓存通知。</summary>
public readonly struct DistributedCacheNotification
{
	#region 构造函数
	/// <summary>初始化分布式缓存通知。</summary>
	/// <param name="kind">单一的通知种类。</param>
	/// <param name="key">不含后端命名空间的逻辑缓存键。</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/>为<see cref="DistributedCacheNotificationKind.None"/>、组合标志或未定义标志。</exception>
	/// <exception cref="ArgumentNullException"><paramref name="key"/>为空。</exception>
	public DistributedCacheNotification(DistributedCacheNotificationKind kind, string key)
	{
		if(!IsSingle(kind))
			throw new ArgumentOutOfRangeException(nameof(kind));
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		this.Kind = kind;
		this.Key = key;

		static bool IsSingle(DistributedCacheNotificationKind kind) => IsValid(kind) && ((int)kind & ((int)kind - 1)) == 0;
	}
	#endregion

	#region 公共属性
	/// <summary>获取逻辑缓存键。</summary>
	public string Key { get; }
	/// <summary>获取通知种类。</summary>
	public DistributedCacheNotificationKind Kind { get; }
	/// <summary>获取一个值，指示当前通知是否为空。</summary>
	public bool IsEmpty => this.Kind == DistributedCacheNotificationKind.None || string.IsNullOrEmpty(this.Key);
	#endregion

	#region 重写方法
	public override string ToString() => this.IsEmpty ? string.Empty : $"[{this.Kind}] {this.Key}";
	#endregion

	#region 内部方法
	internal static bool IsValid(DistributedCacheNotificationKind kind) => kind != DistributedCacheNotificationKind.None && (kind & ~DistributedCacheNotificationKind.All) == 0;
	#endregion
}
