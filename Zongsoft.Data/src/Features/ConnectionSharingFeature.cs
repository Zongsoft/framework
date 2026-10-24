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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
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
