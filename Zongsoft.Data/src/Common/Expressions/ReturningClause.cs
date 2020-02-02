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
using System.Collections.Generic;

namespace Zongsoft.Data.Common.Expressions
{
	public class ReturningClause
	{
		#region 构造函数
		public ReturningClause(TableDefinition table, params ReturningMember[] members)
		{
			this.Table = table ?? throw new ArgumentNullException(nameof(table));

			if(members == null || members.Length == 0)
				this.Members = new List<ReturningMember>();
			else
				this.Members = new List<ReturningMember>(members);
		}

		public ReturningClause(params ReturningMember[] members)
		{
			if(members == null || members.Length == 0)
				this.Members = new List<ReturningMember>();
			else
				this.Members = new List<ReturningMember>(members);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取 Returning/Output 子句的输出(INTO)表定义，如果为空则表示无输出表。
		/// </summary>
		public TableDefinition Table
		{
			get;
		}

		/// <summary>
		/// 获取 Returning/Output 子句的成员字段集，如果为空集则表示全部字段。
		/// </summary>
		public ICollection<ReturningMember> Members
		{
			get;
		}
		#endregion

		#region 公共方法
		public ReturningMember Append(FieldIdentifier field, ReturningMode mode)
		{
			var member = new ReturningMember(field, mode);
			this.Members.Add(member);
			return member;
		}
		#endregion

		#region 嵌套结构
		public enum ReturningMode
		{
			Deleted,
			Inserted,
		}

		public struct ReturningMember
		{
			public FieldIdentifier Field;
			public ReturningMode Mode;

			public ReturningMember(FieldIdentifier field, ReturningMode mode)
			{
				this.Field = field;
				this.Mode = mode;
			}
		}
		#endregion
	}
}
