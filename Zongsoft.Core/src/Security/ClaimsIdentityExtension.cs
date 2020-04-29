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
using System.Security.Claims;
using System.Security.Principal;

namespace Zongsoft.Security
{
	public static class ClaimsIdentityExtension
	{
		public static bool IsAnonymous(this IIdentity identity)
		{
			return identity == null || !identity.IsAuthenticated || string.IsNullOrEmpty(identity.Name);
		}

		public static bool IsAdministrator(this ClaimsIdentity identity)
		{
			const string USER_ADMINISTRATOR = "Administrator";
			const string ROLE_ADMINISTRATORS = "Administrators";

			return
				identity != null &&
				identity.IsAuthenticated &&
				(
					string.Equals(identity.Name, USER_ADMINISTRATOR, StringComparison.OrdinalIgnoreCase) ||
					identity.HasClaim(identity.RoleClaimType, ROLE_ADMINISTRATORS)
				);
		}

		public static bool InRole(this ClaimsIdentity identity, string role)
		{
			if(identity == null || string.IsNullOrEmpty(role))
				return false;

			return identity.HasClaim(identity.RoleClaimType, role);
		}

		public static bool InRoles(this ClaimsIdentity identity, params string[] roles)
		{
			if(identity == null || roles == null || roles.Length == 0)
				return false;

			var name = identity.RoleClaimType;

			foreach(var claim in identity.Claims)
			{
				if(claim != null &&
				   string.Equals(name, claim.Type, StringComparison.OrdinalIgnoreCase) &&
				   roles.Contains(claim.Value, StringComparer.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}

		public static void AddRoles(this ClaimsIdentity identity, string[] roles)
		{
			AddRoles(identity, null, roles);
		}

		public static void AddRoles(this ClaimsIdentity identity, string issuer, string[] roles)
		{
			if(identity == null)
				throw new ArgumentNullException(nameof(identity));

			if(roles == null || roles.Length == 0)
				return;

			if(string.IsNullOrEmpty(issuer))
				issuer = ClaimsIdentity.DefaultIssuer;

			identity.AddClaims
			(
				roles.Where(role => role != null && role.Length > 0)
				     .Select(role => new Claim(identity.RoleClaimType, role, ClaimValueTypes.String, issuer, issuer, identity))
			);
		}

		public static uint GetUserId(this IIdentity identity)
		{
			return GetUserId(identity as ClaimsIdentity);
		}

		public static uint GetUserId(this ClaimsIdentity identity)
		{
			var value = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			if(value != null && value.Length > 0 && uint.TryParse(value, out var userId))
				return userId;

			return 0;
		}

		public static string GetEmail(this IIdentity identity)
		{
			return GetEmail(identity as ClaimsIdentity);
		}

		public static string GetEmail(this ClaimsIdentity identity)
		{
			return identity?.FindFirst(ClaimTypes.Email)?.Value;
		}

		public static string GetPhone(this IIdentity identity)
		{
			return GetPhone(identity as ClaimsIdentity);
		}

		public static string GetPhone(this ClaimsIdentity identity)
		{
			return identity?.FindFirst(ClaimTypes.MobilePhone)?.Value;
		}

		public static string GetNamespace(this IIdentity identity)
		{
			return GetNamespace(identity as ClaimsIdentity);
		}

		public static string GetNamespace(this ClaimsIdentity identity)
		{
			return identity?.FindFirst(ClaimNames.Namespace)?.Value;
		}

		public static Membership.UserStatus GetStatus(this ClaimsIdentity identity)
		{
			var status = identity?.FindFirst(ClaimNames.UserStatus)?.Value;

			if(status != null && status.Length > 0)
			{
				if(byte.TryParse(status, out var value))
					return (Membership.UserStatus)value;
			}

			return 0;
		}

		public static Membership.UserStatus GetStatus(this ClaimsIdentity identity, out DateTime? timestamp)
		{
			timestamp = null;
			var statusValue = identity?.FindFirst(ClaimNames.UserStatus)?.Value;

			if(statusValue != null && statusValue.Length > 0)
			{
				var timestampValue = identity.FindFirst(ClaimNames.UserStatusTimestamp)?.Value;
				if(timestampValue != null && timestampValue.Length > 0 && DateTime.TryParse(timestampValue, out var datetime))
					timestamp = datetime;

				if(byte.TryParse(statusValue, out var status))
					return (Membership.UserStatus)status;
			}

			return 0;
		}

		public static Membership.IUserIdentity AsUser(this IIdentity identity)
		{
			return AsUser(identity as ClaimsIdentity);
		}

		public static Membership.IUserIdentity AsUser(this ClaimsIdentity identity)
		{
			var model = AsModel<Membership.IUserIdentity>(identity, (user, claim) => FillUser(user, claim));
			model.FullName = identity.Label;
			return model;
		}

		public static T AsModel<T>(this IIdentity identity, Action<T, Claim> configure) where T : class
		{
			return AsModel(identity as ClaimsIdentity, configure);
		}

		public static T AsModel<T>(this ClaimsIdentity identity, Action<T, Claim> configure) where T : class
		{
			if(identity == null || identity.IsAnonymous())
				return null;

			T model;

			if(typeof(T).IsAbstract)
				model = Zongsoft.Data.Model.Build<T>();
			else
				model = Activator.CreateInstance<T>();

			if(configure != null)
			{
				foreach(var claim in identity.Claims)
					configure(model, claim);
			}

			return model;
		}

		private static void FillUser(Membership.IUserIdentity user, Claim claim)
		{
			if(string.Equals(claim.Type, ClaimTypes.Name, StringComparison.OrdinalIgnoreCase))
			{
				user.Name = claim.Value;
			}
			else if(string.Equals(claim.Type, ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase))
			{
				if(uint.TryParse(claim.Value, out var userId))
					user.UserId = userId;
			}
			else if(string.Equals(claim.Type, ClaimNames.Namespace, StringComparison.OrdinalIgnoreCase))
			{
				user.Namespace = claim.Value;
			}
			else if(string.Equals(claim.Type, ClaimNames.Description, StringComparison.OrdinalIgnoreCase))
			{
				user.Description = claim.Value;
			}
		}
	}
}
