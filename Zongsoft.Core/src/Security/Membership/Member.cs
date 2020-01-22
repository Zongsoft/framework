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
	[Zongsoft.Data.Model("Security.Member")]
	public struct Member : IEquatable<Member>
	{
		#region 成员字段
		public uint RoleId;
		public uint MemberId;
		public MemberType MemberType;

		public IRole Role;
		public IRole MemberRole;
		public IUser MemberUser;
		#endregion

		#region 构造函数
		public Member(uint roleId, uint memberId, MemberType memberType)
		{
			this.RoleId = roleId;
			this.MemberId = memberId;
			this.MemberType = memberType;

			this.Role = null;
			this.MemberRole = null;
			this.MemberUser = null;
		}
		#endregion

		#region 重写方法
		public bool Equals(Member other)
		{
			return this.RoleId == other.RoleId &&
			       this.MemberId == other.MemberId &&
			       this.MemberType == other.MemberType;
		}

		public override bool Equals(object obj)
		{
			if(obj == null || obj.GetType() != this.GetType())
				return false;

			return base.Equals((Member)obj);
		}

		public override int GetHashCode()
		{
			return (int)(this.RoleId ^ this.MemberId ^ (int)this.MemberType);
		}

		public override string ToString()
		{
			return $"{this.RoleId.ToString()}-{this.MemberType.ToString()}:{this.MemberId.ToString()}";
		}
		#endregion
	}
}
