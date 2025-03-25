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
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Reflection;

namespace Zongsoft.Security;

public static class ClaimsIdentityExtension
{
	#region 公共方法
	public static bool IsAnonymous(this IIdentity identity) => identity == null || !identity.IsAuthenticated || string.IsNullOrEmpty(identity.Name);
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

	public static bool InRole(this ClaimsIdentity identity, string role) => identity != null && role != null && identity.HasClaim(identity.RoleClaimType, role);
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

	public static Claim AddRole(this ClaimsIdentity identity, string role) => AddRole(identity, null, role);
	public static Claim AddRole(this ClaimsIdentity identity, string issuer, string role)
	{
		if(string.IsNullOrEmpty(role))
			return null;

		if(string.IsNullOrEmpty(issuer))
			issuer = ClaimsIdentity.DefaultIssuer;

		var claim = new Claim(identity.RoleClaimType, role, ClaimValueTypes.String, issuer, issuer, identity);
		identity.AddClaim(claim);
		return claim;
	}

	public static void AddRoles(this ClaimsIdentity identity, IEnumerable<string> roles) => AddRoles(identity, null, roles);
	public static void AddRoles(this ClaimsIdentity identity, string issuer, IEnumerable<string> roles)
	{
		if(identity == null)
			throw new ArgumentNullException(nameof(identity));

		if(roles == null)
			return;

		if(string.IsNullOrEmpty(issuer))
			issuer = ClaimsIdentity.DefaultIssuer;

		identity.AddClaims
		(
			roles.Where(role => role != null && role.Length > 0)
				 .Select(role => new Claim(identity.RoleClaimType, role, ClaimValueTypes.String, issuer, issuer, identity))
		);
	}

	public static object GetIdentifier(this IIdentity identity) => GetIdentifier(identity as ClaimsIdentity);
	public static object GetIdentifier(this ClaimsIdentity identity) => identity != null && identity.FindFirst(ClaimTypes.NameIdentifier).TryGetValue(out var value) ? value : null;
	public static T GetIdentifier<T>(this IIdentity identity) => GetIdentifier<T>(identity as ClaimsIdentity);
	public static T GetIdentifier<T>(this ClaimsIdentity identity)
	{
		var claim = identity?.FindFirst(ClaimTypes.NameIdentifier);
		return claim == null || string.IsNullOrEmpty(claim.Value) ? default : Common.Convert.ConvertValue<T>(claim.Value);
	}

	public static string GetEmail(this IIdentity identity) => GetEmail(identity as ClaimsIdentity);
	public static string GetEmail(this ClaimsIdentity identity) => identity?.FindFirst(ClaimTypes.Email)?.Value;
	public static string GetPhone(this IIdentity identity) => GetPhone(identity as ClaimsIdentity);
	public static string GetPhone(this ClaimsIdentity identity) => identity?.FindFirst(ClaimTypes.MobilePhone)?.Value;
	public static string GetNamespace(this IIdentity identity) => GetNamespace(identity as ClaimsIdentity);
	public static string GetNamespace(this ClaimsIdentity identity) => identity?.FindFirst(ClaimNames.Namespace)?.Value;
	public static bool SetNamespace(this IIdentity identity, string @namespace) => SetNamespace(identity as ClaimsIdentity, @namespace);
	public static bool SetNamespace(this ClaimsIdentity identity, string @namespace, string issuer = null, string originalIssuer = null)
	{
		if(string.IsNullOrWhiteSpace(@namespace))
			return false;

		return SetClaim(identity, ClaimNames.Namespace, @namespace, ClaimValueTypes.String, issuer, originalIssuer) != null;
	}

	public static T AsModel<T>(this IIdentity identity, Func<T, Claim, bool> configure = null) where T : class
	{
		return AsModel(identity as ClaimsIdentity, configure);
	}

