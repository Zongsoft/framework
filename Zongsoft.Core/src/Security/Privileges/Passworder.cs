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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Security.Privileges;

/// <summary>提供用户密码设置和验证功能的基类。</summary>
public abstract class Passworder
{
	/// <summary>设置用户密码；空密码表示清除已设置的密码。</summary>
	/// <param name="identifier">用户标识。</param>
	/// <param name="password">密码明文；空引用或空字符串表示清除密码。</param>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>是否成功更新用户密码。</returns>
	public abstract ValueTask<bool> SetAsync(Identifier identifier, string password, CancellationToken cancellation = default);

	/// <summary>验证指定用户的密码；用户不存在或密码不匹配时返回失败。</summary>
	/// <param name="identifier">用户标识。</param>
	/// <param name="password">待验证的密码明文。</param>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>用户存在且密码匹配时返回成功；存储密码为空时，仅空引用或空字符串验证成功。</returns>
	/// <exception cref="AuthenticationException">账号已禁用。</exception>
	public abstract ValueTask<bool> VerifyAsync(Identifier identifier, string password, CancellationToken cancellation = default);

	/// <summary>验证账号密码，成功时返回用户标识，密码不匹配时返回空标识。</summary>
	/// <param name="identity">登录账号。</param>
	/// <param name="namespace">账号所属的命名空间。</param>
	/// <param name="password">待验证的密码明文。</param>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>密码匹配时返回用户标识，否则返回空标识；存储密码为空时，仅空引用或空字符串验证成功。</returns>
	/// <exception cref="AuthenticationException">账号不存在或已禁用。</exception>
	public abstract ValueTask<Identifier> VerifyAsync(string identity, string @namespace, string password, CancellationToken cancellation = default);
}
