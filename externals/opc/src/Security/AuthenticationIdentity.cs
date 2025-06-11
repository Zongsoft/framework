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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Security.Cryptography.X509Certificates;

using Opc.Ua;

namespace Zongsoft.Externals.Opc.Security;

public class AuthenticationIdentity
{
	internal static AuthenticationIdentity GetIdentity(UserIdentityToken token) => token switch
	{
		AnonymousIdentityToken => null,
		UserNameIdentityToken user => new Account(user.UserName, user.DecryptedPassword),
		X509IdentityToken x509 => new Certificate(x509.Certificate),
		IssuedIdentityToken => throw new Zongsoft.Security.SecurityException(),
		_ => null,
	};

	public sealed class Account : AuthenticationIdentity, IEquatable<Account>, IEquatable<AuthenticationIdentity>
	{
		private readonly int _hashcode;

		public Account(string userName, string password)
		{
			this.UserName = userName;
			this.Password = password;
			_hashcode = string.IsNullOrEmpty(userName) ? 0 : userName.ToUpperInvariant().GetHashCode();
		}

		public string UserName { get; }
		public string Password { get; }

		public bool Equals(Account other) => other is not null && string.Equals(this.UserName, other.UserName, StringComparison.OrdinalIgnoreCase);
		public bool Equals(AuthenticationIdentity other) => this.Equals(other as Account);
		public override bool Equals(object obj) => this.Equals(obj as Account);
		public override int GetHashCode() => _hashcode;
		public override string ToString() => $"{this.UserName}";
	}

	public sealed class Certificate : AuthenticationIdentity, IEquatable<Certificate>, IEquatable<AuthenticationIdentity>
	{
		public Certificate(X509Certificate2 x509) => this.X509 = x509;
		public X509Certificate2 X509 { get; }

		public bool Equals(Certificate other) => other is not null && object.Equals(this.X509, other.X509);
		public bool Equals(AuthenticationIdentity other) => this.Equals(other as Certificate);
		public override bool Equals(object obj) => this.Equals(obj as Account);
		public override int GetHashCode() => HashCode.Combine(this.X509);
		public override string ToString() => this.X509?.ToString();
	}
}
