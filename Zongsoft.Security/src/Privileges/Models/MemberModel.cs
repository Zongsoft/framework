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

using Zongsoft.Data;
using Zongsoft.Components;

namespace Zongsoft.Security.Privileges.Models;

[Model($"{Module.NAME}.Member")]
public abstract class MemberModel : IMember<RoleModel>, IMember, IEquatable<MemberModel>
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
	Identifier IMember.RoleId => new Identifier<uint>(Roles.Identifiers.Role, this.RoleId);
	Identifier IMember.MemberId => this.MemberType switch
	{
		MemberType.Role => new Identifier<uint>(Roles.Identifiers.Role, this.MemberId),
		MemberType.User => new Identifier<uint>(Users.Identifiers.User, this.MemberId),
		_ => default,
	};
	#endregion

	#region 重写方法
	public bool Equals(MemberModel other) => other is not null &&
		this.RoleId == other.RoleId &&
		this.MemberId == other.MemberId &&
		this.MemberType == other.MemberType;

	bool IEquatable<IMember>.Equals(IMember other) => other is not null &&
		((IMember)this).RoleId.Equals(other.RoleId) &&
		((IMember)this).MemberId.Equals(other.MemberId) &&
		this.MemberType == other.MemberType;

	public bool Equals(IMember<RoleModel> other) => other is not null &&
		((IMember)this).RoleId.Equals(other.RoleId) &&
		((IMember)this).MemberId.Equals(other.MemberId) &&
		this.MemberType == other.MemberType;

	public override bool Equals(object obj) => obj is MemberModel other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(this.RoleId, this.MemberId, this.MemberType);
	public override string ToString() => $"{this.RoleId}:{this.MemberId}@{this.MemberType}";
	#endregion
}
