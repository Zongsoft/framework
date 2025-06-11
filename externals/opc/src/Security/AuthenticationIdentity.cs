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

namespace Zongsoft.Externals.Opc.Security;

public class AuthenticationIdentity
{
	public sealed class Account : AuthenticationIdentity
	{
		public Account(string userName, string password)
		{
			this.UserName = userName;
			this.Password = password;
		}

		public string UserName { get; }
		public string Password { get; }

		public override string ToString() => $"{this.UserName}";
	}

	public sealed class Certificate : AuthenticationIdentity
	{
		public Certificate(X509Certificate2 x509) => this.X509 = x509;
		public X509Certificate2 X509 { get; }
		public override string ToString() => this.X509?.ToString();
	}
}