	public static T AsModel<T>(this ClaimsIdentity identity, Func<T, Claim, bool> configure = null) where T : class
	{
		static string GetClaimName(string text)
		{
			var index = text.LastIndexOf('/');

			if(index >= 0 && index < text.Length - 1)
				return text.Substring(index + 1);

			return text;
		}

		static void ConfigureProperties(IDictionary<string, object> properties, Claim claim)
		{
			var isMultiple =
				claim.Type == ClaimNames.Authorization ||
				claim.Type == (claim.Subject?.RoleClaimType ?? ClaimsIdentity.DefaultRoleClaimType);

			var key = GetClaimName(claim.Type) + (isMultiple ? "s" : null);

			if(properties.TryGetValue(key, out var value))
			{
				if(value is ICollection<string> collection)
					collection.Add(claim.Value);
				else
					properties[key] = new List<string>(new[] { value?.ToString(), claim.Value });
			}
			else
			{
				if(isMultiple)
					properties.Add(key, new List<string>(new[] { claim.Value }));
				else
					properties.Add(key, claim.Value);
			}
		}

		if(identity == null || identity.IsAnonymous())
			return null;

		T model;

		if(typeof(T).IsAbstract)
			model = Model.Build<T>();
		else
			model = Activator.CreateInstance<T>();

		if(typeof(Privileges.IUser).IsAssignableFrom(typeof(T)))
		{
			var user = (Privileges.IUser)model;
			user.Nickname = identity.Label;

			var propertiesInfo = model.GetType().GetProperty("Properties");

			foreach(var claim in identity.Claims)
			{
				if(!SetUserProperty(user, claim))
				{
					var configured = configure?.Invoke(model, claim);

					if(configured == null || !configured.Value)
					{
						if(propertiesInfo != null && Reflector.TryGetValue(propertiesInfo, ref model, out var value) && value is IDictionary<string, object> properties)
							ConfigureProperties(properties, claim);
					}
				}
			}
		}
		else if(model is IModel m)
		{
			m.TrySetValue("FullName", identity.Label);
			m.TrySetValue("Nickname", identity.Label);

			var propertiesInfo = model.GetType().GetProperty("Properties");

			foreach(var claim in identity.Claims)
			{
				if(!SetModelProperty(m, claim))
				{
					var configured = configure?.Invoke(model, claim);

					if(configured == null || !configured.Value)
					{
						if(propertiesInfo != null && Reflector.TryGetValue(propertiesInfo, ref model, out var value) && value is IDictionary<string, object> properties)
							ConfigureProperties(properties, claim);
					}
				}
			}
		}
		else if(configure != null)
		{
			Reflector.TrySetValue(ref model, "FullName", identity.Label);
			Reflector.TrySetValue(ref model, "Nickname", identity.Label);

			var propertiesInfo = model.GetType().GetProperty("Properties");

			foreach(var claim in identity.Claims)
			{
				if(!SetObjectProperty(model, claim))
				{
					var configured = configure?.Invoke(model, claim);

					if(configured == null || !configured.Value)
					{
						if(propertiesInfo != null && Reflector.TryGetValue(propertiesInfo, ref model, out var value) && value is IDictionary<string, object> properties)
							ConfigureProperties(properties, claim);
					}
				}
			}
		}

		return model;
	}

	public static bool TryGetClaim<T>(this IIdentity identity, string name, out T value, StringExtension.TryParser<T> converter = null)
	{
		if(identity is ClaimsIdentity claimsIdentity)
			return TryGetClaim<T>(claimsIdentity, name, out value, converter);

		value = default;
		return false;
	}

	public static bool TryGetClaim<T>(this ClaimsIdentity identity, string name, out T value, StringExtension.TryParser<T> converter = null)
	{
		if(TryGetClaim(identity, name, out var text))
		{
			if(converter != null)
				converter(text, out value);
			else
				value = Common.Convert.ConvertValue<T>(text);

			return true;
		}

		value = default;
		return false;
	}

	public static bool TryGetClaim(this IIdentity identity, string name, out string value)
	{
		if(identity is ClaimsIdentity claimsIdentity)
			return TryGetClaim(claimsIdentity, name, out value);

		value = null;
		return false;
	}

	public static bool TryGetClaim(this ClaimsIdentity identity, string name, out string value)
	{
		if(identity == null)
			throw new ArgumentNullException(nameof(identity));

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		var claim = identity.FindFirst(name);

		if(claim != null)
		{
			value = claim.Value;
			return true;
		}

		value = null;
		return false;
	}

