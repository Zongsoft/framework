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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Data;

namespace Zongsoft.Security.Privileges;

public static class UserUtility
{
	public static ICondition GetIdentity(string identity, string @namespace = null)
	{
		if(string.IsNullOrEmpty(identity))
			return null;

		var index = identity.IndexOf(':');

		if(index > 0 && index < identity.Length)
		{
			var prefix = identity[..index];

			return prefix.ToLowerInvariant() switch
			{
				"phone" => Condition.Equal(nameof(IUser.Phone), identity[(index + 1)..]) & GetNamespace(@namespace),
				"email" => Condition.Equal(nameof(IUser.Email), identity[(index + 1)..]) & GetNamespace(@namespace),
				_ => Condition.Equal(nameof(IUser.Name), identity) & GetNamespace(@namespace),
			};
		}

		if(identity.Contains('@'))
			return Condition.Equal(nameof(IUser.Email), identity) & GetNamespace(@namespace);
		if(char.IsDigit(identity[0]))
			return Condition.Equal(nameof(IUser.Phone), identity) & GetNamespace(@namespace);

		return Condition.Equal(nameof(IUser.Name), identity) & GetNamespace(@namespace);

		static Condition GetNamespace(string @namespace) => string.IsNullOrEmpty(@namespace) ?
			Condition.Equal(nameof(IUser.Namespace), null) :
			Condition.Equal(nameof(IUser.Namespace), @namespace);
	}

	public static ClaimsIdentity Identity(this IUser user, string scheme, string issuer, TimeSpan? expiration = null)
	{
		if(user == null)
			return new ClaimsIdentity();

		var identity = new CredentialIdentity(user.Name, scheme, issuer)
		{
			Label = user.Nickname
		};

		SetClaims(identity, user, expiration);

		return identity;
	}

	public static bool SetClaims(this ClaimsIdentity identity, IUser user, TimeSpan? expiration = null)
	{
		if(identity == null || user == null)
			return false;

		if(!string.IsNullOrWhiteSpace(user.Nickname))
			identity.Label = user.Nickname;

		identity.AddClaim(new Claim(identity.NameClaimType, user.Name, ClaimValueTypes.String));
		identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Identifier.Value.ToString()));

		if(!string.IsNullOrEmpty(user.Email))
			identity.AddClaim(new Claim(ClaimTypes.Email, user.Email, ClaimValueTypes.String));
		if(!string.IsNullOrEmpty(user.Phone))
			identity.AddClaim(new Claim(ClaimTypes.MobilePhone, user.Phone, ClaimValueTypes.String));

		if(!string.IsNullOrEmpty(user.Gender))
			identity.AddClaim(new Claim(ClaimTypes.Gender, user.Gender, ClaimValueTypes.String));
		if(!string.IsNullOrEmpty(user.Avatar))
			identity.AddClaim(new Claim(nameof(user.Avatar), user.Avatar, ClaimValueTypes.String));

		if(!string.IsNullOrEmpty(user.Namespace))
			identity.AddClaim(new Claim(ClaimNames.Namespace, user.Namespace, ClaimValueTypes.String));
		if(!string.IsNullOrEmpty(user.Description))
			identity.AddClaim(new Claim(ClaimNames.Description, user.Description, ClaimValueTypes.String));

		if(expiration.HasValue && expiration.Value > TimeSpan.Zero)
			identity.AddClaim(new Claim(ClaimTypes.Expiration, expiration.ToString(), expiration.Value.TotalHours > 24 ? ClaimValueTypes.YearMonthDuration : ClaimValueTypes.DaytimeDuration));

		return true;
	}
}