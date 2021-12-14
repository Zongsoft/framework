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
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Security;
using Zongsoft.Services;

namespace Zongsoft.Externals.Wechat
{
	public static class CertificateManager
	{
		public static readonly CertificateManager<Certificate> Instance = new CertificateManager<Certificate>("Wechat");
	}

	public class CertificateManager<TCertificate> where TCertificate : ICertificate
	{
		#region 成员字段
		private ICollection<ICertificateProvider<TCertificate>> _providers;
		private readonly ConcurrentDictionary<CertificateKey, TCertificate> _certificates;
		#endregion

		#region 构造函数
		public CertificateManager(string authority, params ICertificateProvider<TCertificate>[] providers)
		{
			if(string.IsNullOrEmpty(authority))
				throw new ArgumentNullException(nameof(authority));

			this.Authority = authority;
			_certificates = new ConcurrentDictionary<CertificateKey, TCertificate>();
			_providers = providers != null && providers.Length > 0 ? providers : null;
		}
		#endregion

		#region 公共属性
		public string Authority { get; }
		public ICollection<TCertificate> Certificates { get => _certificates.Values; }
		public ICollection<ICertificateProvider<TCertificate>> Providers { get => _providers; }
		#endregion

		#region 公共方法
		public TCertificate GetCertificate(string subject, string format = null)
		{
			if(string.IsNullOrEmpty(subject))
				return default;

			var key = new CertificateKey(subject, format);

			if(_certificates.TryGetValue(key, out var certificate))
			{
				if(this.IsValidate(certificate))
					return certificate;

				return _certificates.TryUpdate(key, certificate = this.GetCertificateCore(subject, format), certificate) ? certificate : _certificates[key];
			}
			else
			{
				return _certificates.TryAdd(key, certificate = this.GetCertificateCore(subject, format)) ? certificate : _certificates[key];
			}
		}
		#endregion

		#region 虚拟方法
		protected virtual bool IsValidate(TCertificate certificate) => certificate.Validity.IsValidate();
		protected virtual IEnumerable<ICertificateProvider<TCertificate>> GetProviders() => _providers??= ApplicationContext.Current?.Services?.ResolveAll<ICertificateProvider<TCertificate>>(this.Authority).ToList();
		#endregion

		#region 私有方法
		private TCertificate GetCertificateCore(string subject, string format = null)
		{
			var providers = _providers ?? this.GetProviders();

			if(providers == null)
				return default;

			foreach(var provider in providers)
			{
				var certificate = provider.GetCertificate(subject, format);

				if(certificate != null && this.IsValidate(certificate))
					return certificate;
			}

			return default;
		}
		#endregion

		private struct CertificateKey : IEquatable<CertificateKey>
		{
			public CertificateKey(string subject, string format = null)
			{
				if(string.IsNullOrEmpty(subject))
					throw new ArgumentNullException(nameof(subject));

				this.Subject = subject.ToLowerInvariant();
				this.Format = format?.ToLowerInvariant();
			}

			public readonly string Subject;
			public readonly string Format;

			public bool Equals(CertificateKey other) => string.Equals(this.Subject, other.Subject) && string.Equals(this.Format, other.Format);
			public override bool Equals(object obj) => obj is CertificateKey other && this.Equals(other);
			public override int GetHashCode() => HashCode.Combine(this.Subject, this.Format);
			public override string ToString() => string.IsNullOrEmpty(this.Format) ? this.Subject : $"{this.Subject}!{this.Format}";
		}
	}
}
