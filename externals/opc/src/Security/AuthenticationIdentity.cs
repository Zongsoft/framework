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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Text;
using System.Security.Cryptography.X509Certificates;

using Opc.Ua;

namespace Zongsoft.Externals.Opc.Security;

public class AuthenticationIdentity
{
	#region 内部方法
	internal static AuthenticationIdentity GetIdentity(IUserIdentityTokenHandler token) => token switch
	{
		AnonymousIdentityTokenHandler => null,
		UserNameIdentityTokenHandler user => new Account(user.UserName, user.DecryptedPassword == null ? string.Empty : Encoding.UTF8.GetString(user.DecryptedPassword)),
		X509IdentityTokenHandler x509 => new Certificate(((X509IdentityToken)x509.Token).CertificateData.ToArray()),
		IssuedIdentityTokenHandler => throw new Zongsoft.Security.SecurityException(),
		_ => null,
	};
	#endregion

	public sealed class Account : AuthenticationIdentity, IEquatable<Account>, IEquatable<AuthenticationIdentity>
	{
		private readonly int _hashcode;

		public Account(string userName, string password)
		{
			this.UserName = userName;
			this.Password = password;
			_hashcode = string.IsNullOrEmpty(userName) ? 0 : userName.ToUpperInvariant().GetHashCode();
		}

		#region 公共属性
		public string UserName { get; }
		public string Password { get; }
		#endregion

		#region 公共方法
		public bool Equals(Account other) => other is not null && string.Equals(this.UserName, other.UserName, StringComparison.OrdinalIgnoreCase);
		public bool Equals(AuthenticationIdentity other) => this.Equals(other as Account);
		#endregion
		#region 重写方法
		public override bool Equals(object obj) => this.Equals(obj as Account);
		public override int GetHashCode() => _hashcode;
		public override string ToString() => $"{this.UserName}";
		#endregion
	}

	public sealed class Certificate : AuthenticationIdentity, IDisposable, IEquatable<Certificate>, IEquatable<AuthenticationIdentity>
	{
		#region 构造与资源
		private readonly bool _owned;
		public Certificate(X509Certificate2 x509) => this.X509 = x509;
		internal Certificate(byte[] data)
		{
#if NET9_0_OR_GREATER
			this.X509 = X509CertificateLoader.LoadCertificate(data);
#else
			this.X509 = new X509Certificate2(data);
#endif
			_owned = true;
		}

		public void Dispose()
		{
			if(_owned)
				this.X509?.Dispose();
		}
		#endregion

		#region 公共属性
		public X509Certificate2 X509 { get; }
		#endregion

		#region 公共方法
		public bool Equals(Certificate other) => other is not null && object.Equals(this.X509, other.X509);
		public bool Equals(AuthenticationIdentity other) => this.Equals(other as Certificate);
		#endregion
		#region 重写方法
		public override bool Equals(object obj) => this.Equals(obj as Certificate);
		public override int GetHashCode() => HashCode.Combine(this.X509);
		public override string ToString() => this.X509?.ToString();
		#endregion
	}
}
