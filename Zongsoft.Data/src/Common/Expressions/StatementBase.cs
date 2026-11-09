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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Collections.Generic;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions;

public abstract class StatementBase : Expression, IStatementBase
{
	#region 成员字段
	private ICollection<IStatementBase> _slaves;
	#endregion

	#region 构造函数
	protected StatementBase(ParameterExpressionCollection parameters = null)
	{
		this.Slots = new StatementSlotCollection();
		this.Parameters = parameters ?? this.CreateParameters();
	}

	protected StatementBase(TableIdentifier table, ParameterExpressionCollection parameters = null)
	{
		this.Table = table ?? throw new ArgumentNullException(nameof(table));
		this.Slots = new StatementSlotCollection();
		this.Parameters = parameters ?? this.CreateParameters();
	}

	protected StatementBase(IDataEntity entity, string alias, ParameterExpressionCollection parameters = null)
	{
		this.Table = new TableIdentifier(entity, alias);
		this.Slots = new StatementSlotCollection();
		this.Parameters = parameters ?? this.CreateParameters();
	}
	#endregion

	#region 公共属性
	public TableIdentifier Table { get; protected set; }
	public IDataEntity Entity => this.Table?.Entity;
	public StatementSlotCollection Slots { get; }
	public ParameterExpressionCollection Parameters { get; }
	public virtual bool HasSlaves => _slaves != null && _slaves.Count > 0;
	public virtual ICollection<IStatementBase> Slaves
	{
		get
		{
			if(_slaves == null)
				System.Threading.Interlocked.CompareExchange(ref _slaves, new List<IStatementBase>(), null);

			return _slaves;
		}
	}
	#endregion

	#region 虚拟方法
	protected virtual ParameterExpressionCollection CreateParameters() => new();
	#endregion

	#region 公共方法
	public ISelectStatementBase Subquery(TableIdentifier table) => new SubqueryStatement(this, table);
	#endregion

	#region 嵌套子类
	private class SubqueryStatement : SelectStatement, ISelectStatementBase
	{
		#region 构造函数
		public SubqueryStatement(IStatementBase host, TableIdentifier table) : base(table, string.Empty, host?.Parameters)
		{
			this.Host = host ?? throw new ArgumentNullException(nameof(host));
		}
		#endregion

		#region 公共属性
		public IStatementBase Host { get; }
		#endregion

		#region 重写方法
		protected override ParameterExpressionCollection CreateParameters() => this.Host.Parameters;
		#endregion
	}
	#endregion
}
