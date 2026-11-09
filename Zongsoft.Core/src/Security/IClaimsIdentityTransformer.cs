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

/// <summary>提供 <see cref="ClaimsIdentity"/> 身份标识转换功能的接口。</summary>
public interface IClaimsIdentityTransformer
{
	/// <summary>确认是否支持对指定的身份标识进行转换。</summary>
	/// <param name="identity">指定要转换的 <see cref="ClaimsIdentity"/> 身份标识对象。</param>
	/// <returns>如果能转换则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	bool CanTransform(ClaimsIdentity identity);

	/// <summary>转换安全身份对象。</summary>
	/// <param name="identity">指定要转换的 <see cref="ClaimsIdentity"/> 身份标识对象。</param>
	/// <returns>返回转换后的对象。</returns>
	object Transform(ClaimsIdentity identity);
}
