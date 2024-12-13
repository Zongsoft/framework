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
using System.Reflection;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;

namespace Zongsoft.Security
{
	[DefaultMember(nameof(Transformers))]
	public class ClaimsPrincipalTransformer : IClaimsPrincipalTransformer
	{
		#region 单例字段
		public static readonly ClaimsPrincipalTransformer Default = new ClaimsPrincipalTransformer();
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
					dictionary.Add(nameof(ClaimsPrincipal.Identity), this.OnTransform((principal.Identity as ClaimsIdentity) ?? new ClaimsIdentity(principal.Identity)));
				else
					dictionary.Add(nameof(ClaimsPrincipal.Identity), transform((principal.Identity as ClaimsIdentity) ?? new ClaimsIdentity(principal.Identity)));
			}

			if(principal.Identities != null)
			{
				var identities = principal.Identities
					.Where(identity => identity != principal.Identity)
					.Select(identity => new
					{
						Scheme = identity.AuthenticationType,
						Identity = transform == null ? this.OnTransform(identity) : transform(identity)
					});

				dictionary.Add(nameof(ClaimsPrincipal.Identities), identities);
			}

			return new Result(dictionary);
		}
		#endregion

		#region 虚拟方法
		protected virtual object OnTransform(ClaimsIdentity identity)
		{
			foreach(var transformer in this.Transformers)
			{
				if(transformer.CanTransform(identity))
					return transformer.Transform(identity);
			}

			return identity.AsModel<Membership.IUserModel>();
		}
		#endregion

		[System.Text.Json.Serialization.JsonConverter(typeof(Result.ResultConverter))]
		private sealed class Result(IDictionary<string, object> dictionary)
		{
			private readonly IDictionary<string, object> Dictionary = dictionary;
			private bool IsEmpty => this.Dictionary == null || this.Dictionary.Count == 0;

			public sealed class ResultConverter : System.Text.Json.Serialization.JsonConverter<Result>
			{
				public override Result Read(ref System.Text.Json.Utf8JsonReader reader, Type type, System.Text.Json.JsonSerializerOptions options)
				{
					var dictionary = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
					return new(dictionary);
				}

				public override void Write(System.Text.Json.Utf8JsonWriter writer, Result value, System.Text.Json.JsonSerializerOptions options)
				{
					if(value == null || value.IsEmpty)
					{
						writer.WriteNullValue();
						return;
					}

					writer.WriteStartObject();

					foreach(var entry in value.Dictionary)
					{
						Serialization.JsonWriterExtension.WritePropertyName(writer, entry.Key, options);
						System.Text.Json.JsonSerializer.Serialize(writer, entry.Value, options);
					}

					writer.WriteEndObject();
				}
			}
		}
	}
}
