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
using System.Threading;
using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using System.Collections.Generic;

namespace Zongsoft.Security;

[DefaultMember(nameof(Transformers))]
public class ClaimsPrincipalTransformer : IClaimsPrincipalTransformer
{
	#region 单例字段
	public static readonly ClaimsPrincipalTransformer Default = new();
	#endregion

	#region 构造函数
	protected ClaimsPrincipalTransformer() => this.Transformers = new List<IClaimsIdentityTransformer>();
	#endregion

	#region 公共属性
	public ICollection<IClaimsIdentityTransformer> Transformers { get; }
	#endregion

	#region 公共方法
	public object Transform(ClaimsPrincipal principal, Func<ClaimsIdentity, object> transform = null)
	{
		if(principal == null)
			return null;

		var type = principal.GetType();
		var result = new Dictionary<string, object>();

		if(type != typeof(ClaimsPrincipal) && type != typeof(GenericPrincipal))
		{
			var properties = type.GetTypeInfo().DeclaredProperties.Where(p =>
				p.CanRead && !p.IsSpecialName &&
				p.GetMethod.IsPublic && !p.GetMethod.IsStatic
			);

			foreach(var property in properties)
			{
				if(property.PropertyType == typeof(CancellationToken) ||
				   property.PropertyType == typeof(CancellationTokenSource) ||
				   typeof(Microsoft.Extensions.Primitives.IChangeToken).IsAssignableFrom(property.PropertyType))
					continue;

				if(property.PropertyType == typeof(TimeSpan))
					result.Add(property.Name, Reflection.Reflector.GetValue(property, ref principal).ToString());
				else
					result.Add(property.Name, Reflection.Reflector.GetValue(property, ref principal));
			}
		}

		if(principal.Identity != null)
		{
			if(transform == null)
				result.Add(nameof(ClaimsPrincipal.Identity), this.OnTransform((principal.Identity as ClaimsIdentity) ?? new ClaimsIdentity(principal.Identity)));
			else
				result.Add(nameof(ClaimsPrincipal.Identity), transform((principal.Identity as ClaimsIdentity) ?? new ClaimsIdentity(principal.Identity)));
		}

		if(principal.Identities != null && principal.Identities.Any())
		{
			var identities = principal.Identities
				.Where(identity => identity != principal.Identity)
				.Select(identity => new
				{
					Scheme = identity.AuthenticationType,
					Identity = transform == null ? this.OnTransform(identity) : transform(identity)
				});

			if(identities.Any())
				result.Add(nameof(ClaimsPrincipal.Identities), identities);
		}

		return result;
	}
	#endregion

	#region 虚拟方法
	protected virtual object OnTransform(ClaimsIdentity identity)
	{
		if(identity == null)
			return null;

		foreach(var transformer in this.Transformers)
		{
			if(transformer.CanTransform(identity))
				return transformer.Transform(identity);
		}

		return identity.Claims
			.Select(claim => new KeyValuePair<string, string>(claim.Type, claim.Value));
	}
	#endregion
}
