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

namespace Zongsoft.Security.Membership
{
	public readonly struct IdentityQualifier(string @namespace, string identity) : IEquatable<IdentityQualifier>
	{
		#region 公共字段
		public readonly string Namespace = string.IsNullOrWhiteSpace(@namespace) ? null : @namespace.Trim();
		public readonly string Identity = string.IsNullOrWhiteSpace(identity) ? null : identity.Trim();
		#endregion

		#region 静态方法
		public static IdentityQualifier Parse(string identifier)
		{
			if(string.IsNullOrWhiteSpace(identifier))
				throw new ArgumentNullException(nameof(identifier));

			return TryParse(identifier, out var result) ? result : throw new ArgumentException($"Illegal argument value: {identifier}", nameof(identifier));
		}

		public static bool TryParse(string identifier, out IdentityQualifier identity)
		{
			identity = default;
			if(string.IsNullOrWhiteSpace(identifier))
				return false;

			var separator = identifier.IndexOf(':');

			identity = separator switch
			{
				0 => new IdentityQualifier(null, identifier[(separator + 1)..]),
				-1 => new IdentityQualifier(null, identifier),
				_ => separator == identifier.Length - 1 ?
					new IdentityQualifier(identifier[..separator], null) :
					new IdentityQualifier(identifier[..separator], identifier[(separator + 1)..]),
			};

			return true;
		}
		#endregion

		#region 重写方法
		public bool Equals(IdentityQualifier identity) =>
			string.Equals(this.Namespace, identity.Namespace, StringComparison.OrdinalIgnoreCase) &&
			string.Equals(this.Identity, identity.Identity, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object obj) => obj is IdentityQualifier other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Namespace, this.Identity);
		public override string ToString() => string.IsNullOrEmpty(this.Namespace) ? this.Identity : $"{this.Namespace}:{this.Identity}";
		#endregion

		#region 符号重写
		public static bool operator ==(IdentityQualifier left, IdentityQualifier right) => left.Equals(right);
		public static bool operator !=(IdentityQualifier left, IdentityQualifier right) => !(left == right);
		#endregion
	}
}