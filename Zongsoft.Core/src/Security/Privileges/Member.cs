﻿/*
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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Common;
using Zongsoft.Components;

namespace Zongsoft.Security.Privileges;

public readonly struct Member : IEquatable<Member>
{
	#region 构造函数
	public Member(Identifier memberId, MemberType memberType)
	{
		this.MemberId = memberId;
		this.MemberType = memberType;
	}
	#endregion

	#region 公共属性
	public Identifier MemberId { get; }
	public MemberType MemberType { get; }
	#endregion

	#region 重写方法
	public readonly bool Equals(Member other) =>
		this.MemberId == other.MemberId &&
		this.MemberType == other.MemberType;
	public override readonly bool Equals(object obj) => obj is Member other && this.Equals(other);
	public override readonly int GetHashCode() => HashCode.Combine(this.MemberId, this.MemberType);
	public override readonly string ToString() => $"{this.MemberId}@{this.MemberType}";
	#endregion

	#region 符号重载
	public static bool operator ==(Member left, Member right) => left.Equals(right);
	public static bool operator !=(Member left, Member right) => !(left == right);
	#endregion
}
