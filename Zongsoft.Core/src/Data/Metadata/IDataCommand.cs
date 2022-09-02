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

namespace Zongsoft.Data.Metadata
{
	/// <summary>
	/// 表示数据命令的元数据类。
	/// </summary>
	public interface IDataCommand : IEquatable<IDataCommand>
	{
		/// <summary>获取元数据所属的提供程序。</summary>
		IDataMetadataProvider Metadata { get; }

		/// <summary>获取数据命令的名称。</summary>
		string Name { get; }

		/// <summary>获取或设置命令的类型。</summary>
		DataCommandType Type { get; set; }

		/// <summary>获取或设置数据命令的别名（函数或存储过程的名称）。</summary>
		string Alias { get; set; }

		/// <summary>获取或设置数据命令支持的驱动。</summary>
		string Driver { get; set; }

		/// <summary>获取或设置命令的变化性。</summary>
		CommandMutability Mutability { get; set; }

		/// <summary>获取数据命令的参数集合。</summary>
		Collections.INamedCollection<IDataCommandParameter> Parameters { get; }

		/// <summary>获取数据命令的脚本对象。</summary>
		IDataCommandScriptor Scriptor { get; }
	}
}
