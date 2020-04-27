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
using System.Security.Claims;

namespace Zongsoft.Security
{
	public static class ClaimsIdentityExtension
	{
		private const string Namespace_ClaimType = "http://schemas.zongsoft.com/security/claims/namespace";
		private const string Description_ClaimType = "http://schemas.zongsoft.com/security/claims/description";

		public static Membership.IUserIdentity AsUser(this System.Security.Principal.IIdentity identity)
		{
			return AsUser(identity as ClaimsIdentity);
		}

		public static Membership.IUserIdentity AsUser(this ClaimsIdentity identity)
		{
			if(identity == null)
				return null;

			return Zongsoft.Data.Model.Build<Membership.IUserIdentity>(user =>
			{
				GetUserInfo(identity.Claims, out var userId, out var @namespace, out var description);

				user.UserId = userId;
				user.Name = identity.Name;
				user.FullName = identity.Label;
				user.Namespace = @namespace;
				user.Description = description;
			});
		}

		private static void GetUserInfo(IEnumerable<Claim> claims, out uint userId, out string @namespace, out string description)
		{
			int count = 0;

			userId = 0;
			@namespace = null;
			description = null;

			foreach(var claim in claims)
			{
				if(string.Equals(claim.Type, ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase))
				{
					uint.TryParse(claim.Value, out userId);
					count++;
				}
				else if(string.Equals(claim.Type, Namespace_ClaimType, StringComparison.OrdinalIgnoreCase))
				{
					count++;
					@namespace = claim.Value;
				}
				else if(string.Equals(claim.Type, Description_ClaimType, StringComparison.OrdinalIgnoreCase))
				{
					count++;
					description = claim.Value;
				}

				if(count >= 3)
					return;
			}
		}
	}
}
