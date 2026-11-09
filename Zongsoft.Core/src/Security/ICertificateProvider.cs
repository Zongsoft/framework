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

/// <summary>表示数字证书提供程序的接口。</summary>
[Obsolete("This interface has been deprecated, Please use the CertificateManager class instead.")]
public interface ICertificateProvider<out TCertificate> where TCertificate : ICertificate
{
	/// <summary>获取数字证书提供程序名称。</summary>
	string Name { get; }

	/// <summary>获取一个符合参数的数字证书。</summary>
	/// <param name="subject">指定要获取的主体标识。</param>
	/// <param name="format">指定要获取的证书格式。</param>
	/// <returns>返回指定条件的证书对象，如果没有找到则返回空(<c>null</c>)。</returns>
	TCertificate GetCertificate(string subject, string format = null);
}
