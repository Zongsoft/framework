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
using System.Collections.Generic;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions
{
	public class InsertStatement : StatementBase, IMutateStatement
	{
		#region 构造函数
		public InsertStatement(IDataEntity entity, SchemaMember schema, ParameterExpressionCollection parameters = null) : base(entity, null, parameters)
		{
			this.Schema = schema;
			this.Fields = new List<FieldIdentifier>();
			this.Values = new List<IExpression>();
			this.Options = new InsertStatementOptions();
		}
		#endregion

		#region 公共属性
		/// <summary>获取插入语句对应的序号字段值（如果有的话）的查询语句。</summary>
		public SelectStatement Sequence { get; set; }

		/// <summary>获取或设置插入语句对应的模式成员。</summary>
		public SchemaMember Schema { get; set; }

		/// <summary>获取新增或更新字段集合。</summary>
		public IList<FieldIdentifier> Fields { get; }

		/// <summary>获取新增或更新字段值集合。</summary>
		public IList<IExpression> Values { get; }

		/// <summary>获取或设置输出子句。</summary>
		public ReturningClause Returning { get; set; }

		/// <summary>获取插入语句的选项。</summary>
		public InsertStatementOptions Options { get; }
		#endregion
	}

	public class InsertStatementOptions
	{
		#region 公共属性
		/// <summary>获取或设置一个值，指示是否忽略插入数据导致的数据库约束（主键、唯一索引、外键约束等）冲突。</summary>
		public bool ConstraintIgnored { get; set; }
		#endregion

		#region 公共方法
		public void Apply(IDataInsertOptions options)
		{
			if(options == null)
				return;

			this.ConstraintIgnored = options.ConstraintIgnored;
		}
		#endregion
	}
}
