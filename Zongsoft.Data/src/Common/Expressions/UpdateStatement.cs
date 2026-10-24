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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data.Common.Expressions;

public class UpdateStatement : MutateStatement
{
	#region 构造函数
	public UpdateStatement(Metadata.IDataEntity entity, SchemaMember schema = null) : base(entity, schema)
	{
		this.Tables = [this.Table];
		this.Fields = [];
		this.Links = [];
	}
	#endregion

	#region 公共属性
	/// <summary>获取一个表标识的集合，表示要修改的表。</summary>
	public IList<TableIdentifier> Tables { get; }

	/// <summary>获取更新字段/值的集合。</summary>
	public ICollection<FieldValue> Fields { get; }
	#endregion

	#region 内部属性
	/// <summary>获取关联更新信息的集合，包含从属更新语句、关联字段映射及去重后的目标键。</summary>
	internal List<Link> Links { get; } = [];
	#endregion

	#region 嵌套子类
	internal sealed class Link(UpdateStatement statement)
	{
		public readonly UpdateStatement Statement = statement;
		public readonly List<KeyValuePair<FieldIdentifier, FieldIdentifier>> Fields = [];
		public readonly HashSet<object[]> Keys = new(KeyComparer.Instance);

		private sealed class KeyComparer : IEqualityComparer<object[]>
		{
			public static readonly KeyComparer Instance = new();
			public bool Equals(object[] left, object[] right) => StructuralComparisons.StructuralEqualityComparer.Equals(left, right);
			public int GetHashCode(object[] key) => StructuralComparisons.StructuralEqualityComparer.GetHashCode(key);
		}
	}
	#endregion
}
