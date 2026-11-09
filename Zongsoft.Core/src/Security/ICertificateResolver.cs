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
using System.IO;

namespace Zongsoft.Security;

/// <summary>提供数字证书解析功能的接口。</summary>
public interface ICertificateResolver
{
	/// <summary>获取数字证书解析器名称，通常对应数字证书的格式名。</summary>
	string Name { get; }

	ICertificate Resolve(byte[] data, string secret = null, CertificateDescriptor descriptor = default);
	ICertificate Resolve(Stream stream, string secret = null, CertificateDescriptor descriptor = default);
	ICertificate Resolve(string filePath, string secret = null, CertificateDescriptor descriptor = default);
}
