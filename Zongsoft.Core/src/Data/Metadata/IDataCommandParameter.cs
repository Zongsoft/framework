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
using System.Data;

namespace Zongsoft.Data.Metadata
{
	/// <summary>
	/// 表示命令参数的元数据类。
	/// </summary>
	public interface IDataCommandParameter
	{
		/// <summary>获取命令参数的名称。</summary>
		string Name { get; }

		/// <summary>获取命令参数的别名。</summary>
		string Alias { get; }

		/// <summary>获取命令参数的类型。</summary>
		DbType Type { get; }

		/// <summary>获取命令参数的最大长度。</summary>
		int Length { get; }

		/// <summary>获取或设置命令参数的值。</summary>
		object Value { get; set; }

		/// <summary>获取命令参数的传递方向。</summary>
		ParameterDirection Direction { get; }
	}
}
