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
	/// 表示数据实体的元数据接口。
	/// </summary>
	public interface IDataEntity : IEquatable<IDataEntity>
	{
		#region 属性声明
		/// <summary>获取元数据所属的提供程序。</summary>
		IDataMetadataProvider Metadata { get; }

		/// <summary>获取所属命名空间。</summary>
		string Namespace { get; }

		/// <summary>获取数据实体的名称。</summary>
		string Name { get; }

		/// <summary>获取数据实体映射的别名（表名）。</summary>
		string Alias { get; }

		/// <summary>获取数据实体继承的父实体名。</summary>
		string BaseName { get; }

		/// <summary>获取数据实体支持的驱动。</summary>
		string Driver { get; }

		/// <summary>获取数据实体的主键属性数组。</summary>
		IDataEntitySimplexProperty[] Key { get; }

		/// <summary>获取一个值，指示是否为不可变实体，默认为否(False)。</summary>
		/// <remarks>不可变实体只支持新增和删除操作。</remarks>
		bool Immutable { get; }

		/// <summary>获取一个值，指示该实体定义中是否含有序号属性。</summary>
		bool HasSequences { get; }

		/// <summary>获取数据实体的属性元数据集合。</summary>
		IDataEntityPropertyCollection Properties { get; }
		#endregion

		#region 方法定义
		IDataEntityPropertySequence[] GetSequences();
		#endregion
	}
}
