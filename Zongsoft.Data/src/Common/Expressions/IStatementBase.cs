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

namespace Zongsoft.Data.Common.Expressions;

/// <summary>表示语句的基础接口。</summary>
public interface IStatementBase : IExpression
{
	#region 属性定义
	/// <summary>获取语句对应的主表。</summary>
	TableIdentifier Table { get; }

	/// <summary>获取当前语句的插槽集。</summary>
	StatementSlotCollection Slots { get; }

	/// <summary>获取一个值，指示当前语句是否有依附于自己的从属语句。</summary>
	bool HasSlaves { get; }

	/// <summary>获取依附于当前语句的从属语句集合。</summary>
	/// <remarks>对于只是获取从属语句的使用者，应先使用<see cref="HasSlaves"/>属性进行判断成功后再使用该属性，这样可避免创建不必要的集合对象。</remarks>
	ICollection<IStatementBase> Slaves { get; }

	/// <summary>获取当前语句的参数集。</summary>
	ParameterExpressionCollection Parameters { get; }
	#endregion

	#region 方法定义
	/// <summary>创建一个子查询语句。</summary>
	/// <param name="table">指定要创建的子查询的主表。</param>
	/// <returns>返回创建的子查询语句。</returns>
	ISelectStatementBase Subquery(TableIdentifier table);
	#endregion
}
