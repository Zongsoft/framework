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

namespace Zongsoft.Security.Membership
{
	/// <summary>
	/// 表示命名空间映射器的接口。
	/// </summary>
	public interface INamespaceMapper
	{
		T Map<T>(string @namespace)
		{
			if(this.TryMap<T>(@namespace, out var result))
				return result;

			return default;
		}

		/// <summary>
		/// 尝试获取指定命名空间的映射键值。
		/// </summary>
		/// <typeparam name="T">命名空间映射键的类型参数。</typeparam>
		/// <param name="namespace">指定的命名空间标识。</param>
		/// <param name="result">输出参数，表示对应命名空间映射的键值。</param>
		/// <returns>如果获取成功则返回真(True)，否则返回假(False)。</returns>
		bool TryMap<T>(string @namespace, out T result);
	}
}
