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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Opc.Ua;

namespace Zongsoft.Externals.Opc;

public partial class OpcNodeType : IEquatable<OpcNodeType>
{
	#region 构造函数
	internal OpcNodeType(NodeId nodeId, string name = null)
	{
		this.NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
		this.Identifier = nodeId.ToString();
		this.Name = name;
	}

	public OpcNodeType(string identifier, string name = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(identifier);
		this.NodeId = NodeId.Parse(identifier);
		this.Identifier = identifier;
		this.Name = name;
	}
	#endregion

	#region 内部字段
	internal readonly NodeId NodeId;
	#endregion

	#region 公共属性
	public string Identifier { get; }
	public string Name { get; }
	#endregion

	#region 重写方法
	public bool Equals(OpcNodeType other) => other is not null && this.NodeId == other.NodeId && this.Identifier == other.Identifier;
	public override bool Equals(object obj) => obj is OpcNodeType other && this.Equals(other);
	public override int GetHashCode() => this.NodeId == null || this.NodeId.IsNullNodeId ? HashCode.Combine(this.Identifier) : this.NodeId.GetHashCode();
	public override string ToString() => string.IsNullOrEmpty(this.Name) ? this.Identifier : $"{this.Name}|{this.Identifier}";
	#endregion
}

partial class OpcNodeType
{
	private static readonly Dictionary<NodeId, OpcNodeType> _types = new()
	{
		{ DataTypeIds.String, new(DataTypeIds.String, nameof(String)) },
		{ DataTypeIds.Boolean, new(DataTypeIds.Boolean, nameof(Boolean)) },
		{ DataTypeIds.DateTime, new(DataTypeIds.DateTime, nameof(DateTime)) },
		{ DataTypeIds.Byte, new(DataTypeIds.Byte, nameof(Byte)) },
		{ DataTypeIds.SByte, new(DataTypeIds.SByte, nameof(SByte)) },
		{ DataTypeIds.Int16, new(DataTypeIds.Int16, nameof(Int16)) },
		{ DataTypeIds.Int32, new(DataTypeIds.Int32, nameof(Int32)) },
		{ DataTypeIds.Int64, new(DataTypeIds.Int64, nameof(Int64)) },
		{ DataTypeIds.UInt16, new(DataTypeIds.UInt16, nameof(UInt16)) },
		{ DataTypeIds.UInt32, new(DataTypeIds.UInt32, nameof(UInt32)) },
		{ DataTypeIds.UInt64, new(DataTypeIds.UInt64, nameof(UInt64)) },
		{ DataTypeIds.Float, new(DataTypeIds.Float, nameof(DataTypeIds.Float)) },
		{ DataTypeIds.Double, new(DataTypeIds.Double, nameof(DataTypeIds.Double)) },
		{ DataTypeIds.Number, new(DataTypeIds.Number, nameof(DataTypeIds.Number)) },
		{ DataTypeIds.Decimal, new(DataTypeIds.Decimal, nameof(DataTypeIds.Decimal)) },

		{ ObjectTypeIds.FolderType, new(ObjectTypeIds.FolderType, "Folder") },

		{ VariableTypeIds.BaseVariableType, new(VariableTypeIds.BaseVariableType, "Variable") },
		{ VariableTypeIds.BaseDataVariableType, new(VariableTypeIds.BaseDataVariableType, "Scalar") },

		{ ReferenceTypeIds.Organizes, new(ReferenceTypeIds.Organizes, nameof(ReferenceTypeIds.Organizes)) },
		{ ReferenceTypeIds.References, new(ReferenceTypeIds.References, nameof(ReferenceTypeIds.References)) },
	};

	internal static OpcNodeType Get(ExpandedNodeId id) => Get((NodeId)id);
	internal static OpcNodeType Get(NodeId id)
	{
		if(_types.TryGetValue(id, out var type))
			return type;

		lock(_types)
		{
			if(_types.TryGetValue(id, out type))
				return type;

			return _types[id] = type = new(id);
		}
	}
}