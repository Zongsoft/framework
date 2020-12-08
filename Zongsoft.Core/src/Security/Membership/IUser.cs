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
	/// 表示权限系统用户实体的接口。
	/// </summary>
	public interface IUser : IUserIdentity
	{
		#region 常量定义
		/// <summary>系统管理员用户名。</summary>
		public const string Administrator = nameof(Administrator);
		#endregion

		/// <summary>
		/// 获取或设置用户的邮箱标识。
		/// </summary>
		string Email { get; set; }

		/// <summary>
		/// 获取或设置用户的电话标识。
		/// </summary>
		string Phone { get; set; }

		/// <summary>
		/// 获取或设置用户状态。
		/// </summary>
		UserStatus Status { get; set; }

		/// <summary>
		/// 获取或设置用户状态变更时间。
		/// </summary>
		DateTime? StatusTimestamp { get; set; }

		/// <summary>
		/// 获取或设置创建时间。
		/// </summary>
		DateTime Creation { get; set; }

		/// <summary>
		/// 获取或设置最后修改时间。
		/// </summary>
		DateTime? Modification { get; set; }
	}
}
