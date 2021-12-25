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

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Security.Membership
{
	/// <summary>
	/// 表示提供身份验证功能的接口。
	/// </summary>
	public interface IAuthentication
	{
		#region 事件定义
		/// <summary>表示验证完成的事件。</summary>
		event EventHandler<AuthenticatedEventArgs> Authenticated;

		/// <summary>表示验证开始的事件。</summary>
		event EventHandler<AuthenticatingEventArgs> Authenticating;
		#endregion

		#region 属性定义
		/// <summary>获取验证的标识。</summary>
		string Scheme { get; }
		#endregion

		#region 方法定义
		/// <summary>
		/// 身份验证。
		/// </summary>
		/// <param name="scheme">指定的身份验证方案。</param>
		/// <param name="key">待验证的凭证标识。</param>
		/// <param name="data">待验证的凭证数据。</param>
		/// <param name="scenario">身份验证的场景。</param>
		/// <param name="parameters">身份验证的扩展参数。</param>
		/// <returns>返回的凭证主体。</returns>
		OperationResult<CredentialPrincipal> Authenticate(string scheme, string key, object data, string scenario, IDictionary<string, object> parameters);
		#endregion
	}
}
