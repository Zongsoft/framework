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
	public abstract class StatementBase : Expression, IStatementBase
	{
		#region 成员字段
		private ICollection<IStatementBase> _slaves;
		private ParameterExpressionCollection _parameters;
		#endregion

		#region 构造函数
		protected StatementBase()
		{
		}

		protected StatementBase(TableIdentifier table)
		{
			this.Table = table ?? throw new ArgumentNullException(nameof(table));
		}

		protected StatementBase(IDataEntity entity, string alias = null)
		{
			this.Table = new TableIdentifier(entity, alias);
		}
		#endregion

		#region 公共属性
		public TableIdentifier Table { get; protected set; }

		public IDataEntity Entity { get => this.Table?.Entity; }

		public virtual bool HasSlaves { get => _slaves != null && _slaves.Count > 0; }

		public virtual ICollection<IStatementBase> Slaves
		{
			get
			{
				if(_slaves == null)
					System.Threading.Interlocked.CompareExchange(ref _slaves, new List<IStatementBase>(), null);

				return _slaves;
			}
		}

		public virtual bool HasParameters { get => _parameters != null && _parameters.Count > 0; }

		public virtual ParameterExpressionCollection Parameters
		{
			get
			{
				if(_parameters == null)
				{
					lock(this)
					{
						if(_parameters == null)
							_parameters = this.CreateParameters();
					}
				}

				return _parameters;
			}
		}
		#endregion

		#region 虚拟方法
		protected virtual ParameterExpressionCollection CreateParameters()
		{
			return new ParameterExpressionCollection();
		}
		#endregion

		#region 公共方法
		public ISelectStatementBase Subquery(TableIdentifier table)
		{
			return new SubqueryStatement(this, table);
		}
		#endregion

		#region 嵌套子类
		private class SubqueryStatement : SelectStatement, ISelectStatementBase
		{
			#region 构造函数
			public SubqueryStatement(IStatementBase host, TableIdentifier table) : base(table, string.Empty)
			{
				this.Host = host ?? throw new ArgumentNullException(nameof(host));
			}
			#endregion

			#region 公共属性
			public IStatementBase Host { get; }
			#endregion

			#region 重写方法
			protected override ParameterExpressionCollection CreateParameters() => Host.Parameters;
			#endregion
		}
		#endregion
	}
}
