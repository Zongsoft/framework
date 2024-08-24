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
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Zongsoft.Security
{
	public partial class Certificate
	{
		internal class X509 : ICertificate
		{
			#region 静态字段
			public static readonly ICertificateResolver Resolver = new CertificateResolver();
			#endregion

			#region 成员字段
			private X509Certificate2 _certificate;
			private RSASignaturer _signaturer;
			#endregion

			#region 构造函数
			internal X509(X509Certificate2 certificate, CertificateDescriptor descriptor)
			{
				_certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
				this.Issuer = descriptor.Issuer ?? new CertificateIssuer(_certificate.Issuer, _certificate.IssuerName?.Name);
				this.Subject = descriptor.Subject ?? new CertificateSubject(_certificate.Subject, _certificate.SubjectName?.Name);
			}
			#endregion

			#region 公共属性
			public string Identifier => _certificate.SerialNumber;
			public string Name => _certificate.FriendlyName;
			public string Format => "X509";
			public string Algorithm => "SHA256";
			public ICertificateIssuer Issuer { get; }
			public ICertificateSubject Subject { get; }
			public CertificateValidity Validity => new CertificateValidity(_certificate.NotBefore, _certificate.NotAfter);
			public ISignaturer Signaturer => _signaturer ??= new RSASignaturer(_certificate.GetRSAPrivateKey(), this.Algorithm);
			#endregion

			#region 公共方法
			public object GetProtocol() => _certificate;
			#endregion

			#region 重写方法
			public bool Equals(X509 other) => other != null && string.Equals(this.Identifier, other.Identifier);
			public bool Equals(ICertificate other) => this.Equals(other as X509);
			public override bool Equals(object obj) => obj is X509 other && this.Equals(other);
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
				var certificate = Interlocked.Exchange(ref _certificate, null);

				if(certificate != null)
				{
					_signaturer = null;
					certificate.Dispose();
				}
			}
			#endregion

			#region 嵌套子类
			private class CertificateResolver : ICertificateResolver
			{
				public string Name => "X509";

				public ICertificate Resolve(byte[] data, string secret = null, CertificateDescriptor descriptor = default) => new X509(string.IsNullOrEmpty(secret) ? new X509Certificate2(data) : new X509Certificate2(data, secret), descriptor);
				public ICertificate Resolve(string filePath, string secret = null, CertificateDescriptor descriptor = default) => this.Resolve(Zongsoft.IO.FileSystem.File.Open(filePath), secret, descriptor);

				public ICertificate Resolve(Stream stream, string secret = null, CertificateDescriptor descriptor = default)
				{
					if(stream == null)
						throw new ArgumentNullException(nameof(stream));

					var data = stream.CanSeek ? new List<byte>((int)stream.Length) : new List<byte>();
					var buffer = new byte[1024];
					int bytesRead;

					while((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
					{
						if(bytesRead == buffer.Length)
							data.AddRange(buffer);
						else
							data.AddRange(buffer.AsSpan(0, bytesRead).ToArray());
					}

					return new X509(string.IsNullOrEmpty(secret) ? new X509Certificate2(data.ToArray()) : new X509Certificate2(data.ToArray(), secret), descriptor);
				}
			}
			#endregion
		}
	}
}
