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
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace Zongsoft.Security;

public static class ClaimsPrincipalExtension
{
	public static ClaimsIdentity GetIdentity(this ClaimsPrincipal principal, string scheme)
	{
		if(principal == null)
			throw new ArgumentNullException(nameof(principal));

		if(string.IsNullOrEmpty(scheme))
			return principal.Identity as ClaimsIdentity;

		return principal.Identity is ClaimsIdentity primary && primary.AuthenticationType == scheme ?
			primary : principal.Identities.FirstOrDefault(identity => identity.AuthenticationType == scheme);
	}

	public static ClaimsIdentity GetIdentity(this ClaimsPrincipal principal, params string[] schemes)
	{
		if(principal == null)
			throw new ArgumentNullException(nameof(principal));

		if(schemes == null || schemes.Length == 0)
			return principal.Identity as ClaimsIdentity;

		return principal.Identity is ClaimsIdentity primary && schemes.Contains(primary.AuthenticationType) ?
			primary : principal.Identities.FirstOrDefault(identity => schemes.Contains(identity.AuthenticationType));
	}

	public static bool IsAnonymous(this IPrincipal principal)
	{
		return principal == null || principal.Identity.IsAnonymous();
	}

	public static bool InRole(this ClaimsPrincipal principal, string role, string module = null)
	{
		if(principal == null || string.IsNullOrEmpty(role))
			return false;

		if(string.IsNullOrEmpty(module))
			return (principal.Identity is ClaimsIdentity identity) && identity.InRole(role);

		foreach(var identity in principal.Identities)
		{
			if(identity != null && identity.HasClaim(ClaimTypes.System, module))
				return identity.InRole(role);
		}

		return false;
	}

	public static bool InRoles(this ClaimsPrincipal principal, string[] roles, string module = null)
	{
		if(principal == null || roles == null || roles.Length == 0)
			return false;

		if(string.IsNullOrEmpty(module))
			return (principal.Identity is ClaimsIdentity identity) && identity.InRoles(roles);

		foreach(var identity in principal.Identities)
		{
			if(identity != null && identity.HasClaim(ClaimTypes.System, module))
				return identity.InRoles(roles);
		}

		return false;
	}

	public static bool IsAdministrator(this ClaimsPrincipal principal, string module = null)
	{
		if(principal == null)
			return false;

		if(string.IsNullOrEmpty(module))
			return principal.Identity is ClaimsIdentity identity && identity.IsAdministrator();

		foreach(var identity in principal.Identities)
		{
			if(identity != null && identity.IsAuthenticated &&
			   identity.HasClaim(ClaimTypes.System, module))
				return identity.IsAdministrator();
		}

		return false;
	}
}