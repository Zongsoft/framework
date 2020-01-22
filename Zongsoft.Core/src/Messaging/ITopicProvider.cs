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
using System.Collections.Generic;

namespace Zongsoft.Messaging
{
	/// <summary>
	/// 表示主题提供程序的接口。
	/// </summary>
	public interface ITopicProvider
	{
		/// <summary>
		/// 获取指定名称的主题服务接口。
		/// </summary>
		/// <param name="name">指定要获取的主题名称。</param>
		/// <returns>返回指定名称的主题对象，如果指定名称的主题不存在则返回空(null)。 </returns>
		ITopic Get(string name);

		/// <summary>
		/// 创建一个指定名称的主题服务。
		/// </summary>
		/// <param name="name">指定要创建的主题名称，必须确保相同主题提供程序中的所有主题名不同名。</param>
		/// <param name="state">指定的自定义状态对象。</param>
		/// <returns>返回创建成功的主题对象，如果失败则返回空(null)。</returns>
		/// <remarks>对实现的约定：如果指定名称的主题已经存在，应返回空(null)而不是抛出异常。</remarks>
		ITopic Register(string name, object state = null);

		/// <summary>
		/// 删除指定名称的主题服务。
		/// </summary>
		/// <param name="name">指定要删除的主题名称。</param>
		/// <returns>返回一个值，指示删除操作是否成功。</returns>
		/// <remarks>对实现的约定：如果删除操作失败，应返回假(False)，并尽量避免抛出异常。</remarks>
		bool Unregister(string name);
	}
}
