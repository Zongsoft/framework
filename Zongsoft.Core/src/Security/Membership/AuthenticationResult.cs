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

namespace Zongsoft.Security.Membership
{
	public class AuthenticationResult
	{
		#region 构造函数
		private AuthenticationResult(ClaimsIdentity identity, IDictionary<string, object> parameters)
		{
			this.Identity = identity;
			this.Parameters = parameters;
		}

		private AuthenticationResult(AuthenticationReason reason, IDictionary<string, object> parameters)
		{
			this.Reason = reason;
			this.Parameters = parameters;
		}

		private AuthenticationResult(Exception exception, IDictionary<string, object> parameters)
		{
			this.Exception = exception;
			this.Reason = AuthenticationReason.Unknown;
			this.Parameters = parameters;
		}
		#endregion

		#region 公共属性
		public ClaimsIdentity Identity { get; }

		public ClaimsPrincipal Principal { get; set; }

		public IClaimsPrincipalTransformer Transformer { get; set; }

		public AuthenticationReason Reason { get; }

		public Exception Exception { get; }

		public IDictionary<string, object> Parameters { get; }

		public bool Succeed
		{
			get => this.Identity?.IsAuthenticated == true;
		}

		public bool Failed
		{
			get => this.Reason != AuthenticationReason.None || this.Exception != null || this.Identity == null || !this.Identity.IsAuthenticated;
		}
		#endregion

		#region 公共方法
		public object Transform()
		{
			if(this.Principal == null)
				return null;

			return (this.Transformer ?? ClaimsPrincipalTransformer.Default).Transform(this.Principal);
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			var identity = this.Identity;

			if(identity != null)
				return identity.ToString();

			return this.Exception == null ?
				this.Reason.ToString() :
				this.Reason.ToString() + ":" + this.Exception.ToString();
		}
		#endregion

		#region 静态方法
		public static AuthenticationResult Success(ClaimsIdentity identity, IDictionary<string, object> parameters = null)
		{
			return new AuthenticationResult(identity ?? throw new ArgumentNullException(nameof(identity)), parameters);
		}

		public static AuthenticationResult Fail(AuthenticationReason reason, IDictionary<string, object> parameters = null)
		{
			return new AuthenticationResult(reason, parameters);
		}

		public static AuthenticationResult Fail(Exception exception, IDictionary<string, object> parameters = null)
		{
			return new AuthenticationResult(exception ?? throw new ArgumentNullException(nameof(exception)), parameters);
		}
		#endregion
	}
}
