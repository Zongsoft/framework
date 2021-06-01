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
using System.Security.Claims;
using System.Security.Principal;

namespace Zongsoft.Security.Membership
{
	public static class UserExtension
	{
		public static ClaimsIdentity Identity(this IUser user, string scheme, string issuer, TimeSpan? expiration = null)
		{
			if(user == null)
				return new ClaimsIdentity();

			var identity = new CredentialIdentity(user.Name, scheme, issuer)
			{
				Label = user.FullName
			};

			identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString(), ClaimValueTypes.UInteger32, issuer, issuer, identity));

			if(!string.IsNullOrEmpty(user.Namespace))
				identity.AddClaim(new Claim(ClaimNames.Namespace, user.Namespace, ClaimValueTypes.String, issuer, issuer, identity));
			if(!string.IsNullOrEmpty(user.Description))
				identity.AddClaim(new Claim(ClaimNames.Description, user.Description, ClaimValueTypes.String, issuer, issuer, identity));
			if(!string.IsNullOrEmpty(user.Email))
				identity.AddClaim(new Claim(ClaimTypes.Email, user.Email.ToString(), ClaimValueTypes.String, issuer, issuer, identity));
			if(!string.IsNullOrEmpty(user.Phone))
				identity.AddClaim(new Claim(ClaimTypes.MobilePhone, user.Phone.ToString(), ClaimValueTypes.String, issuer, issuer, identity));

			identity.AddClaim(new Claim(ClaimNames.UserStatus, user.Status.ToString(), ClaimValueTypes.Integer32, issuer, issuer, identity));

			if(user.StatusTimestamp.HasValue)
				identity.AddClaim(new Claim(ClaimNames.UserStatusTimestamp, user.StatusTimestamp.ToString(), ClaimValueTypes.DateTime, issuer, issuer, identity));

			identity.AddClaim(new Claim(ClaimNames.Creation, user.Creation.ToString(), ClaimValueTypes.DateTime, issuer, issuer, identity));

			if(user.Modification.HasValue)
				identity.AddClaim(new Claim(ClaimNames.Modification, user.Modification.ToString(), ClaimValueTypes.DateTime, issuer, issuer, identity));

			if(expiration.HasValue && expiration.Value > TimeSpan.Zero)
				identity.AddClaim(new Claim(ClaimTypes.Expiration, expiration.ToString(), expiration.Value.TotalHours > 24 ? ClaimValueTypes.YearMonthDuration : ClaimValueTypes.DaytimeDuration, issuer, issuer, identity));

			return identity;
		}
	}
}