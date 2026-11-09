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
using System.Security.Claims;

namespace Zongsoft.Security;

/// <summary>提供 <see cref="ClaimsPrincipal"/> 安全主体转换功能的接口。</summary>
public interface IClaimsPrincipalTransformer
{
	/// <summary>转换安全主体对象。</summary>
	/// <param name="principal">指定要转换的 <see cref="ClaimsPrincipal"/> 安全主体对象。</param>
	/// <param name="transform">指定的 <see cref="ClaimsIdentity"/> 身份标识的转换函数。</param>
	/// <returns>返回转换后的对象。</returns>
	object Transform(ClaimsPrincipal principal, Func<ClaimsIdentity, object> transform = null);
}
