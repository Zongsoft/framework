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
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration.Options;

namespace Zongsoft.Security.Membership
{
	[Service(typeof(IAuthenticator), typeof(ICredentialProvider))]
	public partial class Authenticator : AuthenticatorBase
	{
		#region 公共属性
		[Options("Security/Membership/Authentication")]
		public Configuration.AuthenticationOptions Options { get; set; }
		#endregion

		#region 重写方法
		protected override void OnAuthenticating(string @namespace, string identity, string scenario, IDictionary<string, object> parameters)
		{
			//设置凭证有效期的配置策略
			if(parameters != null)
				parameters["Authentication:Options"] = this.Options;

			//调用基类同名方法
			base.OnAuthenticating(@namespace, identity, scenario, parameters);
		}
		#endregion
	}
}
