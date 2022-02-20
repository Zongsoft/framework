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
using System.Security.Claims;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Common;

namespace Zongsoft.Security.Membership
{
	public static class MembershipUtility
	{
		#region 公共方法
		public static bool InRoles(IDataAccess dataAccess, IRole role, params string[] roleNames)
		{
			if(role == null || role.Name == null || roleNames == null || roleNames.Length < 1)
				return false;

			//如果指定的角色对应的是系统内置管理员（即 Administrators），那么它拥有对任何角色的隶属判断
			if(string.Equals(role.Name, IRole.Administrators, StringComparison.OrdinalIgnoreCase))
				return true;

			//处理非系统内置管理员角色
			if(GetAncestors(dataAccess, role, out ISet<IRole> flats, out _) > 0)
			{
				//如果所属的角色中包括系统内置管理员，则该用户自然属于任何角色
				return flats.Any(role =>
					string.Equals(role.Name, IRole.Administrators, StringComparison.OrdinalIgnoreCase) ||
					roleNames.Contains(role.Name)
				);
			}

			return false;
		}

		public static bool InRoles(IDataAccess dataAccess, IUser user, params string[] roleNames)
		{
			if(user == null || user.Name == null || roleNames == null || roleNames.Length < 1)
				return false;

			//如果指定的用户对应的是系统内置管理员（即 Administrator），那么它拥有对任何角色的隶属判断
			if(string.Equals(user.Name, IUser.Administrator, StringComparison.OrdinalIgnoreCase))
				return true;

			//处理非系统内置管理员账号
			if(GetAncestors(dataAccess, user, out ISet<IRole> flats, out _) > 0)
			{
				//如果所属的角色中包括系统内置管理员，则该用户自然属于任何角色
				return flats.Any(role =>
					string.Equals(role.Name, IRole.Administrators, StringComparison.OrdinalIgnoreCase) ||
					roleNames.Contains(role.Name)
				);
			}

			return false;
		}

		/// <summary>
		/// 获取指定角色的所有上级角色集。
		/// </summary>
		/// <param name="dataAccess">数据访问服务。</param>
		/// <param name="role">指定的角色对象。</param>
		/// <param name="flats">输出参数，表示所隶属的所有上级角色集，该集已经去除重复。</param>
		/// <param name="hierarchies">输出参数，表示所隶属的所有上级角色的层级列表，该列表包含的所有角色已经去除重复。</param>
		/// <returns>返回指定成员隶属的所有上级角色去重后的数量。</returns>
		public static int GetAncestors<TRole>(IDataAccess dataAccess, IRole role, out ISet<TRole> flats, out IList<IEnumerable<TRole>> hierarchies) where TRole : IRole
		{
			if(dataAccess == null)
				throw new ArgumentNullException(nameof(dataAccess));

			if(role == null)
				throw new ArgumentNullException(nameof(role));

			//获取角色表的命名空间字段名
			var field = Mapping.Instance.Namespace.GetField(Mapping.Instance.Role);

			return GetAncestors(
				dataAccess,
				role.RoleId,
				MemberType.Role,
				Condition.Equal("Role." + field, Mapping.Instance.Namespace.GetNamespace(role)),
				out flats, out hierarchies);
		}

		/// <summary>
		/// 获取指定用户的所有上级角色集。
		/// </summary>
		/// <param name="dataAccess">数据访问服务。</param>
		/// <param name="user">指定的用户对象。</param>
		/// <param name="flats">输出参数，表示所隶属的所有上级角色集，该集已经去除重复。</param>
		/// <param name="hierarchies">输出参数，表示所隶属的所有上级角色的层级列表，该列表包含的所有角色已经去除重复。</param>
		/// <returns>返回指定成员隶属的所有上级角色去重后的数量。</returns>
		public static int GetAncestors<TRole>(IDataAccess dataAccess, IUser user, out ISet<TRole> flats, out IList<IEnumerable<TRole>> hierarchies) where TRole : IRole
		{
			if(dataAccess == null)
				throw new ArgumentNullException(nameof(dataAccess));

			if(user == null)
				throw new ArgumentNullException(nameof(user));

			//获取角色表的命名空间字段名
			var field = Mapping.Instance.Namespace.GetField(Mapping.Instance.Role);

			return GetAncestors(
				dataAccess,
				user.UserId,
				MemberType.User,
				Condition.Equal("Role." + field, Mapping.Instance.Namespace.GetNamespace(user)),
				out flats, out hierarchies);
		}

