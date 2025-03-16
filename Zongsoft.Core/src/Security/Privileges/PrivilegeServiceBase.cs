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
using System.Security.Claims;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

public abstract class PrivilegeServiceBase : IPrivilegeService
{
	#region 保护属性
	protected abstract IDataAccess Accessor { get; }
	protected virtual ClaimsPrincipal Principal => ApplicationContext.Current?.Principal;
	#endregion

	#region 获取方法
	public IAsyncEnumerable<IPrivilege> GetPrivilegesAsync(Identifier identifier, Parameters parameters, CancellationToken cancellation = default) => this.OnGetPrivilegesAsync(identifier, parameters, cancellation);
	#endregion

	#region 设置方法
	public ValueTask<int> SetPrivilegesAsync(Identifier identifier, IEnumerable<IPrivilege> privileges, Parameters parameters, CancellationToken cancellation = default) => this.SetPrivilegesAsync(identifier, privileges, false, parameters, cancellation);
	public async ValueTask<int> SetPrivilegesAsync(Identifier identifier, IEnumerable<IPrivilege> privileges, bool shouldResetting, Parameters parameters, CancellationToken cancellation = default)
	{
		if(identifier.IsEmpty || privileges == null)
			return 0;

		if(shouldResetting)
			await this.OnResetPrivilegesAsync(identifier, parameters, cancellation);

		return await this.OnSetPrivilegesAsync(identifier, privileges, parameters, cancellation);
	}
	#endregion

	#region 抽象方法
	protected abstract IAsyncEnumerable<IPrivilege> OnGetPrivilegesAsync(Identifier identifier, Parameters parameters, CancellationToken cancellation);
	protected abstract ValueTask<int> OnResetPrivilegesAsync(Identifier identifier, Parameters parameters, CancellationToken cancellation);
	protected abstract ValueTask<int> OnSetPrivilegesAsync(Identifier identifier, IEnumerable<IPrivilege> privileges, Parameters parameters, CancellationToken cancellation);
	#endregion
}
