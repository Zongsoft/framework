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

public class ReturningClause
{
	#region 成员字段
	private readonly HashSet<ReturningMember> _members;
	#endregion

	#region 构造函数
	public ReturningClause(TableDefinition table, params ReturningMember[] members)
	{
		this.Table = table ?? throw new ArgumentNullException(nameof(table));

		if(members == null || members.Length == 0)
			_members = new();
		else
			_members = [.. members];
	}

	public ReturningClause(params ReturningMember[] members)
	{
		if(members == null || members.Length == 0)
			_members = new();
		else
			_members = [.. members];
	}
	#endregion

	#region 公共属性
	/// <summary>获取 Returning/Output 子句的输出(INTO)表定义，如果为空则表示无输出表。</summary>
	public TableDefinition Table { get; }

	/// <summary>获取 Returning/Output 子句的成员字段集，空集不生成输出子句。</summary>
	public ICollection<ReturningMember> Members => _members;
	#endregion

	#region 公共方法
	public ReturningMember? Append(FieldIdentifier field, ReturningKind kind)
	{
		var member = new ReturningMember(field, kind);
		return _members.Add(member) ? member : null;
	}
	#endregion

	#region 嵌套结构
	public readonly struct ReturningMember(FieldIdentifier field, ReturningKind kind) : IEquatable<ReturningMember>
	{
		public readonly FieldIdentifier Field = field;
		public readonly ReturningKind Kind = kind;

		public bool Equals(ReturningMember other) => object.Equals(this.Field, other.Field) && this.Kind == other.Kind;
		public override bool Equals(object obj) => obj is ReturningMember other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Field, this.Kind);
		public override string ToString() => $"{this.Field}({this.Kind})";

		public static bool operator ==(ReturningMember left, ReturningMember right) => left.Equals(right);
		public static bool operator !=(ReturningMember left, ReturningMember right) => !(left == right);
	}
	#endregion
}
