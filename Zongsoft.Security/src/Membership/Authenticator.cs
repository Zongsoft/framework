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
using System.Security.Claims;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Configuration.Options;

namespace Zongsoft.Security.Membership
{
	[Service(typeof(IAuthentication))]
	public class Authenticator : AuthenticationBase
	{
		#region 构造函数
		public Authenticator(IServiceProvider serviceProvider) : base(serviceProvider) { }
		#endregion

		#region 公共属性
		[Options("Security/Membership/Authentication")]
		public Configuration.AuthenticationOptions Options { get; set; }
		#endregion

		#region 身份验证
		protected override OperationResult<ClaimsIdentity> OnAuthenticate(string scheme, string key, object data, string scenario, IDictionary<string, object> parameters)
		{
			var authenticator = this.ServiceProvider.Resolve<IAuthenticator>(scheme);

			if(authenticator == null)
				return OperationResult.Fail("InvalidAuthenticator");

			//校验身份
			var result = authenticator.Verify(key, data);

			if(result.Failed)
				return result;

			//定义凭证有效期
			var period = TimeSpan.Zero;

			//获取指定场景对应的凭证有效期
			if(this.Options != null && this.Options.Expiration.TryGet(scenario, out var option))
				period = option.Period;

			//签发身份
			var identity = authenticator.Issue(result.Value, period, parameters);

			if(identity == null)
				return OperationResult.Fail(SecurityReasons.InvalidIdentity);

			return OperationResult.Success(identity);
		}
		#endregion
	}
}
