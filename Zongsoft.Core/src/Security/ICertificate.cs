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

namespace Zongsoft.Security
{
	/// <summary>
	/// 表示数字证书的接口。
	/// </summary>
	public interface ICertificate : IEquatable<ICertificate>
	{
		/// <summary>获取证书号码。</summary>
		string Code { get; }

		/// <summary>获取证书名称。</summary>
		string Name { get; }

		/// <summary>获取证书格式。</summary>
		string Format { get; }

		/// <summary>获取证书签名算法。</summary>
		string Algorithm { get; }

		/// <summary>获取或设置证书签发者信息。</summary>
		ICertificateIssuer Issuer { get; set; }

		/// <summary>获取或设置证书持有者信息。</summary>
		ICertificateSubject Subject { get; set; }

		/// <summary>获取或设置证书有效期。</summary>
		CertificateValidity Validity { get; set; }
	}

	/// <summary>
	/// 表示数字证书有效期的结构。
	/// </summary>
	public struct CertificateValidity : IEquatable<CertificateValidity>
	{
		#region 构造函数
		public CertificateValidity(DateTime start, DateTime final)
		{
			this.Start = start.ToUniversalTime();
			this.Final = final.ToUniversalTime();
		}
		#endregion

		#region 公共字段
		/// <summary>生效时间。</summary>
		public DateTime Start;

		/// <summary>过期时间。</summary>
		public DateTime Final;
		#endregion

		#region 公共方法
		public bool IsValidate(DateTime? timestamp = null)
		{
			var time = timestamp == null ? DateTime.UtcNow : timestamp.Value.ToUniversalTime();
			return this.Start <= time && this.Final >= time;
		}
		#endregion

		#region 重写方法
		public bool Equals(CertificateValidity other) => this.Start == other.Start && this.Final == other.Final;
		public override bool Equals(object obj) => obj is CertificateValidity other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Start, this.Final);
		public override string ToString() => $"{this.Start}~{this.Final}";
		#endregion

		#region 符号重写
		public static bool operator ==(CertificateValidity left, CertificateValidity right) => left.Equals(right);
		public static bool operator !=(CertificateValidity left, CertificateValidity right) => !(left == right);

		public static implicit operator Data.Range<DateTime>(CertificateValidity validity) => Data.Range.Create<DateTime>(validity.Start, validity.Final);
		public static implicit operator Data.Range<DateTimeOffset>(CertificateValidity validity) => Data.Range.Create<DateTimeOffset>(validity.Start, validity.Final);
		#endregion
	}

	/// <summary>
	/// 表示数字证书签发者的接口。
	/// </summary>
	public interface ICertificateIssuer
	{
		/// <summary>获取表示身份唯一性的标识。</summary>
		string Identifier { get; }

		/// <summary>获取主体名称。</summary>
		string Name { get; }
	}

	/// <summary>
	/// 表示数字证书持有者的接口。
	/// </summary>
	public interface ICertificateSubject
	{
		/// <summary>获取表示身份唯一性的标识。</summary>
		string Identifier { get; }

		/// <summary>获取主体名称。</summary>
		string Name { get; }
	}
}
