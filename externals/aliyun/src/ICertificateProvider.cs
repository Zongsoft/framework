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
using System.Collections.Generic;

namespace Zongsoft.Externals.Aliyun;

/// <summary>表示阿里云访问凭证提供程序的接口。</summary>
public interface ICertificateProvider : IEnumerable<ICertificate>
{
	/// <summary>获取凭证数量。</summary>
	int Count { get; }

	/// <summary>获取默认凭证。</summary>
	ICertificate Default { get; }

	/// <summary>获取指定名称的凭证。</summary>
	/// <param name="name">指定要获取的凭证名称。</param>
	/// <returns>返回指定名称的凭证，如果不存在则返回空(<c>null</c>)。</returns>
	ICertificate GetCertificate(string name);
}
