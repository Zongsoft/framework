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

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions
{
	public class UpsertStatement : MutateStatement
	{
		#region 构造函数
		public UpsertStatement(IDataEntity entity, SchemaMember schema) : base(entity, schema, "TAR")
		{
			this.Fields = new List<FieldIdentifier>();
			this.Values = new List<IExpression>();
			this.Updation = new List<FieldValue>();
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取插入语句对应的序号字段值（如果有的话）的查询语句。
		/// </summary>
		public SelectStatement Sequence
		{
			get; set;
		}

		/// <summary>
		/// 获取新增或更新字段集合。
		/// </summary>
		public IList<FieldIdentifier> Fields
		{
			get;
		}

		/// <summary>
		/// 获取新增或更新字段值集合。
		/// </summary>
		public IList<IExpression> Values
		{
			get;
		}

		/// <summary>
		/// 获取更新字段/值的集合。
		/// </summary>
		public ICollection<FieldValue> Updation
		{
			get;
		}
		#endregion
	}
}
