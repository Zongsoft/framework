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
using System.Security.Claims;

namespace Zongsoft.Security.Privileges;

public static class AuthenticatorExtension
{
	public static ClaimsIdentity Identity(this IAuthenticator authenticator, IUser user, TimeSpan? expiration = null)
	{
		if(user == null)
			return new ClaimsIdentity();

		var issuer = authenticator.Name;
		return user.Identity(authenticator.Name, issuer, expiration);
	}
}
