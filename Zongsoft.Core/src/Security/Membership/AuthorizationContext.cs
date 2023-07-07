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
using System.Security.Claims;

using Zongsoft.Components;

namespace Zongsoft.Security.Membership
{
	public class AuthorizationContext : ExecutorContext<AuthorizationRequest, AuthorizationResponse>
	{
		#region 构造函数
		public AuthorizationContext(AuthorizationRequest request, IEnumerable<KeyValuePair<string, object>> parameters = null) : base(null, request, parameters) { }
		public AuthorizationContext(IExecutor executor, AuthorizationRequest request, IEnumerable<KeyValuePair<string, object>> parameters = null) : base(executor, request, parameters) { }
		#endregion

		#region 公共方法
		public bool Authorized() => this.Response.IsAuthorized;
		public void Authorized(bool value) => this.Response = new AuthorizationResponse(value);
		#endregion
	}

	public readonly struct AuthorizationRequest
	{
        public AuthorizationRequest(ClaimsIdentity identity, string target, string action)
        {
			this.Identity = identity;
			this.Target = target;
			this.Action = action;
        }

        /// <summary>获取授权的用户对象。</summary>
        public ClaimsIdentity Identity { get; }

		/// <summary>获取待授权的资源标识。</summary>
		public string Target { get; }

		/// <summary>获取待授权的行为标识。</summary>
		public string Action { get; }
	}

	public struct AuthorizationResponse
	{
        public AuthorizationResponse(bool authorized)
        {
			this.IsAuthorized = authorized;
        }

        /// <summary>获取或设置是否授权通过。</summary>
        public bool IsAuthorized { get; set; }
	}
}
