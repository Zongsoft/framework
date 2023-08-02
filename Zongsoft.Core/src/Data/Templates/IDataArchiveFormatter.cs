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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Data.Templates
{
	/// <summary>表示数据文件格式化器的接口。</summary>
	/// <remarks>数据文件生成器会对每条数据生成目标记录时候会调用该接口的<see cref="Format(object, ModelPropertyDescriptor)"/>格式化方法，并将其格式化的结果作为最终结果写入到目标记录的相应单元内。</remarks>
	public interface IDataArchiveFormatter
	{
		/// <summary>格式化指定的数据属性值。</summary>
		/// <param name="target">指定的待格式化的目标对象。</param>
		/// <param name="property">指定的待格式化的目标属性。</param>
		/// <returns>返回格式化后的属性值。</returns>
		object Format(object target, ModelPropertyDescriptor property);
	}
}