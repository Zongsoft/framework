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
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Data;

namespace Zongsoft.Security.Membership
{
	public static class MembershipUtility
	{
		public static bool InRoles(IDataAccess dataAccess, IUserIdentity user, params string[] roleNames)
		{
			if(user == null || user.Name == null || roleNames == null || roleNames.Length < 1)
				return false;

			//如果指定的用户编号对应的是系统内置管理员（即 Administrator），那么它拥有对任何角色的隶属判断
			if(string.Equals(user.Name, User.Administrator, StringComparison.OrdinalIgnoreCase))
				return true;

			//处理非系统内置管理员账号
			if(GetAncestors(dataAccess, user, out var flats, out _) > 0)
			{
				//如果所属的角色中包括系统内置管理员，则该用户自然属于任何角色
				return flats.Any(role =>
					string.Equals(role.Name, Role.Administrators, StringComparison.OrdinalIgnoreCase) ||
					roleNames.Contains(role.Name)
				);
			}

			return false;
		}

		public static int GetAncestors(IDataAccess dataAccess, IUserIdentity user, out ISet<IRole> flats, out IList<IEnumerable<IRole>> hierarchies)
		{
			if(user == null)
			{
				flats = null;
				hierarchies = null;
				return 0;
			}

			return GetAncestors(dataAccess, user.UserId, user.Name, user.Namespace, out flats, out hierarchies);
		}

		public static int GetAncestors(IDataAccess dataAccess, uint userId, string name, string @namespace, out ISet<IRole> flats, out IList<IEnumerable<IRole>> hierarchies)
		{
			flats = null;
			hierarchies = null;

			//如果指定的用户编号为零或用户名为空则退出
			if(userId == 0 || string.IsNullOrEmpty(name))
				return 0;

			//如果指定编号的用户是内置的“Administrator”账号，则直接返回（因为内置管理员只隶属于内置的“Administrators”角色，而不能属于其他角色）
			if(string.Equals(name, User.Administrator, StringComparison.OrdinalIgnoreCase))
			{
				//获取当前用户同命名空间下的“Administrators”内置角色
				flats = new HashSet<IRole>(dataAccess.Select<IRole>(Condition.Equal(nameof(IRole.Name), Role.Administrators) & Condition.Equal(nameof(IRole.Namespace), @namespace)));

				if(flats.Count > 0)
				{
					hierarchies = new List<IEnumerable<IRole>>
					{
						flats
					};
				}

				return flats.Count;
			}

			return GetAncestors(dataAccess, @namespace, userId, MemberType.User, out flats, out hierarchies);
		}

		public static int GetAncestors(IDataAccess dataAccess, IRole role, out ISet<IRole> flats, out IList<IEnumerable<IRole>> hierarchies)
		{
			flats = null;
			hierarchies = null;

			//如果指定编号的角色不存在或是一个内置角色（内置角色没有归属），则退出
			if(role == null || IsBuiltin(role.Name))
				return 0;

			return GetAncestors(dataAccess, role.Namespace, role.RoleId, MemberType.Role, out flats, out hierarchies);
		}

		/// <summary>
		/// 获取指定用户或角色的上级角色集。
		/// </summary>
		/// <param name="dataAccess">数据访问服务。</param>
		/// <param name="memberId">成员编号（用户或角色）。</param>
		/// <param name="memberType">成员类型，表示<paramref name="memberId"/>对应的成员类型。</param>
		/// <param name="flats">输出参数，表示所隶属的所有上级角色集，该集已经去除重复。</param>
		/// <param name="hierarchies">输出参数，表示所隶属的所有上级角色的层级列表，该列表包含的所有角色已经去除重复。</param>
		/// <returns>返回指定成员隶属的所有上级角色去重后的数量。</returns>
		public static int GetAncestors(IDataAccess dataAccess, string @namespace, uint memberId, MemberType memberType, out ISet<IRole> flats, out IList<IEnumerable<IRole>> hierarchies)
		{
			if(dataAccess == null)
				throw new ArgumentNullException(nameof(dataAccess));

			flats = null;
			hierarchies = null;

			//获取指定用户所属命名空间下的所有成员及其关联的角色对象（注：即时加载到内存中）
			var members = dataAccess.Select<Member>(Condition.Equal("Role.Namespace", @namespace), "*, Role{*}")
									.Where(m => m.Role != null)
									.ToArray();

			flats = new HashSet<IRole>();
			hierarchies = new List<IEnumerable<IRole>>();

			//从角色成员集合中查找出指定成员的父级角色
			var parents = members.Where(m => m.MemberId == memberId && m.MemberType == memberType)
								 .Select(m => m.Role).ToList();

			//如果父级角色集不为空
			while(parents.Any())
			{
				//将父角色集合并到输出参数中
				flats.UnionWith(parents);
				//将特定层级的所有父角色集加入到层级列表中
				hierarchies.Add(parents);

				//从角色成员集合中查找出当前层级中所有角色的父级角色集合（并进行全局去重）
				parents = members.Where(m => parents.Any(p => p.RoleId == m.MemberId) && m.MemberType == MemberType.Role)
								 .Select(m => m.Role)
								 .Except(flats).ToList();
			}

			return flats.Count;
		}

		internal static Condition GetUserIdentity(string identity)
		{
			return GetUserIdentity(identity, out _);
		}

		internal static Condition GetUserIdentity(string identity, out UserIdentityType identityType)
		{
			if(string.IsNullOrWhiteSpace(identity))
				throw new ArgumentNullException(nameof(identity));

			if(identity.Contains("@"))
			{
				identityType = UserIdentityType.Email;
				return Condition.Equal(nameof(IUser.Email), identity);
			}

			if(IsNumericString(identity))
			{
				identityType = UserIdentityType.Phone;
				return Condition.Equal(nameof(IUser.Phone), identity);
			}

			identityType = UserIdentityType.Name;
			return Condition.Equal(nameof(IUser.Name), identity);
		}

		internal static UserIdentityType GetIdentityType(string identity)
		{
			if(string.IsNullOrEmpty(identity))
				throw new ArgumentNullException(nameof(identity));

			if(identity.Contains("@"))
				return UserIdentityType.Email;

			if(IsNumericString(identity))
				return UserIdentityType.Phone;

			return UserIdentityType.Name;
		}

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static bool IsNumericString(string text)
		{
			if(string.IsNullOrEmpty(text))
				return false;

			for(var i = 0; i < text.Length; i++)
			{
				if(text[i] < '0' || text[i] > '9')
					return false;
			}

			return true;
		}

		private static bool IsBuiltin(string name)
		{
			return string.Equals(name, User.Administrator, StringComparison.OrdinalIgnoreCase) ||
			       string.Equals(name, Role.Administrators, StringComparison.OrdinalIgnoreCase);
		}
		#endregion

	}
}