	public static bool TryGetClaims<T>(this IIdentity identity, string name, out T[] values, StringExtension.TryParser<T> converter = null)
	{
		if(identity is ClaimsIdentity claimsIdentity)
			return TryGetClaims<T>(claimsIdentity, name, out values, converter);

		values = null;
		return false;
	}

	public static bool TryGetClaims<T>(this ClaimsIdentity identity, string name, out T[] values, StringExtension.TryParser<T> converter = null)
	{
		if(TryGetClaims(identity, name, out var raws))
		{
			var list = new List<T>(raws.Length);

			if(converter == null)
				converter = Common.Convert.TryConvertValue<T>;

			foreach(var raw in raws)
			{
				if(converter(raw, out var value))
					list.Add(value);
			}

			values = list.ToArray();
			return true;
		}

		values = null;
		return false;
	}

	public static bool TryGetClaims(this IIdentity identity, string name, out string[] values)
	{
		if(identity is ClaimsIdentity claimsIdentity)
			return TryGetClaims(claimsIdentity, name, out values);

		values = null;
		return false;
	}

	public static bool TryGetClaims(this ClaimsIdentity identity, string name, out string[] values)
	{
		if(identity == null)
			throw new ArgumentNullException(nameof(identity));

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		values = identity.FindAll(name).Select(claim => claim.Value).ToArray();
		return values != null && values.Length > 0;
	}

	public static bool RemoveClaim(this IIdentity identity, string name) => RemoveClaim(identity as ClaimsIdentity, name);
	public static bool RemoveClaim(this ClaimsIdentity identity, string name)
	{
		if(identity == null || string.IsNullOrEmpty(name))
			return false;

		//注意：必须将结果集转成新集，因为之后的循环删除会因为集合变化导致循环异常
		var claims = identity.FindAll(name).ToArray();

		for(int i = 0; i < claims.Length; i++)
			identity.RemoveClaim(claims[i]);

		return claims != null && claims.Length > 0;
	}

	public static Claim SetClaim(this IIdentity identity, string name, object value, string issuer = null, string originalIssuer = null) =>
		SetClaim(identity as ClaimsIdentity, name, value, issuer, originalIssuer);
	public static Claim SetClaim(this ClaimsIdentity identity, string name, object value, string issuer = null, string originalIssuer = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(identity == null)
			return null;

		if(string.IsNullOrEmpty(issuer))
			issuer = ClaimsIdentity.DefaultIssuer;
		if(string.IsNullOrEmpty(originalIssuer))
			originalIssuer = ClaimsIdentity.DefaultIssuer;

		identity.RemoveClaim(name);
		if(value == null)
			return null;

		var claim = new Claim(name, Common.Convert.ConvertValue<string>(value), ClaimUtility.GetValueType(value.GetType()), issuer, originalIssuer, identity);
		identity.AddClaim(claim);
		return claim;
	}

	public static Claim SetClaim(this IIdentity identity, string name, string value, string valueType, string issuer = null, string originalIssuer = null) =>
		SetClaim(identity as ClaimsIdentity, name, value, valueType, issuer, originalIssuer);
	public static Claim SetClaim(this ClaimsIdentity identity, string name, string value, string valueType, string issuer = null, string originalIssuer = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(identity == null || string.IsNullOrEmpty(value))
			return null;

		if(string.IsNullOrEmpty(issuer))
			issuer = ClaimsIdentity.DefaultIssuer;
		if(string.IsNullOrEmpty(originalIssuer))
			originalIssuer = ClaimsIdentity.DefaultIssuer;

		identity.RemoveClaim(name);
		if(value == null)
			return null;

		var claim = new Claim(name, value, valueType, issuer, originalIssuer, identity);
		identity.AddClaim(claim);
		return claim;
	}

	public static Claim AddClaim(this IIdentity identity, string name, string value, string valueType, string issuer = null, string originalIssuer = null) =>
		AddClaim(identity as ClaimsIdentity, name, value, valueType, issuer, originalIssuer);
	public static Claim AddClaim(this ClaimsIdentity identity, string name, string value, string valueType, string issuer = null, string originalIssuer = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(identity == null || string.IsNullOrEmpty(value))
			return null;

		if(string.IsNullOrEmpty(issuer))
			issuer = ClaimsIdentity.DefaultIssuer;
		if(string.IsNullOrEmpty(originalIssuer))
			originalIssuer = ClaimsIdentity.DefaultIssuer;

		var claim = new Claim(name, value, valueType, issuer, originalIssuer, identity);
		identity.AddClaim(claim);
		return claim;
	}

