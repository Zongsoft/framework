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

using Zongsoft.Common;
using Zongsoft.Components;

namespace Zongsoft.Security.Privileges;

public class Member<TRole> : IMember<TRole>, IMember, IEquatable<Member<TRole>>, IEquatable<IMember<TRole>> where TRole : IRole
{
	#region 构造函数
	protected Member(Identifier roleId, Member member)
	{
		this.RoleId = roleId;
		this.MemberId = member.MemberId;
		this.MemberType = member.MemberType;
	}

	protected Member(Identifier roleId, Identifier memberId, MemberType memberType)
	{
		this.RoleId = roleId;
		this.MemberId = memberId;
		this.MemberType = memberType;
	}
	#endregion

	#region 公共属性
	public Identifier RoleId { get; protected set; }
	public Identifier MemberId { get; protected set; }
	public MemberType MemberType { get; protected set; }
	public TRole Role { get; protected set; }
	object IMember.Member => this.GetMember();
	#endregion

	#region 虚拟方法
	protected virtual object GetMember() => this.MemberType switch
	{
		MemberType.Role => Member.Role(this.MemberId),
		MemberType.User => Member.User(this.MemberId),
		_ => null,
	};
	#endregion

	#region 重写方法
	public bool Equals(IMember other) => other is not null &&
		this.RoleId == other.RoleId &&
		this.MemberId == other.MemberId &&
		this.MemberType == other.MemberType;
	public bool Equals(IMember<TRole> other) => other is not null &&
		this.RoleId == other.RoleId &&
		this.MemberId == other.MemberId &&
		this.MemberType == other.MemberType;
	public bool Equals(Member<TRole> other) => other is not null &&
		this.RoleId == other.RoleId &&
		this.MemberId == other.MemberId &&
		this.MemberType == other.MemberType;

	public override bool Equals(object obj) => obj is Member<TRole> other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(this.RoleId, this.MemberId, this.MemberType);
	public override string ToString() => $"{this.RoleId}:{this.MemberId}@{this.MemberType}";
	#endregion

	#region 符号重载
	public static bool operator ==(Member<TRole> left, Member<TRole> right) => left.Equals(right);
	public static bool operator !=(Member<TRole> left, Member<TRole> right) => !(left == right);

	public static bool operator ==(Member<TRole> left, IMember<TRole> right) => left.Equals(right);
	public static bool operator !=(Member<TRole> left, IMember<TRole> right) => !(left == right);

	public static bool operator ==(IMember<TRole> left, Member<TRole> right) => left.Equals(right);
	public static bool operator !=(IMember<TRole> left, Member<TRole> right) => !(left == right);
	#endregion
}
