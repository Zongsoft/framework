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

namespace Zongsoft.Security.Membership
{
	/// <summary>
	/// 表示验证失败原因的枚举。
	/// </summary>
	public enum AuthenticationReason
	{
		/// <summary>验证成功</summary>
		[Description("${Text.AuthenticationReason.Succeed}")]
		Succeed = 0,

		/// <summary>未知的原因</summary>
		[Description("${Text.AuthenticationReason.Unknown}")]
		Unknown = -1,
		/// <summary>禁止验证通过</summary>
		[Description("${Text.AuthenticationReason.Forbidden}")]
		Forbidden = -2,

		/// <summary>无效的身份标识</summary>
		[Description("${Text.AuthenticationReason.InvalidIdentity}")]
		InvalidIdentity = 1,
		/// <summary>无效的密码</summary>
		[Description("${Text.AuthenticationReason.InvalidPassword}")]
		InvalidPassword = 2,
		/// <summary>帐户尚未批准</summary>
		[Description("${Text.AuthenticationReason.AccountUnapproved}")]
		AccountUnapproved = 3,
		/// <summary>帐户被暂时挂起（可能是因为密码验证失败次数过多。）</summary>
		[Description("${Text.AuthenticationReason.AccountSuspended}")]
		AccountSuspended = 4,
		/// <summary>帐户已被禁用</summary>
		[Description("${Text.AuthenticationReason.AccountDisabled}")]
		AccountDisabled = 5,
	}
}
