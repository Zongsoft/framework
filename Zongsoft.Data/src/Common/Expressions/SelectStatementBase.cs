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
	/// <summary>
	/// 表示查询语句的基类。
	/// </summary>
	public abstract class SelectStatementBase : Statement, ISelectStatementBase, ISource
	{
		#region 构造函数
		protected SelectStatementBase(string alias = null)
		{
			this.Alias = alias ?? string.Empty;
			this.Select = new SelectClause();
		}

		protected SelectStatementBase(ISource source, string alias = null) : base(source)
		{
			if(source == null)
				throw new ArgumentNullException(nameof(source));

			this.Alias = alias ?? source.Alias;
			this.Select = new SelectClause();
		}

		protected SelectStatementBase(IDataEntity entity, string alias = null) : base(entity, "T")
		{
			this.Alias = alias ?? string.Empty;
			this.Select = new SelectClause();
		}
		#endregion

		#region 公共属性
		/// <summary>获取查询语句的别名。</summary>
		public string Alias { get; }

		/// <summary>获取查询语句的选择子句。</summary>
		public SelectClause Select { get; }
		#endregion

		#region 公共方法
		public FieldIdentifier CreateField(string name, string alias = null)
		{
			return new FieldIdentifier(this, name, alias);
		}

		public FieldIdentifier CreateField(IDataEntityProperty property)
		{
			if(property == null)
				throw new ArgumentNullException(nameof(property));

			return new FieldIdentifier(this, property.GetFieldName(out var alias), alias)
			{
				Token = new DataEntityPropertyToken(property)
			};
		}
		#endregion
	}
}
