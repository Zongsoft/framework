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

/// <summary>表示分布式缓存事件的基类。</summary>
public class DistributedCacheEventArgs : EventArgs
{
	#region 构造函数
	protected DistributedCacheEventArgs(string key) => this.Key = key;
	#endregion

	#region 公共属性
	/// <summary>获取缓存项的键。</summary>
	public string Key { get; }

	/// <summary>获取一个值，指示当前事件是否表示缓存过期。</summary>
	public bool IsExpired => this is ExpiredEventArgs;
	/// <summary>获取一个值，指示当前事件是否表示缓存被移除。</summary>
	public bool IsRemoved => this is RemovedEventArgs;
	/// <summary>获取一个值，指示当前事件是否表示缓存被更新。</summary>
	public bool IsUpdated => this is UpdatedEventArgs;
	#endregion

	#region 重写方法
	public override string ToString() => this.Key;
	#endregion

	#region 静态方法
	public static DistributedCacheEventArgs Expired(string key) => new ExpiredEventArgs(key);
	public static DistributedCacheEventArgs Removed(string key) => new RemovedEventArgs(key);
	public static DistributedCacheEventArgs Updated(string key) => new UpdatedEventArgs(key);
	#endregion

	#region 嵌套子类
	private sealed class ExpiredEventArgs(string key) : DistributedCacheEventArgs(key)
	{
		public override string ToString() => $"Expired: {this.Key}";
	}

	private sealed class RemovedEventArgs(string key) : DistributedCacheEventArgs(key)
	{
		public override string ToString() => $"Removed: {this.Key}";
	}

	private sealed class UpdatedEventArgs(string key) : DistributedCacheEventArgs(key)
	{
		public override string ToString() => $"Updated: {this.Key}";
	}
	#endregion
}