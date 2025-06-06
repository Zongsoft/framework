﻿/*
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
using System.Collections.Generic;

using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

/// <summary>
/// 提供授权定义服务的接口。
/// </summary>
public interface IPrivilegeService
{
	/// <summary>获取权限过滤服务。</summary>
	IPrivilegeService Filtering { get; }

	IAsyncEnumerable<IPrivilege> GetPrivilegesAsync(Identifier identifier, Parameters parameters, CancellationToken cancellation = default);
	IAsyncEnumerable<IPrivilege> GetPrivilegesAsync(IEnumerable<Identifier> identifiers, Parameters parameters, CancellationToken cancellation = default);

	ValueTask<int> SetPrivilegesAsync(Identifier identifier, IEnumerable<IPrivilege> privileges, Parameters parameters, CancellationToken cancellation = default);
	ValueTask<int> SetPrivilegesAsync(Identifier identifier, IEnumerable<IPrivilege> privileges, bool shouldResetting, Parameters parameters, CancellationToken cancellation = default);
}

/// <summary>
/// 提供授权定义服务的接口。
/// </summary>
/// <typeparam name="TPrivilege">泛型参数，表示授权定义的类型。</typeparam>
public interface IPrivilegeService<in TPrivilege> : IPrivilegeService where TPrivilege : IPrivilege
{
	ValueTask<int> SetPrivilegesAsync(Identifier identifier, IEnumerable<TPrivilege> privileges, Parameters parameters, CancellationToken cancellation = default);
	ValueTask<int> SetPrivilegesAsync(Identifier identifier, IEnumerable<TPrivilege> privileges, bool shouldResetting, Parameters parameters, CancellationToken cancellation = default);
}
