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

namespace Zongsoft.Data.Metadata
{
	/// <summary>
	/// 表示数据实体单值属性的元数据类。
	/// </summary>
	public interface IDataEntitySimplexProperty : IDataEntityProperty
	{
		/// <summary>获取数据实体属性的别名（字段名）。</summary>
		string Alias { get; }

		/// <summary>获取数据实体属性的数据类型。</summary>
		System.Data.DbType Type { get; }

		/// <summary>获取或设置文本或数组属性的最大长度，单位：字节。</summary>
		int Length { get; set; }

		/// <summary>获取或设置数值属性的精度。</summary>
		byte Precision { get; set; }

		/// <summary>获取或设置数值属性的小数点位数。</summary>
		byte Scale { get; set; }

		/// <summary>获取或设置默认值。</summary>
		object DefaultValue { get; set; }

		/// <summary>获取或设置属性是否允许为空。</summary>
		bool Nullable { get; set; }

		/// <summary>获取或设置属性是否可以参与排序。</summary>
		bool Sortable { get; set; }

		/// <summary>获取数据序号器元数据。</summary>
		IDataEntityPropertySequence Sequence { get; }
	}
}
