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

namespace Zongsoft.Security
{
	/// <summary>
	/// 提供安全理由短语的定义类。
	/// </summary>
	public static class SecurityReasons
	{
		/// <summary>未知的原因</summary>
		public static readonly string Unknown = nameof(Unknown);

		/// <summary>禁止验证通过</summary>
		public static readonly string Forbidden = nameof(Forbidden);

		/// <summary>校验失败</summary>
		public static readonly string VerifyFaild = nameof(VerifyFaild);

		/// <summary>无效的身份标识</summary>
		public static readonly string InvalidIdentity = nameof(InvalidIdentity);

		/// <summary>无效的密码</summary>
		public static readonly string InvalidPassword = nameof(InvalidPassword);

		/// <summary>无效的参数。</summary>
		public static readonly string InvalidArgument = nameof(InvalidArgument);

		/// <summary>帐户尚未批准</summary>
		public static readonly string AccountUnapproved = nameof(AccountUnapproved);

		/// <summary>帐户被暂时挂起（可能因为密码验证失败次数过多）</summary>
		public static readonly string AccountSuspended = nameof(AccountSuspended);

		/// <summary>帐户已被禁用</summary>
		public static readonly string AccountDisabled = nameof(AccountDisabled);
	}
}
