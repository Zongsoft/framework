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
using System.Collections.Generic;

namespace Zongsoft.Security.Membership
{
	/// <summary>
	/// 提供关于角色和角色成员管理的接口。
	/// </summary>
	public interface IRoleProvider<TRole> where TRole : IRoleModel
	{
		#region 事件定义
		/// <summary>表示角色信息发生更改之后的事件。</summary>
		event EventHandler<ChangedEventArgs> Changed;
		#endregion

		#region 方法定义
		/// <summary>确定指定编号的角色是否存在。</summary>
		/// <param name="roleId">指定要查找的角色编号。</param>
		/// <returns>如果指定编号的角色是存在的则返回真(True)，否则返回假(False)。</returns>
		bool Exists(uint roleId);

		/// <summary>确定指定的角色名在指定的命名空间内是否已经存在。</summary>
		/// <param name="name">要确定的角色名。</param>
		/// <param name="namespace">要确定的角色所属的命名空间，如果为空(null)或空字符串("")则表示当前用户所在命名空间。</param>
		/// <returns>如果指定名称的角色在命名空间内已经存在则返回真(True)，否则返回假(False)。</returns>
		bool Exists(string name, string @namespace = null);

		/// <summary>获取指定编号对应的角色对象。</summary>
		/// <param name="roleId">要查找的角色编号。</param>
		/// <returns>返回由<paramref name="roleId"/>参数指定的角色对象，如果没有找到指定编号的角色则返回空。</returns>
		TRole GetRole(uint roleId);

		/// <summary>获取指定名称对应的角色对象。</summary>
		/// <param name="name">要查找的角色名称。</param>
		/// <param name="namespace">要查找的角色所属的命名空间，如果为空(null)或空字符串("")则表示当前用户所在命名空间。</param>
		/// <returns>返回找到的角色对象；如果在指定的命名空间内没有找到指定名称的角色则返回空(null)。</returns>
		/// <exception cref="System.ArgumentNullException">当<paramref name="name"/>参数为空(null)或者全空格字符。</exception>
		TRole GetRole(string name, string @namespace = null);

		/// <summary>获取指定命名空间中的角色集。</summary>
		/// <param name="namespace">要获取的角色集所属的命名空间。如果为星号(*)则忽略命名空间即系统中的所有角色；如果为空(null)或空字符串("")则查找当前用户所在命名空间的角色集。</param>
		/// <param name="paging">查询的分页设置，默认为第一页。</param>
		/// <returns>返回当前命名空间中的所有角色对象集。</returns>
		IEnumerable<TRole> GetRoles(string @namespace, Zongsoft.Data.Paging paging = null);

		/// <summary>查找指定关键字的角色。</summary>
		/// <param name="keyword">要查找的关键字。</param>
		/// <param name="paging">查询的分页设置，默认为第一页。</param>
		/// <returns>返回找到的角色对象集。</returns>
		IEnumerable<TRole> Find(string keyword, Zongsoft.Data.Paging paging = null);

		/// <summary>设置指定编号的角色所属命名空间。</summary>
		/// <param name="roleId">要设置的角色编号。</param>
		/// <param name="namespace">要设置的命名空间。</param>
		/// <returns>如果设置成功则返回真(True)，否则返回假。</returns>
		bool SetNamespace(uint roleId, string @namespace);

		/// <summary>更新指定命名空间下所有角色到新的命名空间。</summary>
		/// <param name="oldNamespace">指定的旧命名空间。</param>
		/// <param name="newNamespace">指定的新命名空间。</param>
		/// <returns>返回更新成功的角色数。</returns>
		int SetNamespaces(string oldNamespace, string newNamespace);

		/// <summary>设置指定编号的角色名称。</summary>
		/// <param name="roleId">要设置的角色编号。</param>
		/// <param name="name">要设置的角色名称。</param>
		/// <returns>如果设置成功则返回真(True)，否则返回假(False)。</returns>
		bool SetName(uint roleId, string name);

		/// <summary>设置指定编号的角色昵称。</summary>
		/// <param name="roleId">要设置的角色编号。</param>
		/// <param name="nickname">要设置的角色昵称。</param>
		/// <returns>如果设置成功则返回真(True)，否则返回假(False)。</returns>
		bool SetNickname(uint roleId, string nickname);

		/// <summary>设置指定编号的角色描述信息。</summary>
		/// <param name="roleId">要设置的角色编号。</param>
		/// <param name="description">要设置的角色描述信息。</param>
		/// <returns>如果设置成功则返回真(True)，否则返回假(False)。</returns>
		bool SetDescription(uint roleId, string description);

		/// <summary>删除指定编号集的多个角色。</summary>
		/// <param name="ids">要删除的角色编号集合。</param>
		/// <returns>如果删除成功则返回删除的数量，否则返回零。</returns>
		int Delete(params uint[] ids);

		/// <summary>创建一个角色。</summary>
		/// <param name="name">指定的新建角色的名称。</param>
		/// <param name="namespace">新建角色所属的命名空间。</param>
		/// <param name="nickname">指定的新建角色的昵称。</param>
		/// <param name="description">指定的新建角色的描述信息。</param>
		/// <returns>返回创建成功的角色对象，如果为空(null)则表示创建失败。</returns>
		TRole Create(string name, string @namespace, string nickname = null, string description = null);

		/// <summary>创建一个角色。</summary>
		/// <param name="role">要创建的角色对象。</param>
		/// <returns>如果创建成功则返回真(true)，否则返回假(false)。</returns>
		bool Create(TRole role);

		/// <summary>创建单个或者多个角色。</summary>
		/// <param name="roles">要创建的角色对象集。</param>
		/// <returns>返回创建成功的角色数量。</returns>
		int Create(IEnumerable<TRole> roles);

		/// <summary>修改指定编号的角色信息。</summary>
		/// <param name="roleId">指定要修改的角色编号。</param>
		/// <param name="role">要修改的角色对象。</param>
		/// <returns>如果修改成功则返回真(true)，否则返回假(false)。</returns>
		bool Update(uint roleId, TRole role);
		#endregion
	}
}
