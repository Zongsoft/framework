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

namespace Zongsoft.Security
{
	public struct CertificateDescriptor
	{
		#region 构造函数
		public CertificateDescriptor(string identifier, CertificateValidity validity = default, ICertificateIssuer issuer = null, ICertificateSubject subject = null)
		{
			this.Identifier = identifier;
			this.Validity = validity;
			this.Issuer = issuer;
			this.Subject = subject;
		}
		#endregion

		#region 公共属性
		/// <summary>获取证书标识。</summary>
		public string Identifier { get; }

		/// <summary>获取证书签发者信息。</summary>
		public ICertificateIssuer Issuer { get; }

		/// <summary>获取证书持有者信息。</summary>
		public ICertificateSubject Subject { get; }

		/// <summary>获取证书有效期。</summary>
		public CertificateValidity Validity { get; }
		#endregion
	}
}
