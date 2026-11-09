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

using Zongsoft.Data.Common;

namespace Zongsoft.Data.Features;

/// <summary>提供活动读取器之间的连接共享能力。</summary>
public interface IConnectionSharingFeature
{
	/// <summary>判断指定数据源是否允许多个活动读取器共享同一连接。</summary>
	/// <param name="source">指定的数据源。</param>
	/// <returns>允许共享时返回真，否则返回假。</returns>
	bool CanShare(IDataSource source);
}

/// <summary>提供默认不共享活动读取器连接的实现。</summary>
public class ConnectionSharingFeature : IConnectionSharingFeature
{
	/// <summary>获取默认不共享连接的特性实例。</summary>
	public static readonly ConnectionSharingFeature Instance = new();

	/// <summary>判断指定数据源是否允许多个活动读取器共享同一连接，默认返回假。</summary>
	/// <param name="source">指定的数据源。</param>
	/// <returns>默认返回假，派生类可重写此方法提供共享能力。</returns>
	public virtual bool CanShare(IDataSource source) => false;
}
