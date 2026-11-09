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
using System.Security.Claims;
using System.Collections.Generic;

using Zongsoft.Components;

namespace Zongsoft.Security.Privileges;

public partial class Authenticators
{
	private static readonly Lazy<IdentityAuthenticator> _identity = new(true);
	private static readonly Lazy<SecretorAuthenticator> _secretor = new(true);

	public static IdentityAuthenticator Identity => _identity.Value;
	public static SecretorAuthenticator Secretor => _secretor.Value;

	private static async ValueTask<ClaimsIdentity> IssueRolesAsync(IUser user, ClaimsIdentity identity, CancellationToken cancellation)
	{
		if(user == null || identity == null || !user.Identifier.HasValue ||
		   string.Equals(user.Name, Users.Administrator, StringComparison.OrdinalIgnoreCase))
			return identity;

		var pending = new Queue<Member>();
		var visited = new HashSet<Identifier>();
		pending.Enqueue(Member.User(user.Identifier));

		while(pending.TryDequeue(out var member))
		{
			await foreach(var role in Authentication.Servicer.Members.GetParentsAsync(member, cancellation))
			{
				if(role == null || !role.Enabled || !visited.Add(role.Identifier))
					continue;

				if(!identity.InRoles([role.Name]))
					identity.AddRole(role.Name);

				pending.Enqueue(Member.Role(role.Identifier));
			}
		}

		return identity;
	}
}
