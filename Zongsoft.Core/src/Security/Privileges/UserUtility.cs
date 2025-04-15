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

internal static class UserUtility
{
	internal static ICondition GetCriteria(string identity, string @namespace = null) => GetCriteria(identity, @namespace, out _);
	internal static ICondition GetCriteria(string identity, out string identityType) => GetCriteria(identity, null, out identityType);
	internal static ICondition GetCriteria(string identity, string @namespace, out string identityType)
	{
		if(string.IsNullOrEmpty(identity))
		{
			identityType = null;
			return null;
		}

		var index = identity.IndexOf(':');

		if(index > 0 && index < identity.Length)
		{
			identityType = identity[..index];

			return identityType.ToLowerInvariant() switch
			{
				"phone" => Condition.Equal(nameof(IUser.Phone), identity[(index + 1)..]) & GetNamespace(@namespace),
				"email" => Condition.Equal(nameof(IUser.Email), identity[(index + 1)..]) & GetNamespace(@namespace),
				_ => Condition.Equal(nameof(IUser.Name), identity) & GetNamespace(@namespace),
			};
		}

		if(identity.Contains('@'))
		{
			identityType = nameof(IUser.Email);
			return Condition.Equal(nameof(IUser.Email), identity) & GetNamespace(@namespace);
		}

		if(IsPhone(identity))
		{
			identityType = nameof(IUser.Phone);
			return Condition.Equal(nameof(IUser.Phone), identity) & GetNamespace(@namespace);
		}

		identityType = nameof(IUser.Name);
		return Condition.Equal(nameof(IUser.Name), identity) & GetNamespace(@namespace);

		static Condition GetNamespace(string @namespace) => @namespace switch
		{
			null or "*" => null,
			"" => Condition.Equal(nameof(IUser.Namespace), null),
			_ => Condition.Equal(nameof(IUser.Namespace), @namespace),
		};

		static bool IsPhone(ReadOnlySpan<char> text)
		{
			if(text.IsEmpty)
				return false;

			for(int i = 0; i < text.Length; i++)
			{
				if(!char.IsDigit(text[i]) && text[i] != '+')
					return false;
			}

			return true;
		}
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

	private static bool SetClaims(this ClaimsIdentity identity, IUser user, TimeSpan? expiration = null)
	{
		if(identity == null || user == null)
			return false;

		if(!string.IsNullOrWhiteSpace(user.Nickname))
			identity.Label = user.Nickname;

		identity.SetClaim(identity.NameClaimType, user.Name, ClaimValueTypes.String);
		identity.SetClaim(ClaimTypes.NameIdentifier, user.Identifier.Value);

		if(!string.IsNullOrEmpty(user.Email))
			identity.SetClaim(ClaimTypes.Email, user.Email, ClaimValueTypes.Email);
		if(!string.IsNullOrEmpty(user.Phone))
			identity.SetClaim(ClaimTypes.MobilePhone, user.Phone, ClaimValueTypes.String);

		if(user.Gender.HasValue)
			identity.SetClaim(ClaimTypes.Gender, user.Gender, ClaimValueTypes.Boolean);
		if(!string.IsNullOrEmpty(user.Avatar))
			identity.SetClaim(nameof(user.Avatar), user.Avatar, ClaimValueTypes.String);

		if(!string.IsNullOrEmpty(user.Namespace))
			identity.SetClaim(ClaimNames.Namespace, user.Namespace, ClaimValueTypes.String);
		if(!string.IsNullOrEmpty(user.Description))
			identity.SetClaim(ClaimNames.Description, user.Description, ClaimValueTypes.String);

		if(expiration.HasValue && expiration.Value > TimeSpan.Zero)
			identity.SetClaim(ClaimTypes.Expiration, expiration.ToString(), expiration.Value.TotalHours > 24 ? ClaimValueTypes.YearMonthDuration : ClaimValueTypes.DaytimeDuration);

		return true;
	}
}