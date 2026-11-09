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

namespace Zongsoft.Security;

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
