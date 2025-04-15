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
using System.Security.Claims;

namespace Zongsoft.Security;

/// <summary>
/// 提供 <see cref="ClaimsIdentity"/> 身份标识转换功能的接口。
/// </summary>
public interface IClaimsIdentityTransformer
{
	/// <summary>确认是否支持对指定的身份标识进行转换。</summary>
	/// <param name="identity">指定要转换的 <see cref="ClaimsIdentity"/> 身份标识对象。</param>
	/// <returns>如果能转换则返回真(True)，否则返回假(False)。</returns>
	bool CanTransform(ClaimsIdentity identity);

	/// <summary>转换安全身份对象。</summary>
	/// <param name="identity">指定要转换的 <see cref="ClaimsIdentity"/> 身份标识对象。</param>
	/// <returns>返回转换后的对象。</returns>
	object Transform(ClaimsIdentity identity);
}
