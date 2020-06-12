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
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Data;

namespace Zongsoft.Security.Membership
{
	internal static class MembershipHelper
	{
		#region 常量定义
		internal const string Administrator = "Administrator";
		internal const string Administrators = "Administrators";
		#endregion

		#region 公共方法
		public static bool InRoles(IDataAccess dataAccess, IUserIdentity user, params string[] roleNames)
		{
			if(user == null || user.Name == null || roleNames == null || roleNames.Length < 1)
				return false;

			//如果指定的用户编号对应的是系统内置管理员（即 Administrator），那么它拥有对任何角色的隶属判断
			if(string.Equals(user.Name, Administrator, StringComparison.OrdinalIgnoreCase))
				return true;

			//处理非系统内置管理员账号
			if(GetAncestors(dataAccess, user, out var flats, out _) > 0)
			{
				//如果所属的角色中包括系统内置管理员，则该用户自然属于任何角色
				return flats.Any(role =>
					string.Equals(role.Name, Administrators, StringComparison.OrdinalIgnoreCase) ||
					roleNames.Contains(role.Name)
				);
			}

			return false;
		}

		public static int GetAncestors(IDataAccess dataAccess, IUserIdentity user, out ISet<IRole> flats, out IList<IEnumerable<IRole>> hierarchies)
		{
			flats = null;
			hierarchies = null;

			//如果指定编号的用户不存在，则退出
			if(user == null)
				return 0;

			//如果指定编号的用户是内置的“Administrator”账号，则直接返回（因为内置管理员只隶属于内置的“Administrators”角色，而不能属于其他角色）
			if(string.Equals(user.Name, Administrator, StringComparison.OrdinalIgnoreCase))
			{
				//获取当前用户同命名空间下的“Administrators”内置角色
				flats = new HashSet<IRole>(dataAccess.Select<IRole>(Condition.Equal(nameof(IRole.Name), Administrators) & Condition.Equal(nameof(IRole.Namespace), user.Namespace)));

				if(flats.Count > 0)
				{
					hierarchies = new List<IEnumerable<IRole>>();
					hierarchies.Add(flats);
				}

				return flats.Count;
			}

			return GetAncestors(dataAccess, user.Namespace, user.UserId, MemberType.User, out flats, out hierarchies);
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

			flats = new HashSet<IRole>(RoleComparer.Instance);
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
				                 .Except(flats, RoleComparer.Instance).ToList();
			}

			return flats.Count;
		}

		public static IEnumerable<AuthorizationToken> GetAuthorizedTokens(IDataAccess dataAccess, string @namespace, uint memberId, MemberType memberType)
		{
			GetAncestors(dataAccess, @namespace, memberId, memberType, out var flats, out var hierarchies);
			return GetAuthorizedTokens(dataAccess, flats, hierarchies, memberId, memberType);
		}

		public static IEnumerable<AuthorizationToken> GetAuthorizedTokens(IDataAccess dataAccess, ISet<IRole> flats, IList<IEnumerable<IRole>> hierarchies, uint memberId, MemberType memberType)
		{
			var conditions = Condition.Equal("MemberId", memberId) & Condition.Equal("MemberType", memberType);

			//获取指定成员的所有上级角色集和上级角色的层级列表
			if(flats != null && flats.Count > 0)
			{
				//如果指定成员有上级角色，则进行权限定义的查询条件还需要加上所有上级角色
				conditions = ConditionCollection.Or(
					conditions,
					Condition.In("MemberId", flats.Select(p => p.RoleId)) & Condition.Equal("MemberType", MemberType.Role)
				);
			}

			//获取指定条件的所有权限定义（注：禁止分页查询，并即时加载到数组中）
			var permissions = dataAccess.Select<Permission>(conditions, Paging.Disabled).ToArray();

			//获取指定条件的所有权限过滤定义（注：禁止分页查询，并即时加载到数组中）
			var permissionFilters = dataAccess.Select<PermissionFilter>(conditions, Paging.Disabled).ToArray();

			var states = new HashSet<AuthorizationState>();
			IEnumerable<Permission> prepares;
			IEnumerable<AuthorizationState> grants, denies;

			//如果上级角色层级列表不为空则进行分层过滤
			if(hierarchies != null && hierarchies.Count > 0)
			{
				//从最顶层（即距离指定成员最远的层）开始到最底层（集距离指定成员最近的层）
				for(int i = hierarchies.Count - 1; i >= 0; i--)
				{
					//定义权限集过滤条件：当前层级的角色集的所有权限定义
					prepares = permissions.Where(p => hierarchies[i].Any(role => role.RoleId == p.MemberId) && p.MemberType == MemberType.Role);

					grants = prepares.Where(p => p.Granted).Select(p => new AuthorizationState(p.SchemaId, p.ActionId)).ToArray();
					denies = prepares.Where(p => !p.Granted).Select(p => new AuthorizationState(p.SchemaId, p.ActionId)).ToArray();

					states.UnionWith(grants);  //合并授予的权限定义
					states.ExceptWith(denies); //排除拒绝的权限定义

					//更新授权集中的相关目标的过滤文本
					SetPermissionFilters(states, permissionFilters.Where(p => hierarchies[i].Any(role => role.RoleId == p.MemberId) && p.MemberType == MemberType.Role));
				}
			}

			//查找权限定义中当前成员的设置项
			prepares = permissions.Where(p => p.MemberId == memberId && p.MemberType == memberType);

			grants = prepares.Where(p => p.Granted).Select(p => new AuthorizationState(p.SchemaId, p.ActionId)).ToArray();
			denies = prepares.Where(p => !p.Granted).Select(p => new AuthorizationState(p.SchemaId, p.ActionId)).ToArray();

			states.UnionWith(grants);  //合并授予的权限定义
			states.ExceptWith(denies); //排除拒绝的权限定义

			//更新授权集中的相关目标的过滤文本
			SetPermissionFilters(states, permissionFilters.Where(p => p.MemberId == memberId && p.MemberType == memberType));

			foreach(var group in states.GroupBy(p => p.SchemaId))
			{
				yield return new AuthorizationToken(group.Key, group.Select(p => new AuthorizationToken.ActionToken(p.ActionId, p.Filter)));
			}
		}

