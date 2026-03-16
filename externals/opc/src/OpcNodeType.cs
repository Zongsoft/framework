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
	internal OpcNodeType(NodeId nodeId, Type type)
	{
		this.NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
		this.Identifier = nodeId.ToString();
		this.Type = type ?? typeof(object);
		this.Name = Common.TypeAlias.GetAlias(this.Type);
	}

	public OpcNodeType(string identifier, Type type)
	{
		ArgumentException.ThrowIfNullOrEmpty(identifier);
		this.NodeId = NodeId.Parse(identifier);
		this.Identifier = identifier;
		this.Type = type ?? typeof(object);
		this.Name = Common.TypeAlias.GetAlias(this.Type);
	}
	#endregion

	#region 内部字段
	internal readonly NodeId NodeId;
	#endregion

	#region 公共属性
	public string Identifier { get; }
	public string Name { get; }
	public Type Type { get; }
	#endregion

	#region 公共方法
	public bool IsArray(out Type elementType)
	{
		if(this.Type != null && this.Type.IsArray)
		{
			elementType = this.Type.GetElementType();
			return true;
		}

		elementType = null;
		return false;
	}
	#endregion

	#region 重写方法
	public bool Equals(OpcNodeType other) => other is not null && this.NodeId == other.NodeId && this.Identifier == other.Identifier;
	public override bool Equals(object obj) => obj is OpcNodeType other && this.Equals(other);
	public override int GetHashCode() => this.NodeId == null || this.NodeId.IsNullNodeId ? HashCode.Combine(this.Identifier) : this.NodeId.GetHashCode();
	public override string ToString() => string.IsNullOrEmpty(this.Name) ? this.Identifier : $"{this.Name}|{this.Identifier}";
	#endregion

	#region 符号重写
	public static implicit operator Type(OpcNodeType type) => type?.Type;
	#endregion
}

partial class OpcNodeType
{
	private readonly struct Key(NodeId id, int rank = 0) : IEquatable<Key>
	{
		public readonly NodeId Id = id;
		public readonly int Rank = rank;

		public bool Equals(Key other) => this.Id == other.Id && this.Rank == other.Rank;
		public override bool Equals(object obj) => obj is Key other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Id, this.Rank);
		public override string ToString() => this.Rank == 0 ? this.Id.ToString() : $"{this.Id}[{this.Rank}]";
	}

	private static readonly Dictionary<Key, OpcNodeType> _cache = [];
	internal static OpcNodeType Get(ExpandedNodeId id, int rank = 0) => Get((NodeId)id, rank);
	internal static OpcNodeType Get(NodeId id, int rank = 0)
	{
		if(id == null || id.IsNullNodeId)
			return null;

		var key = new Key(id, rank);

		if(_cache.TryGetValue(key, out var result))
			return result;

		lock(_cache)
		{
			if(_cache.TryGetValue(key, out result))
				return result;

			return _cache[key] = result = new(id, Utility.GetDataType(id, rank));
		}
	}
}