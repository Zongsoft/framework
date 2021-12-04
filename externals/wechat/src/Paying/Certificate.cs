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
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Security.Cryptography;

using Zongsoft.Security;

namespace Zongsoft.Externals.Wechat.Paying
{
	public class Certificate : ICertificate, IEquatable<Certificate>, IDisposable
	{
		#region 私有变量
		private RSA _rsa;
		#endregion

		#region 构造函数
		public Certificate(string code, string name, string format, byte[] privateKey = null, byte[] publicKey = null)
		{
			this.Code = code;
			this.Name = name ?? code;
			this.Format = format;
			this.PublicKey = publicKey;
			this.PrivateKey = privateKey;

			_rsa = RSA.Create();

			if(publicKey != null)
				_rsa.ImportRSAPublicKey(publicKey, out _);
			if(privateKey != null)
				_rsa.ImportPkcs8PrivateKey(privateKey, out _);
		}
		#endregion

		#region 公共属性
		public string Code { get; }
		public string Name { get; }
		public string Format { get; }
		public string Algorithm { get => "SHA256"; }
		public ICertificateIssuer Issuer { get; set; }
		public ICertificateSubject Subject { get; set; }
		public CertificateValidity Validity { get; set; }

		public byte[] PublicKey { get; private set; }
		public byte[] PrivateKey { get; private set; }
		#endregion

		#region 签名方法
		public byte[] Signature(ReadOnlySpan<byte> data)
		{
			var rsa = _rsa;

			if(rsa == null)
				throw new ObjectDisposedException(nameof(Certificate));

			return rsa.SignData(data.ToArray(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		}
		#endregion

		#region 重写方法
		public bool Equals(Certificate other) => string.Equals(this.Code, other.Code);
		public bool Equals(ICertificate other) => string.Equals(this.Code, other.Code);
		public override bool Equals(object obj) => obj is ICertificate other && this.Equals(other);
		public override int GetHashCode() => this.Code.GetHashCode();
		public override string ToString() => string.Equals(this.Code, this.Name) ?
			$"{this.Code}:{this.Issuer}:{this.Subject}({this.Format})" :
			$"{this.Code}:{this.Issuer}:{this.Subject}@{this.Name}({this.Format})";
		#endregion

		#region 处置方法
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			var rsa = System.Threading.Interlocked.Exchange(ref _rsa, null);

			if(rsa != null)
				rsa.Dispose();

			this.PublicKey = null;
			this.PrivateKey = null;
		}
		#endregion
	}

	public class CertificateIssuer : ICertificateIssuer
	{
		public CertificateIssuer(string identifier, string name)
		{
			this.Identifier = identifier;
			this.Name = name;
		}

		public string Identifier { get; }
		public string Name { get; }
	}

	public class CertificateSubject : ICertificateSubject
	{
		public CertificateSubject(string identifier, string name)
		{
			this.Identifier = identifier;
			this.Name = name;
		}

		public string Identifier { get; }
		public string Name { get; }
	}
}
