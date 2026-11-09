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
using System.Security.Principal;

namespace Zongsoft.Security;

internal static class Anonymous
{
	public static readonly IIdentity Identity = new AnonymousIdentity();
	public static readonly ClaimsPrincipal Principal = new AnonymousPrincipal();

	private class AnonymousPrincipal : ClaimsPrincipal
	{
		public AnonymousPrincipal() : base(Anonymous.Identity) { }
	}

	private class AnonymousIdentity : IIdentity
	{
		public AnonymousIdentity() { }

		public string AuthenticationType => string.Empty;
		public bool IsAuthenticated => false;
		public string Name => string.Empty;
	}
}
