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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions
{
	/// <summary>
	/// 表示查询语句的类。
	/// </summary>
	public class SelectStatement : SelectStatementBase
	{
		#region 构造函数
		public SelectStatement(string alias = null) : base(alias) { }
		public SelectStatement(ISource source, string alias = null) : base(source, alias) { }
		public SelectStatement(IDataEntity entity, string alias = null) : base(entity, alias) { }
		#endregion

		#region 公共属性
		/// <summary>获取或设置查询语句的输出表标识。</summary>
		/// <remarks>
		/// 	<para>在一对多关系（即含有从属查询语句）中，通常需要通过主查询语句的输出表标识来进行关联数据过滤。</para>
		/// </remarks>
		public IIdentifier Into { get; set; }

		/// <summary>获取或设置查询语句的分组子句。</summary>
		public GroupByClause GroupBy { get; set; }

		/// <summary>获取或设置查询语句的排序子句。</summary>
		public OrderByClause OrderBy { get; set; }

		/// <summary>获取或设置查询语句的分页信息。</summary>
		public Paging Paging { get; set; }
		#endregion
	}
}