		private static void SetPermissionFilters(IEnumerable<AuthorizationState> states, IEnumerable<PermissionFilter> filters)
		{
			var groups = filters.GroupBy(p => new AuthorizationState(p.SchemaId, p.ActionId));

			foreach(var group in groups)
			{
				var state = states.FirstOrDefault(p => p.Equals(group.Key));

				if(state != null)
				{
					if(string.IsNullOrWhiteSpace(state.Filter))
						state.Filter = string.Join("; ", group.Select(p => p.Filter));
					else
						state.Filter += " | " + string.Join("; ", group.Select(p => p.Filter));
				}
			}
		}
		#endregion

		#region 内部方法
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
		#endregion

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
			return string.Equals(name, Administrator, StringComparison.OrdinalIgnoreCase) ||
			       string.Equals(name, Administrators, StringComparison.OrdinalIgnoreCase);
		}
		#endregion

		#region 嵌套子类
		private class RoleComparer : IEqualityComparer<IRole>
		{
			#region 单例字段
			public static readonly RoleComparer Instance = new RoleComparer();
			#endregion

			#region 私有构造
			private RoleComparer()
			{
			}
			#endregion

			#region 公共方法
			public bool Equals(IRole x, IRole y)
			{
				if(x == null && y == null)
					return true;

				if(x == null || y == null)
					return false;

				return x.RoleId == y.RoleId;
			}

			public int GetHashCode(IRole role)
			{
				return role == null ? 0 : (int)role.RoleId;
			}
			#endregion
		}

		private class AuthorizationState : IEquatable<AuthorizationState>
		{
			#region 公共字段
			private readonly string KEY;

			public readonly string SchemaId;
			public readonly string ActionId;
			public string Filter;
			#endregion

			#region 构造函数
			public AuthorizationState(string schemaId, string actionId, string filter = null)
			{
				if(string.IsNullOrEmpty(schemaId))
					throw new ArgumentNullException(nameof(schemaId));
				if(string.IsNullOrEmpty(actionId))
					throw new ArgumentNullException(nameof(actionId));

				this.KEY = schemaId.ToUpperInvariant() + ":" + actionId.ToUpperInvariant();
				this.SchemaId = schemaId;
				this.ActionId = actionId;
				this.Filter = filter;
			}
			#endregion

			#region 重写方法
			public bool Equals(AuthorizationState other)
			{
				return string.Equals(this.KEY, other.KEY, StringComparison.Ordinal);
			}

			public override bool Equals(object obj)
			{
				if(obj == null || obj.GetType() != typeof(AuthorizationState))
					return false;

				return this.Equals((AuthorizationState)obj);
			}

			public override int GetHashCode()
			{
				return this.KEY.GetHashCode();
			}

			public override string ToString()
			{
				if(string.IsNullOrEmpty(this.Filter))
					return this.SchemaId + ":" + this.ActionId;
				else
					return this.SchemaId + ":" + this.ActionId + "(" + this.Filter + ")";
			}
			#endregion
		}
		#endregion
	}
}
