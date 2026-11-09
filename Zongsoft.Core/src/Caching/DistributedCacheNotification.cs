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
