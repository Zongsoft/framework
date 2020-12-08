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
	/// <summary>
	/// 表示安全管理数据表映射的类。
	/// </summary>
	public class Mapping
	{
		#region 常量定义
		public const string Security = nameof(Security);
		#endregion

		#region 单例字段
		/// <summary>获取唯一的安全管理数据表实体映射对象。</summary>
		public static readonly Mapping Instance = new Mapping()
		{
			User = $"{Security}.{nameof(User)}",
			Role = $"{Security}.{nameof(Role)}",
			Member = $"{Security}.{nameof(Member)}",
			Permission = $"{Security}.{nameof(Permission)}",
			PermissionFilter = $"{Security}.{nameof(PermissionFilter)}",
		};
		#endregion

		#region 私有构造
		private Mapping() { }
		#endregion

		#region 公共树型
		/// <summary>获取或设置用户实体的映射名称。</summary>
		public string User { get; set; }

		/// <summary>获取或设置角色实体的映射名称。</summary>
		public string Role { get; set; }

		/// <summary>获取或设置成员实体的映射名称。</summary>
		public string Member { get; set; }

		/// <summary>获取或设置权限实体的映射名称。</summary>
		public string Permission { get; set; }

		/// <summary>获取或设置权限过滤实体的映射名称。</summary>
		public string PermissionFilter { get; set; }
		#endregion
	}
}
