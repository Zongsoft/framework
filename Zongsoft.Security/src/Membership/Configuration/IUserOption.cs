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
using System.ComponentModel;

namespace Zongsoft.Security.Membership.Configuration
{
	/// <summary>
	/// 表示用户管理配置的接口。
	/// </summary>
	public interface IUserOption
	{
		/// <summary>
		/// 获取或设置密码的最小长度，零表示不限制。
		/// </summary>
		int PasswordLength
		{
			get; set;
		}

		/// <summary>
		/// 获取或设置密码的强度。
		/// </summary>
		PasswordStrength PasswordStrength
		{
			get; set;
		}

		/// <summary>
		/// 获取或设置用户信息的有效性校验项。
		/// </summary>
		IdentityVerification Verification
		{
			get; set;
		}
	}
}
