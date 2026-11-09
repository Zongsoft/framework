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

namespace Zongsoft.Data.Metadata;

/// <summary>表示数据实体关联约束的元数据类。</summary>
public readonly struct DataAssociationConstraint
{
	#region 构造函数
	public DataAssociationConstraint(string name, DataAssociationConstraintActor actor, object value)
	{
		this.Name = name;
		this.Actor = actor;
		this.Value = value;
	}
	#endregion

	#region 公共属性
	/// <summary>获取关联约束的目标成员名。</summary>
	public string Name { get; }

	/// <summary>获取关联约束的主体(即约束目标)。</summary>
	public DataAssociationConstraintActor Actor { get; }

	/// <summary>获取关联约束的目标值。</summary>
	public object Value { get; }
	#endregion

	#region 重写方法
	public override string ToString() => this.Value == null || Convert.IsDBNull(this.Value) ?
		$"{this.Actor}:{this.Name}=NULL" :
		$"{this.Actor}:{this.Name}={this.Value}";
	#endregion
}
