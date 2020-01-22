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
	/// 表示数据实体复合属性的元数据类。
	/// </summary>
	public interface IDataEntityComplexProperty : IDataEntityProperty
	{
		/// <summary>
		/// 获取关联的外部实体。
		/// </summary>
		IDataEntity Foreign
		{
			get;
		}

		/// <summary>
		/// 获取关联的外部层级属性，只有多级关联该属性才不为空(null)。
		/// </summary>
		IDataEntityProperty ForeignProperty
		{
			get;
		}

		/// <summary>
		/// 获取一个值，指示关联的重复性关系。
		/// </summary>
		DataAssociationMultiplicity Multiplicity
		{
			get;
		}

		/// <summary>
		/// 获取关联的外部角色，通常是关联的目标实体名，但是也支持多级关联（详情见备注说明）。
		/// </summary>
		/// <remarks>
		///		<para>多级关联是指关联的目标为指定实体中的导航属性，实体与导航属性之间以分号(:)区隔。</para>
		/// </remarks>
		string Role
		{
			get;
		}

		/// <summary>
		/// 获取关联的连接数组。
		/// </summary>
		DataAssociationLink[] Links
		{
			get;
		}

		/// <summary>
		/// 获取关联的约束数组。
		/// </summary>
		DataAssociationConstraint[] Constraints
		{
			get;
		}
	}
}
