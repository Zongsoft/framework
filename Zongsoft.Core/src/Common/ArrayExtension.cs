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

namespace Zongsoft.Common
{
	public static class ArrayExtension
	{
		#region 私有变量
		private static readonly Dictionary<Type, Array> _empties = new Dictionary<Type, Array>();
		#endregion

		/// <summary>
		/// 获取一个值，指示数组是否为空(null)或长度为零。
		/// </summary>
		/// <param name="array">指定要判断的数组。</param>
		/// <returns>如果数组为空或空则返回真(True)，否则返回假(False)。</returns>
		public static bool IsEmpty(this Array array) => array == null || array.Length == 0;

		/// <summary>
		/// 获取指定类型的空数组（即元素数量为零的数组）。
		/// </summary>
		/// <param name="type">指定的数组元素类型。</param>
		/// <returns>返回指定元素类型的空数组。</returns>
		public static Array Empty(Type type)
		{
			if(_empties.TryGetValue(type, out var array))
				return array;

			var element = Array.CreateInstance(type, 0);
			return _empties.TryAdd(type, element) ? element : _empties[type];
		}
	}
}
