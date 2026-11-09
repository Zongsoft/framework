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

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions;

/// <summary>表示查询语句的基类。</summary>
public abstract class SelectStatementBase : Statement, ISelectStatementBase, ISource
{
	#region 构造函数
	protected SelectStatementBase(string alias = null, ParameterExpressionCollection parameters = null) : base(parameters)
	{
		this.Alias = alias ?? string.Empty;
		this.Select = new SelectClause();
	}

	protected SelectStatementBase(ISource source, string alias = null, ParameterExpressionCollection parameters = null) : base(source, parameters)
	{
		if(source == null)
			throw new ArgumentNullException(nameof(source));

		this.Alias = alias ?? source.Alias;
		this.Select = new SelectClause();
	}

	protected SelectStatementBase(IDataEntity entity, string alias = null, ParameterExpressionCollection parameters = null) : base(entity, "T", parameters)
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

	/// <summary>获取或设置 With 子句。</summary>
	public CommonTableExpressionCollection With { get; set; }
	#endregion

	#region 公共方法
	public FieldIdentifier CreateField(string name, string alias = null) => new FieldIdentifier(this, name, alias);
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
