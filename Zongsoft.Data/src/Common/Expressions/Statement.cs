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

using Zongsoft.Collections;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions
{
	/// <summary>
	/// 表示带条件子句的语句基类。
	/// </summary>
	public class Statement : StatementBase, IStatement
	{
		#region 构造函数
		protected Statement() => this.From = new SourceCollection();
		protected Statement(ISource source)
		{
			this.Table = source as TableIdentifier;
			this.From = new SourceCollection();

			if(source != null)
				this.From.Add(source);
		}

		protected Statement(IDataEntity entity, string alias = null) : base(entity, alias)
		{
			this.From = new SourceCollection();
			this.From.Add(this.Table);
		}
		#endregion

		#region 公共属性
		/// <summary>获取一个值，指示<c>Form</c>属性是否有值。</summary>
		public bool HasFrom => this.From?.Count > 0;

		/// <summary>获取一个数据源的集合，可以在<c>Where</c>子句中引用的字段源。</summary>
		public SourceCollection From { get; }

		/// <summary>获取或设置条件子句。</summary>
		public IExpression Where { get; set; }
		#endregion

		#region 公共方法
		public JoinClause Join(Aliaser aliaser, ISource source, IDataEntity target, string fullPath = null)
		{
			var clause = JoinClause.Create(source,
			                               target,
			                               fullPath,
			                               name => this.From.TryGetValue(name, out var join) ? (JoinClause)join : null,
			                               entity => new TableIdentifier(entity, aliaser.Generate()));

			if(!this.From.Contains(clause))
				this.From.Add(clause);

			return clause;
		}

		public JoinClause Join(Aliaser aliaser, ISource source, IDataEntityComplexProperty complex, string fullPath = null)
		{
			var joins = JoinClause.Create(source,
			                              complex,
			                              fullPath,
			                              name => this.From.TryGetValue(name, out var join) ? (JoinClause)join : null,
			                              entity => new TableIdentifier(entity, aliaser.Generate()));

			JoinClause last = null;

			foreach(var join in joins)
			{
				if(!this.From.Contains(join))
					this.From.Add(join);

				last = join;
			}

			//返回最后一个Join子句
			return last;
		}

		public JoinClause Join(Aliaser aliaser, ISource source, SchemaMember schema)
		{
			if(schema.Token.Property.IsSimplex)
				return null;

			return this.Join(aliaser, source, (IDataEntityComplexProperty)schema.Token.Property, schema.FullPath);
		}
		#endregion
	}
}
