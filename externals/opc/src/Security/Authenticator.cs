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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
