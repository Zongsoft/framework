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
using System.ComponentModel;
using System.Collections.Generic;

namespace Zongsoft.Security.Membership
{
	[Zongsoft.Data.Model("Security.User")]
	public abstract class User : IUser
	{
		#region 常量定义
		/// <summary>系统管理员用户名。</summary>
		public const string Administrator = nameof(Administrator);
		#endregion

		#region 属性定义
		public abstract uint UserId { get; set; }

		public abstract string Name { get; set; }

		public abstract string FullName { get; set; }

		public abstract string Namespace { get; set; }

		public abstract string Description { get; set; }

		public abstract string Email { get; set; }

		public abstract string Phone { get; set; }

		public abstract UserStatus Status { get; set; }

		public abstract DateTime? StatusTimestamp { get; set; }

		public abstract DateTime Creation { get; set; }

		public abstract DateTime? Modification { get; set; }

		[DefaultValue(typeof(Dictionary<string, object>))]
		public abstract IDictionary<string, object> Properties { get; }
		#endregion
	}
}