		/// <summary>
		/// 获取指定用户的所有上级角色集。
		/// </summary>
		/// <param name="dataAccess">数据访问服务。</param>
		/// <param name="identity">指定的身份标识。</param>
		/// <param name="flats">输出参数，表示所隶属的所有上级角色集，该集已经去除重复。</param>
		/// <param name="hierarchies">输出参数，表示所隶属的所有上级角色的层级列表，该列表包含的所有角色已经去除重复。</param>
		/// <returns>返回指定成员隶属的所有上级角色去重后的数量。</returns>
		public static int GetAncestors<TRole>(IDataAccess dataAccess, ClaimsIdentity identity, out ISet<TRole> flats, out IList<IEnumerable<TRole>> hierarchies) where TRole : IRole
		{
			if(dataAccess == null)
				throw new ArgumentNullException(nameof(dataAccess));

			if(identity == null)
				throw new ArgumentNullException(nameof(identity));

			//获取角色表的命名空间字段名
			var field = Mapping.Instance.Namespace.GetField(Mapping.Instance.Role);

			return GetAncestors(
				dataAccess,
				identity.GetIdentifier<uint>(),
				MemberType.User,
				Condition.Equal("Role." + field, Mapping.Instance.Namespace.GetNamespace(identity)),
				out flats, out hierarchies);
		}

		/// <summary>
		/// 获取指定用户或角色的上级角色集。
		/// </summary>
		/// <param name="dataAccess">数据访问服务。</param>
		/// <param name="memberId">成员编号（用户或角色）。</param>
		/// <param name="memberType">成员类型，表示<paramref name="memberId"/>对应的成员类型。</param>
		/// <param name="filter"></param>
		/// <param name="flats">输出参数，表示所隶属的所有上级角色集，该集已经去除重复。</param>
		/// <param name="hierarchies">输出参数，表示所隶属的所有上级角色的层级列表，该列表包含的所有角色已经去除重复。</param>
		/// <returns>返回指定成员隶属的所有上级角色去重后的数量。</returns>
		private static int GetAncestors<TRole>(IDataAccess dataAccess, uint memberId, MemberType memberType, Condition filter, out ISet<TRole> flats, out IList<IEnumerable<TRole>> hierarchies) where TRole : IRole
		{
			if(dataAccess == null)
				throw new ArgumentNullException(nameof(dataAccess));

			flats = null;
			hierarchies = null;

			//获取指定命名空间下的所有成员及其关联的角色对象（注：即时加载到内存中）
			var members = dataAccess.Select<Member<TRole, IUser>>(
				filter, "*, Role{*}")
				.Where(m => m.Role != null)
				.ToArray();

			flats = new HashSet<TRole>(RoleComparer<TRole>.Instance);
			hierarchies = new List<IEnumerable<TRole>>();

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
								 .Except(flats, RoleComparer<TRole>.Instance).ToList();
			}

			return flats.Count;
		}

		public static IEnumerable<TRole> GetAncestors<TRole>(IDataAccess dataAccess, IRole role) where TRole : IRole
		{
			if(dataAccess == null)
				throw new ArgumentNullException(nameof(dataAccess));

			if(role == null)
				throw new ArgumentNullException(nameof(role));

			//获取角色表的命名空间字段名
			var field = Mapping.Instance.Namespace.GetField(Mapping.Instance.Role);

			return GetAncestors<TRole>(
				dataAccess,
				role.RoleId,
				MemberType.Role,
				Condition.Equal("Role." + field, Mapping.Instance.Namespace.GetNamespace(role)));
		}

		public static IEnumerable<TRole> GetAncestors<TRole>(IDataAccess dataAccess, IUser user) where TRole : IRole
		{
			if(dataAccess == null)
				throw new ArgumentNullException(nameof(dataAccess));

			if(user == null)
				throw new ArgumentNullException(nameof(user));

			//获取角色表的命名空间字段名
			var field = Mapping.Instance.Namespace.GetField(Mapping.Instance.Role);

			return GetAncestors<TRole>(
				dataAccess,
				user.UserId,
				MemberType.User,
				Condition.Equal("Role." + field, Mapping.Instance.Namespace.GetNamespace(user)));
		}

		public static IEnumerable<TRole> GetAncestors<TRole>(IDataAccess dataAccess, ClaimsIdentity identity) where TRole : IRole
		{
			if(dataAccess == null)
				throw new ArgumentNullException(nameof(dataAccess));

			if(identity == null)
				throw new ArgumentNullException(nameof(identity));

			//获取角色表的命名空间字段名
			var field = Mapping.Instance.Namespace.GetField(Mapping.Instance.Role);

			return GetAncestors<TRole>(
				dataAccess,
				identity.GetIdentifier<uint>(),
				MemberType.User,
				Condition.Equal("Role." + field, Mapping.Instance.Namespace.GetNamespace(identity)));
		}

