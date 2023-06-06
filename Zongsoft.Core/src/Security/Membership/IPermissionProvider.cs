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
using System.Collections.Generic;

namespace Zongsoft.Security.Membership
{
	/// <summary>
	/// 提供关于权限管理的接口。
	/// </summary>
	public interface IPermissionProvider
	{
		/// <summary>获取指定用户或角色的权限集。</summary>
		/// <param name="memberId">指定要获取的权限集的成员编号(用户或角色)。</param>
		/// <param name="memberType">指定要获取的权限集的成员类型。</param>
		/// <param name="target">指定的要获取的权限集的目标标识，如果为空(null)或空字符串则忽略该参数。</param>
		/// <returns>返回指定用户或角色的权限集。注意：该结果集不包含指定成员所属的上级角色的权限设置。</returns>
		IEnumerable<PermissionModel> GetPermissions(uint memberId, MemberType memberType, string target = null);

		/// <summary>设置指定用户或角色的权限集。</summary>
		/// <param name="memberId">指定的要设置的权限集的成员编号(用户或角色)。</param>
		/// <param name="memberType">指定的要设置的权限集的成员类型。</param>
		/// <param name="permissions">要设置更新的权限集，如果为空则表示清空指定成员的权限集。</param>
		/// <param name="shouldResetting">指示是否以重置的方式更新权限集。如果为真表示写入之前先清空指定成员下的所有权限设置；否则如果指定的权限项存在则更新它，不存在则新增。</param>
		/// <returns>返回设置成功的记录数。</returns>
		int SetPermissions(uint memberId, MemberType memberType, IEnumerable<PermissionModel> permissions, bool shouldResetting = false);

		/// <summary>设置指定用户或角色的权限集。</summary>
		/// <param name="memberId">指定的要设置的权限集的成员编号(用户或角色)。</param>
		/// <param name="memberType">指定的要设置的权限集的成员类型。</param>
		/// <param name="target">指定的要设置的权限集的目标标识，如果为空(null)或空字符串则忽略该参数。</param>
		/// <param name="permissions">要设置更新的权限集，如果为空则表示清空指定成员的权限集。</param>
		/// <param name="shouldResetting">指示是否以重置的方式更新权限集。如果为真表示写入之前先清空指定成员下的所有权限设置；否则如果指定的权限项存在则更新它，不存在则新增。</param>
		/// <returns>返回设置成功的记录数。</returns>
		int SetPermissions(uint memberId, MemberType memberType, string target, IEnumerable<PermissionModel> permissions, bool shouldResetting = false);

		/// <summary>移除单个或多个权限设置项。</summary>
		/// <param name="memberId">指定的要移除的权限成员编号(用户或角色)。</param>
		/// <param name="memberType">指定的要移除的权限成员类型。</param>
		/// <param name="target">指定的要移除的权限目标标识，为空表示忽略该条件。</param>
		/// <param name="action">指定的要移除的权限操作标识，为空表示忽略该条件。</param>
		/// <returns>如果移除成功则返回受影响的记录数，否则返回零。</returns>
		int RemovePermissions(uint memberId, MemberType memberType, string target = null, string action = null);

		/// <summary>获取指定用户或角色的权限过滤集。</summary>
		/// <param name="memberId">指定要获取的权限过滤集的成员编号(用户或角色)。</param>
		/// <param name="memberType">指定要获取的权限过滤集的成员类型。</param>
		/// <param name="target">指定的要获取的权限过滤集的目标标识，如果为空(null)或空字符串则忽略该参数。</param>
		/// <returns>返回指定用户或角色的权限过滤集。注意：该结果集不包含指定成员所属的上级角色的权限设置。</returns>
		IEnumerable<PermissionFilterModel> GetPermissionFilters(uint memberId, MemberType memberType, string target = null);

		/// <summary>设置指定用户或角色的权限过滤集。</summary>
		/// <param name="memberId">指定的要设置的权限过滤集的成员编号(用户或角色)。</param>
		/// <param name="memberType">指定的要设置的权限过滤集的成员类型。</param>
		/// <param name="permissionFilters">要设置更新的权限过滤集，如果为空则表示清空指定成员的权限过滤集。</param>
		/// <param name="shouldResetting">指示是否以重置的方式更新权限过滤集。如果为真表示写入之前先清空指定成员下的所有权限过滤设置；否则如果指定的权限过滤项存在则更新它，不存在则新增。</param>
		/// <returns>返回设置成功的记录数。</returns>
		int SetPermissionFilters(uint memberId, MemberType memberType, IEnumerable<PermissionFilterModel> permissionFilters, bool shouldResetting = false);

		/// <summary>设置指定用户或角色的权限过滤集。</summary>
		/// <param name="memberId">指定的要设置的权限过滤集的成员编号(用户或角色)。</param>
		/// <param name="memberType">指定的要设置的权限过滤集的成员类型。</param>
		/// <param name="target">指定的要设置的权限过滤集的目标标识，如果为空(null)或空字符串则忽略该参数。</param>
		/// <param name="permissionFilters">要设置更新的权限过滤集，如果为空则表示清空指定成员的权限过滤集。</param>
		/// <param name="shouldResetting">指示是否以重置的方式更新权限过滤集。如果为真表示写入之前先清空指定成员下的所有权限过滤设置；否则如果指定的权限过滤项存在则更新它，不存在则新增。</param>
		/// <returns>返回设置成功的记录数。</returns>
		int SetPermissionFilters(uint memberId, MemberType memberType, string target, IEnumerable<PermissionFilterModel> permissionFilters, bool shouldResetting = false);

		/// <summary>移除单个权限过滤设置项。</summary>
		/// <param name="memberId">指定的要移除的权限成员编号(用户或角色)。</param>
		/// <param name="memberType">指定的要移除的权限成员类型。</param>
		/// <param name="target">指定的要移除的权限目标标识，为空表示忽略该条件。</param>
		/// <param name="action">指定的要移除的权限操作标识，为空表示忽略该条件。</param>
		/// <returns>如果移除成功则返回受影响的记录数，否则返回零。</returns>
		int RemovePermissionFilters(uint memberId, MemberType memberType, string target = null, string action = null);
	}
}
