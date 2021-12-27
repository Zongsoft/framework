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
using System.Reflection;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;

using Zongsoft.Services;

namespace Zongsoft.Security
{
	public class ClaimsPrincipalTransformer : IClaimsPrincipalTransformer
	{
		#region 单例字段
		public static readonly ClaimsPrincipalTransformer Default = new ClaimsPrincipalTransformer();
		#endregion

		#region 构造函数
		protected ClaimsPrincipalTransformer() { }
		#endregion

		#region 公共方法
		public object Transform(ClaimsPrincipal principal, Func<ClaimsIdentity, object> transform = null)
		{
			if(principal == null)
				return null;

			var type = principal.GetType();
			var dictionary = new Dictionary<string, object>();

			if(type != typeof(ClaimsPrincipal) && type != typeof(GenericPrincipal))
			{
				var properties = type.GetTypeInfo().DeclaredProperties.Where(p =>
					p.CanRead && !p.IsSpecialName &&
					p.GetMethod.IsPublic && !p.GetMethod.IsStatic
				);

				foreach(var property in properties)
				{
					if(property.PropertyType == typeof(TimeSpan))
						dictionary.Add(property.Name, Reflection.Reflector.GetValue(property, ref principal).ToString());
					else
						dictionary.Add(property.Name, Reflection.Reflector.GetValue(property, ref principal));
				}
			}

			if(principal.Identity != null)
			{
				if(transform == null)
					dictionary.Add(nameof(ClaimsPrincipal.Identity), this.TransformIdentity((principal.Identity as ClaimsIdentity) ?? new ClaimsIdentity(principal.Identity)));
				else
					dictionary.Add(nameof(ClaimsPrincipal.Identity), transform((principal.Identity as ClaimsIdentity) ?? new ClaimsIdentity(principal.Identity)));
			}

			if(principal.Identities != null)
				dictionary.Add(nameof(ClaimsPrincipal.Identities), principal.Identities
					.Where(identity => identity != principal.Identity)
					.Select(identity => transform == null ? this.TransformIdentity(identity) : transform(identity)));

			return dictionary;
		}
		#endregion

		#region 虚拟方法
		protected virtual object TransformIdentity(ClaimsIdentity identity)
		{
			return this.TransformIdentity(identity.Claims.FirstOrDefault(claim => string.Equals(claim.Type, ClaimTypes.System))?.Value, identity);
		}

		protected virtual object TransformIdentity(string name, ClaimsIdentity identity)
		{
			var transformers = ApplicationContext.Current?.Services.ResolveAll<IClaimsIdentityTransformer>(name);

			foreach(var transformer in transformers)
			{
				if(transformer.CanTransform(identity))
					return transformer.Transform(identity);
			}

			return identity.AsModel<Membership.IUser>();
		}
		#endregion
	}
}
