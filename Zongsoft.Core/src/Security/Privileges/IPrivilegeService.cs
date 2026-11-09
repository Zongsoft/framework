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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

/// <summary>提供授权定义服务的接口。</summary>
public interface IPrivilegeService
{
	/// <summary>获取权限过滤服务。</summary>
	IPrivilegeService Filtering { get; }

	IAsyncEnumerable<IPrivilege> GetPrivilegesAsync(Identifier identifier, Parameters parameters, CancellationToken cancellation = default);
	IAsyncEnumerable<IPrivilege> GetPrivilegesAsync(IEnumerable<Identifier> identifiers, Parameters parameters, CancellationToken cancellation = default);

	ValueTask<int> SetPrivilegesAsync(Identifier identifier, IEnumerable<IPrivilege> privileges, Parameters parameters, CancellationToken cancellation = default);
	ValueTask<int> SetPrivilegesAsync(Identifier identifier, IEnumerable<IPrivilege> privileges, bool shouldResetting, Parameters parameters, CancellationToken cancellation = default);
}

/// <summary>提供授权定义服务的接口。</summary>
/// <typeparam name="TPrivilege">泛型参数，表示授权定义的类型。</typeparam>
public interface IPrivilegeService<in TPrivilege> : IPrivilegeService where TPrivilege : IPrivilege
{
	ValueTask<int> SetPrivilegesAsync(Identifier identifier, IEnumerable<TPrivilege> privileges, Parameters parameters, CancellationToken cancellation = default);
	ValueTask<int> SetPrivilegesAsync(Identifier identifier, IEnumerable<TPrivilege> privileges, bool shouldResetting, Parameters parameters, CancellationToken cancellation = default);
}