	public static string GetQualifiedName(this IIdentity identity) => GetQualifiedName(identity as ClaimsIdentity);
	public static string GetQualifiedName(this ClaimsIdentity identity)
	{
		if(identity == null)
			return null;

		var @namespace = identity.GetNamespace();
		return string.IsNullOrEmpty(@namespace) || @namespace == "*" ? identity.Name : $"{@namespace}:{identity.Name}";
	}

	public static Identifier Identify(this IIdentity identity) => Identify(identity as ClaimsIdentity);
	public static Identifier Identify(this ClaimsIdentity identity) => new
	(
		identity.GetType(),
		identity.GetIdentifier(),
		identity.GetQualifiedName(),
		identity.TryGetClaim<string>(nameof(Identifier.Description), out var description) ? description : null
	);
	#endregion

	#region 私有方法
	private static bool SetUserProperty(Privileges.IUser user, Claim claim)
	{
		switch(claim.Type)
		{
			case ClaimTypes.Name:
				user.Name = claim.Value;
				return true;
			case ClaimTypes.NameIdentifier:
				user.Identifier = new Identifier(typeof(Privileges.IUser), claim.GetValue());
				return true;
			case ClaimTypes.Email:
				user.Email = claim.Value;
				return true;
			case ClaimTypes.MobilePhone:
				user.Phone = claim.Value;
				return true;
			case ClaimNames.Namespace:
				user.Namespace = claim.Value;
				return true;
			case ClaimNames.Description:
				user.Description = claim.Value;
				return true;
		}

		return false;
	}

	private static bool SetModelProperty(IModel model, Claim claim)
	{
		switch(claim.Type)
		{
			case ClaimTypes.Name:
				return model.TrySetValue(nameof(Privileges.IUser.Name), claim.Value);
			case ClaimTypes.NameIdentifier:
				return SetIdentifier(ref model, claim);
			case ClaimNames.Namespace:
				return model.TrySetValue(nameof(Privileges.IUser.Namespace), claim.Value);
			case ClaimNames.Description:
				return model.TrySetValue(nameof(Privileges.IUser.Description), claim.Value);
			case ClaimTypes.Email:
				return model.TrySetValue(nameof(Privileges.IUser.Email), claim.Value);
			case ClaimTypes.MobilePhone:
				return model.TrySetValue(nameof(Privileges.IUser.Phone), claim.Value);
		}

		return false;
	}

	private static bool SetObjectProperty(object target, Claim claim)
	{
		switch(claim.Type)
		{
			case ClaimTypes.Name:
				return Reflector.TrySetValue(ref target, nameof(Privileges.IUser.Name), claim.Value);
			case ClaimTypes.NameIdentifier:
				return SetIdentifier(ref target, claim);
			case ClaimNames.Namespace:
				return Reflector.TrySetValue(ref target, nameof(Privileges.IUser.Namespace), claim.Value);
			case ClaimNames.Description:
				return Reflector.TrySetValue(ref target, nameof(Privileges.IUser.Description), claim.Value);
			case ClaimTypes.Email:
				return Reflector.TrySetValue(ref target, nameof(Privileges.IUser.Email), claim.Value);
			case ClaimTypes.MobilePhone:
				return Reflector.TrySetValue(ref target, nameof(Privileges.IUser.Phone), claim.Value);
		}

		return false;
	}

	private static bool SetIdentifier(ref IModel model, Claim claim)
	{
		if(model is IIdentifiable identifiable)
		{
			identifiable.Identifier = new Identifier(Model.GetModelType(model), claim.GetValue());
			return true;
		}

		return false;
	}

	private static bool SetIdentifier(ref object target, Claim claim)
	{
		if(target is IIdentifiable identifiable)
		{
			identifiable.Identifier = new Identifier(Model.GetModelType(target), claim.GetValue());
			return true;
		}

		return false;
	}
	#endregion
}
