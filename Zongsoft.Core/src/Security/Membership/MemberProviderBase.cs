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
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Services;

namespace Zongsoft.Security.Membership
{
	public abstract class MemberProviderBase<TRole, TUser> : IMemberProvider<TRole, TUser> where TRole : IRoleModel where TUser : IUserModel
	{
		#region 成员字段
		private IDataAccess _dataAccess;
		#endregion

		#region 构造函数
		protected MemberProviderBase(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 公共属性
		public IDataAccess DataAccess
		{
			get
			{
				if(_dataAccess == null)
				{
					_dataAccess = this.ServiceProvider.ResolveRequired<IDataAccessProvider>().GetAccessor(MembershipUtility.Security);

					if(_dataAccess != null && !string.IsNullOrEmpty(Mapping.Instance.Member))
					{
						_dataAccess.Naming.Map<Member>(Mapping.Instance.Member);
						_dataAccess.Naming.Map<Member<TRole, TUser>>(Mapping.Instance.Member);
					}
				}
				return _dataAccess;
			}
		}

		public IServiceProvider ServiceProvider { get; }
		#endregion

		#region 公共方法
		public IEnumerable<TRole> GetAncestors(uint memberId, MemberType memberType)
		{
			//获取指定成员对应的命名空间值
			var value = memberType switch
			{
				MemberType.User => this.DataAccess.Select<object>(Mapping.Instance.User, Condition.Equal(nameof(IUserModel.UserId), memberId), Mapping.Instance.Namespace.GetField(Mapping.Instance.User), Paging.Limit(1)).FirstOrDefault(),
				MemberType.Role => this.DataAccess.Select<object>(Mapping.Instance.Role, Condition.Equal(nameof(IRoleModel.RoleId), memberId), Mapping.Instance.Namespace.GetField(Mapping.Instance.Role), Paging.Limit(1)).FirstOrDefault(),
				_ => null,
			};

			return MembershipUtility.GetAncestors<TRole>(this.DataAccess, memberId, memberType, value);
		}

		public IEnumerable<TRole> GetRoles(uint memberId, MemberType memberType)
		{
			//获取指定成员对应的命名空间值
			var value = memberType switch
			{
				MemberType.User => this.DataAccess.Select<object>(Mapping.Instance.User, Condition.Equal(nameof(IUserModel.UserId), memberId), Mapping.Instance.Namespace.GetField(Mapping.Instance.User), Paging.Limit(1)).FirstOrDefault(),
				MemberType.Role => this.DataAccess.Select<object>(Mapping.Instance.Role, Condition.Equal(nameof(IRoleModel.RoleId), memberId), Mapping.Instance.Namespace.GetField(Mapping.Instance.Role), Paging.Limit(1)).FirstOrDefault(),
				_ => null,
			};

			return this.DataAccess.Select<Member<TRole, TUser>>(
				Condition.Equal(nameof(Member.MemberId), memberId) &
				Condition.Equal(nameof(Member.MemberType), memberType) &
				Condition.Equal($"{nameof(Member.Role)}.{Mapping.Instance.Namespace.GetField(Mapping.Instance.Role)}", value),
				$"*, {nameof(Member.Role)}" + "{*}").Map(p => p.Role);
		}

		public IEnumerable<Member<TRole, TUser>> GetMembers(uint roleId, string schema = null)
		{
			return this.DataAccess.Select<Member<TRole, TUser>>(Condition.Equal(nameof(Member.RoleId), roleId), schema);
		}

		public bool SetMember(uint roleId, uint memberId, MemberType memberType)
		{
			if(roleId == 0 || memberId == 0)
				return false;

			return this.DataAccess.Upsert(new Member(roleId, memberId, memberType)) > 0;
		}

		public int SetMembers(IEnumerable<Member> members)
		{
			if(members == null)
				return 0;

			return this.DataAccess.UpsertMany(members);
		}

		public int SetMembers(uint roleId, params Member[] members)
		{
			if(members == null || members.Length == 0)
				return 0;

			return this.SetMembers(roleId, members, false);
		}

		public int SetMembers(uint roleId, IEnumerable<Member> members, bool shouldResetting = false)
		{
			if(members == null)
				return 0;

			using(var transaction = new Zongsoft.Transactions.Transaction())
			{
				int count = 0;

				//清空指定角色的所有成员
				if(shouldResetting)
					count = this.DataAccess.Delete<Member>(Condition.Equal(nameof(Member.RoleId), roleId));

				//写入指定的角色成员集到数据库中
				count = this.DataAccess.UpsertMany<Member>(members.Select(m => new Member(roleId, m.MemberId, m.MemberType)));

				//提交事务
				transaction.Commit();

				return count;
			}
		}

		public bool RemoveMember(uint roleId, uint memberId, MemberType memberType)
		{
			return this.DataAccess.Delete<Member>(
				Condition.Equal(nameof(Member.RoleId), roleId) &
				Condition.Equal(nameof(Member.MemberId), memberId) &
				Condition.Equal(nameof(Member.MemberType), memberType)) > 0;
		}

		public int RemoveMembers(uint roleId, IEnumerable<Member> members = null)
		{
			var conditions = ConditionCollection.And(Condition.Equal(nameof(Member.RoleId), roleId));

			if(members != null && members.Any())
			{
				int count = 0;

				foreach(var type in members.GroupBy(p => p.MemberType))
				{
					conditions.Add(Condition.Equal(nameof(Member.MemberType), type.Key));
					conditions.Add(Condition.In(nameof(Member.MemberId), type.Select(p => p.MemberId).Distinct()));

					count += this.DataAccess.Delete<Member>(conditions);
				}

				return count;
			}

			return this.DataAccess.Delete<Member>(conditions);
		}
		#endregion
	}
}