		private static IEnumerable<TRole> GetAncestors<TRole>(IDataAccess dataAccess, uint memberId, MemberType memberType, Condition filter) where TRole : IRole
		{
			if(dataAccess == null)
				throw new ArgumentNullException(nameof(dataAccess));

			var roles = new HashSet<TRole>(dataAccess.Select<Member<TRole, IUser>>(
				Mapping.Instance.Member,
				Condition.Equal(nameof(Member.MemberId), memberId) &
				Condition.Equal(nameof(Member.MemberType), memberType) &
				filter,
				"Role{*}"
			)
			.Where(m => m.Role != null)
			.Select(m => m.Role), RoleComparer<TRole>.Instance);

			if(roles.Count > 0)
			{
				var intersection = roles.Select(role => role.RoleId).ToArray();

				while(intersection.Any())
				{
					var parents = dataAccess.Select<Member<TRole, IUser>>(
						Mapping.Instance.Member,
						Condition.In(nameof(Member.MemberId), intersection) &
						Condition.Equal(nameof(Member.MemberType), MemberType.Role) &
						filter,
						"Role{*}",
						DataSelectOptions.Distinct()
					)
					.Where(m => m.Role != null)
					.Select(m => m.Role)
					.ToArray();

					intersection = parents.Except(roles, RoleComparer<TRole>.Instance).Select(p => p.RoleId).ToArray();
					roles.UnionWith(parents);
				}
			}

			return roles;
		}

		public static IEnumerable<AuthorizationToken> GetAuthorizes(IDataAccess dataAccess, IRole role)
		{
			if(GetAncestors<IRole>(dataAccess, role, out var flats, out var hierarchies) > 0)
				return GetAuthorizedTokens(dataAccess, flats, hierarchies, role.RoleId, MemberType.Role);
			else
				return Array.Empty<AuthorizationToken>();
		}

		public static IEnumerable<AuthorizationToken> GetAuthorizes(IDataAccess dataAccess, IUser user)
		{
			GetAncestors<IRole>(dataAccess, user, out var flats, out var hierarchies);
			return GetAuthorizedTokens(dataAccess, flats, hierarchies, user.UserId, MemberType.User);
		}

		public static IEnumerable<AuthorizationToken> GetAuthorizes(IDataAccess dataAccess, ClaimsIdentity identity)
		{
			GetAncestors<IRole>(dataAccess, identity, out var flats, out var hierarchies);
			return GetAuthorizedTokens(dataAccess, flats, hierarchies, identity.GetIdentifier<uint>(), MemberType.User);
		}

		private static IEnumerable<AuthorizationToken> GetAuthorizedTokens(IDataAccess dataAccess, ISet<IRole> flats, IList<IEnumerable<IRole>> hierarchies, uint memberId, MemberType memberType)
		{
			var criteria = Condition.Equal(nameof(Permission.MemberId), memberId) &
			               Condition.Equal(nameof(Permission.MemberType), memberType);

			//获取指定成员的所有上级角色集和上级角色的层级列表
			if(flats != null && flats.Count > 0)
			{
				//如果指定成员有上级角色，则进行权限定义的查询条件还需要加上所有上级角色
				criteria = criteria.Or(
					Condition.In(nameof(Permission.MemberId), flats.Select(p => p.RoleId)) &
					Condition.Equal(nameof(Permission.MemberType), MemberType.Role)
				);
			}

			//获取指定条件的所有权限定义（注：禁止分页查询，并即时加载到数组中）
			var permissions = dataAccess.Select<Permission>(Mapping.Instance.Permission, criteria, Paging.Disabled).ToArray();

			//获取指定条件的所有权限过滤定义（注：禁止分页查询，并即时加载到数组中）
			var permissionFilters = dataAccess.Select<PermissionFilter>(Mapping.Instance.PermissionFilter, criteria, Paging.Disabled).ToArray();

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
		#endregion

		#region 内部方法
		internal static Condition GetIdentityCondition(string identity)
		{
			return GetIdentityCondition(identity, out _);
		}

		internal static Condition GetIdentityCondition(string identity, out UserIdentityType identityType)
		{
			if(string.IsNullOrWhiteSpace(identity))
				throw new ArgumentNullException(nameof(identity));

			if(identity.Contains("@"))
			{
				identityType = UserIdentityType.Email;
				return Condition.Equal(nameof(IUser.Email), identity);
			}

			if(identity.IsDigits(out var digits))
			{
				identityType = UserIdentityType.Phone;
				return Condition.Equal(nameof(IUser.Phone), digits);
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

			if(identity.IsDigits())
				return UserIdentityType.Phone;

			return UserIdentityType.Name;
		}
		#endregion

		#region 私有方法
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

		#region 嵌套子类
		private class RoleComparer<TRole> : IEqualityComparer<TRole> where TRole : IRole
		{
			public static readonly RoleComparer<TRole> Instance = new RoleComparer<TRole>();

			public bool Equals(TRole x, TRole y)
			{
				if(x == null)
					return y == null;
				else
					return y == null ? false : x.RoleId == y.RoleId;
			}

			public int GetHashCode(TRole role)
			{
				return (int)role.RoleId;
			}
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
