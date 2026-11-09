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

public class OrderByClause
{
	#region 构造函数
	public OrderByClause()
	{
		this.Members = new HashSet<OrderByMember>();
	}
	#endregion

	#region 公共属性
	public ICollection<OrderByMember> Members { get; }
	#endregion

	#region 公共方法
	public OrderByMember Add(FieldIdentifier field, SortingMode mode = SortingMode.Ascending)
	{
		var member = new OrderByMember(field, mode);
		this.Members.Add(member);
		return member;
	}
	#endregion

	#region 嵌套结构
	public struct OrderByMember(FieldIdentifier field, SortingMode mode = SortingMode.Ascending)
	{
		public FieldIdentifier Field = field;
		public SortingMode Mode = mode;
	}
	#endregion
}
