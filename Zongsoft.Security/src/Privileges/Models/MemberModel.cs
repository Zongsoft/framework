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
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Components;

namespace Zongsoft.Security.Privileges.Models;

public abstract class MemberModel : IMember<RoleModel>, IEquatable<MemberModel>
{
	#region 公共属性
	public abstract uint RoleId { get; set; }
	public abstract uint MemberId { get; set; }
	public abstract MemberType MemberType { get; set; }
	public abstract RoleModel Role { get; set; }
	public object Member => this.MemberType switch
	{
		MemberType.User => this.MemberUser,
		MemberType.Role => this.MemberRole,
		_ => null
	};

	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public abstract RoleModel MemberRole { get; set; }

	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public abstract UserModel MemberUser { get; set; }
	#endregion

	#region 显式实现
	Identifier IMember<RoleModel>.RoleId => new Identifier<uint>(typeof(RoleModel), this.RoleId);
	Identifier IMember<RoleModel>.MemberId => this.MemberType switch
	{
		MemberType.Role => new Identifier<uint>(typeof(RoleModel), this.MemberId),
		MemberType.User => new Identifier<uint>(typeof(UserModel), this.MemberId),
		_ => default,
	};
	#endregion

	#region 重写方法
	public bool Equals(MemberModel other) => other is not null &&
		this.RoleId == other.RoleId &&
		this.MemberId == other.MemberId &&
		this.MemberType == other.MemberType;
	public bool Equals(IMember<RoleModel> member) => member is MemberModel other && this.Equals(other);
	public override bool Equals(object obj) => obj is MemberModel other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(this.RoleId, this.MemberId, this.MemberType);
	public override string ToString() => $"{this.RoleId}:{this.MemberId}@{this.MemberType}";
	#endregion
}
