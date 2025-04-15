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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Zongsoft.Security;

public partial class Certificate
{
	internal class RsaCertificate : ICertificate
	{
		#region 静态字段
		public static readonly ICertificateResolver Resolver = new CertificateResolver();
		#endregion

		#region 成员字段
		private RSA _rsa;
		private RSASignaturer _signaturer;
		#endregion

		#region 构造函数
		internal RsaCertificate(RSA rsa, CertificateDescriptor descriptor)
		{
			_rsa = rsa ?? throw new ArgumentNullException(nameof(rsa));
			this.Identifier = descriptor.Identifier;
			this.Validity = descriptor.Validity;
			this.Issuer = descriptor.Issuer;
			this.Subject = descriptor.Subject;
		}
		#endregion

		#region 公共属性
		public string Identifier { get; }
		public string Format => "RSA";
		public string Algorithm => "SHA256";
		public ICertificateIssuer Issuer { get; }
		public ICertificateSubject Subject { get; }
		public CertificateValidity Validity { get; }
		public ISignaturer Signaturer => _signaturer ??= new RSASignaturer(_rsa, this.Algorithm);
		#endregion

		#region 公共方法
		public object GetProtocol() => _rsa;
		#endregion

		#region 重写方法
		public bool Equals(RsaCertificate other) => other != null && string.Equals(this.Identifier, other.Identifier);
		public bool Equals(ICertificate other) => this.Equals(other as RsaCertificate);
		public override bool Equals(object obj) => obj is RsaCertificate other && this.Equals(other);
		public override int GetHashCode() => this.Identifier.GetHashCode();
		public override string ToString() => $"[{this.Format}]{this.Identifier}:{this.Subject}({this.Issuer})";
		#endregion

		#region 处置方法
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			var rsa = Interlocked.Exchange(ref _rsa, null);

			if(rsa != null)
			{
				_signaturer = null;
				_rsa.Dispose();
			}
		}
		#endregion

		#region 嵌套子类
		private class CertificateResolver : ICertificateResolver
		{
			public string Name => "RSA";

			public ICertificate Resolve(byte[] data, string secret = null, CertificateDescriptor descriptor = default)
			{
				if(data == null || data.Length == 0)
					return null;

				var rsa = RSA.Create();

				if(string.IsNullOrEmpty(secret))
					rsa.ImportFromPem(Encoding.ASCII.GetString(data));
				else
					rsa.ImportFromEncryptedPem(Encoding.ASCII.GetString(data), secret);

				return new RsaCertificate(rsa, descriptor);
			}

			public ICertificate Resolve(string filePath, string secret = null, CertificateDescriptor descriptor = default) => this.Resolve(Zongsoft.IO.FileSystem.File.Open(filePath), secret, descriptor);

			public ICertificate Resolve(Stream stream, string secret = null, CertificateDescriptor descriptor = default)
			{
				if(stream == null)
					throw new ArgumentNullException(nameof(stream));

				var rsa = RSA.Create();

				using(var reader = new StreamReader(stream))
				{
					if(string.IsNullOrEmpty(secret))
						rsa.ImportFromPem(reader.ReadToEnd());
					else
						rsa.ImportFromEncryptedPem(reader.ReadToEnd(), secret);
				}

				return new RsaCertificate(rsa, descriptor);
			}
		}
		#endregion
	}
}
