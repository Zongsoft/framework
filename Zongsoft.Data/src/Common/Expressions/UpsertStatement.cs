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

public class UpsertStatement : MutateStatement, IBatchStatement
{
	#region 构造函数
	public UpsertStatement(IDataEntity entity, SchemaMember schema) : base(entity, schema, null)
	{
		this.Fields = new List<FieldIdentifier>();
		this.Values = new List<IExpression>();
		this.Updation = new List<FieldValue>();
		this.Options = new UpsertStatementOptions();
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置单次执行的最大记录数，默认为 256。</summary>
	public int BatchSize { get; set; } = 256;

	/// <summary>获取或设置单次执行的参数数量上限，零表示不额外限制。</summary>
	public int ParameterLimit { get; set; }

	/// <summary>获取或设置增改语句对应的序号字段值的查询语句。</summary>
	public SelectStatement SequenceRetriever { get; set; }

	/// <summary>获取新增或更新字段集合。</summary>
	public IList<FieldIdentifier> Fields { get; }

	/// <summary>获取新增或更新字段值集合。</summary>
	public IList<IExpression> Values { get; }

	/// <summary>获取更新字段/值的集合。</summary>
	public ICollection<FieldValue> Updation { get; }

	/// <summary>获取插入语句的选项。</summary>
	public UpsertStatementOptions Options { get; }
	#endregion
}

public class UpsertStatementOptions
{
	#region 公共属性
	/// <summary>获取或设置一个值，指示是否忽略插入数据导致的数据库约束（主键、唯一索引、外键约束等）冲突。</summary>
	public bool ConstraintIgnored { get; set; }
	#endregion

	#region 公共方法
	public void Apply(IDataUpsertOptions options)
	{
		if(options == null)
			return;

		this.ConstraintIgnored = options.ConstraintIgnored;
	}
	#endregion
}
