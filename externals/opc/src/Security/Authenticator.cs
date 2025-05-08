/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Opc.Ua;
using Opc.Ua.Server;

namespace Zongsoft.Externals.Opc.Security;

public class Authenticator : IAuthenticator
{
	#region 公共方法
	public ValueTask<IUserIdentity> AuthenticateAsync(UserIdentityToken token, CancellationToken cancellation = default)
	{
		if(token == null)
			return ValueTask.FromResult<IUserIdentity>(null);

		return token switch
		{
			UserNameIdentityToken user => this.OnAuthenticateAsync(user, cancellation),
			X509IdentityToken x509 => this.OnAuthenticateAsync(x509, cancellation),
			_ => ValueTask.FromResult<IUserIdentity>(null),
		};
	}
	#endregion

	#region 虚拟方法
	protected virtual ValueTask<IUserIdentity> OnAuthenticateAsync(UserNameIdentityToken token, CancellationToken cancellation = default)
	{
		return token == null || string.IsNullOrEmpty(token.UserName) ?
			ValueTask.FromResult<IUserIdentity>(null) : ValueTask.FromResult<IUserIdentity>(new UserIdentity(token));
	}

	protected virtual ValueTask<IUserIdentity> OnAuthenticateAsync(X509IdentityToken token, CancellationToken cancellation = default)
	{
		return token == null || token.Certificate == null ?
			ValueTask.FromResult<IUserIdentity>(null) : ValueTask.FromResult<IUserIdentity>(new UserIdentity(token));
	}
	#endregion
}
