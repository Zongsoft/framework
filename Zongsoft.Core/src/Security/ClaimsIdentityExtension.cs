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
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;

namespace Zongsoft.Security
{
	public static class ClaimsIdentityExtension
	{
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
				user.UserId = GetUserId(identity.Claims);
				user.Name = identity.Name;
				user.FullName = identity.Label;
				user.Namespace = "";
				user.Description = "";
			});
		}

		private static uint GetUserId(IEnumerable<Claim> claims)
		{
			return uint.Parse(claims.First(p => p.Type == ClaimTypes.NameIdentifier).Value);
		}
	}
}
