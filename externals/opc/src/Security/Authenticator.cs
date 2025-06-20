﻿/*
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

namespace Zongsoft.Externals.Opc.Security;

public partial class Authenticator : IAuthenticator
{
	#region 单例字段
	public static readonly Authenticator Default = new DefaultAuthenticator();
	#endregion

	#region 公共方法
	public ValueTask<bool> AuthenticateAsync(OpcServer server, AuthenticationIdentity identity, CancellationToken cancellation = default)
	{
		if(identity == null)
			return ValueTask.FromResult(false);

		return identity switch
		{
			AuthenticationIdentity.Account user => this.OnAuthenticateAsync(server, user, cancellation),
			AuthenticationIdentity.Certificate x509 => this.OnAuthenticateAsync(server, x509, cancellation),
			_ => ValueTask.FromResult(false),
		};
	}
	#endregion

	#region 虚拟方法
	protected virtual ValueTask<bool> OnAuthenticateAsync(OpcServer server, AuthenticationIdentity.Account identity, CancellationToken cancellation = default)
	{
		return ValueTask.FromResult(!string.IsNullOrEmpty(identity.UserName));
	}

	protected virtual ValueTask<bool> OnAuthenticateAsync(OpcServer server, AuthenticationIdentity.Certificate identity, CancellationToken cancellation = default)
	{
		return ValueTask.FromResult(identity.X509 != null && identity.X509.Verify());
	}
	#endregion
}
