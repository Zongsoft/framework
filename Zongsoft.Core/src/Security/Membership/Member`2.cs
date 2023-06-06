/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Security.Membership
{
	public struct Member<TRole, TUser> : IEquatable<Member<TRole, TUser>> where TRole : IRoleModel where TUser : IUserModel
	{
		#region 公共属性
		public uint RoleId { get;set; }
		public uint MemberId { get;set; }
		public MemberType MemberType { get; set; }

		public TRole Role { get; set; }
		public TRole MemberRole { get;set; }
		public TUser MemberUser { get;set; }
		#endregion

		#region 重写方法
		public bool Equals(Member<TRole, TUser> other)
		{
			return this.RoleId == other.RoleId &&
				   this.MemberId == other.MemberId &&
				   this.MemberType == other.MemberType;
		}

		public override bool Equals(object obj) => obj is Member<TRole, TUser> other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.RoleId, this.MemberId, this.MemberType);
		public override string ToString() => $"{this.RoleId}-{this.MemberType}:{this.MemberId}";
		#endregion
	}
}
