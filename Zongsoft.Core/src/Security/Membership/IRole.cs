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
	/// 表示权限系统角色的实体接口。
	/// </summary>
	public interface IRole
	{
		#region 常量定义
		/// <summary>系统管理员角色名。</summary>
		public const string Administrators = nameof(Administrators);

		/// <summary>安全管理员角色名。</summary>
		public const string Security = nameof(Security);
		#endregion

		/// <summary>
		/// 获取或设置角色编号。
		/// </summary>
		uint RoleId { get; set; }

		/// <summary>
		/// 获取或设置角色名称。
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// 获取或设置角色全称。
		/// </summary>
		string FullName { get; set; }

		/// <summary>
		/// 获取或设置角色所属的命名空间。
		/// </summary>
		string Namespace { get; set; }

		/// <summary>
		/// 获取或设置角色的描述信息。
		/// </summary>
		string Description { get; set; }

		/// <summary>
		/// 获取或设置角色成员子集。
		/// </summary>
		IEnumerable<Member> Children { get; set; }
	}
}
