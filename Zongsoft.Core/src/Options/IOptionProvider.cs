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

namespace Zongsoft.Options
{
	/// <summary>
	/// 提供选项数据的获取与保存。
	/// </summary>
	public interface IOptionProvider
	{
		/// <summary>
		/// 根据指定的选项路径获取对应的选项数据。
		/// </summary>
		/// <param name="text">要获取的选项路径表达式文本，该表达式的结构请参考<seealso cref="Zongsoft.Collections.HierarchicalExpression"/>。</param>
		/// <returns>获取到的选项数据对象。</returns>
		object GetOptionValue(string text);

		/// <summary>
		/// 将指定的选项数据保存到指定路径的存储容器中。
		/// </summary>
		/// <param name="text">待保存的选项路径表达式文本，该表达式的结构请参考<seealso cref="Zongsoft.Collections.HierarchicalExpression"/>。</param>
		/// <param name="value">待保存的选项对象。</param>
		void SetOptionValue(string text, object value);
	}
}
