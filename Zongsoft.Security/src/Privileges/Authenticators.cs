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
		   string.Equals(user.Name, IUser.Administrator, StringComparison.OrdinalIgnoreCase))
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
