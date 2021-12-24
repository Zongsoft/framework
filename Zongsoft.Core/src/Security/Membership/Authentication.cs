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
	/// 提供身份验证的平台类。
	/// </summary>
	[System.Reflection.DefaultMember(nameof(Authenticator))]
	public static class Authentication
	{
		#region 静态构造
		static Authentication()
		{
			Challengers = new List<IAuthenticationChallenger>();
			Transformers = new Collections.NamedCollection<IClaimsIdentityTransformer>(transformer => transformer.Name);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置凭证主体的提供程序。
		/// </summary>
		public static ICredentialProvider Authority { get; set; }

		/// <summary>
		/// 获取或设置身份验证器。
		/// </summary>
		public static AuthenticatorBase Authenticator { get; set; }

		/// <summary>
		/// 获取一个身份验证验证器集合。
		/// </summary>
		public static ICollection<IAuthenticationChallenger> Challengers { get; }

		/// <summary>
		/// 获取一个身份转换器集合。
		/// </summary>
		public static Collections.INamedCollection<IClaimsIdentityTransformer> Transformers { get; }
		#endregion

		#region 公共方法
		public static Common.OperationResult<CredentialPrincipal> Authenticate(string scheme, string key, object data, string scenario, IDictionary<string, object> parameters)
		{
			var authenticator = Authenticator ?? throw new InvalidOperationException("Missing the required authenticator.");
			return authenticator.Authenticate(scheme, key, data, scenario, parameters);
		}
		#endregion
	}
}
