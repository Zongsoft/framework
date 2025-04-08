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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

public abstract class PrivilegeServiceBase<TModel> : IPrivilegeService, IMatchable, IMatchable<ClaimsPrincipal>
{
	#region 公共属性
	public virtual string Name => Model.Naming.Get<TModel>();
	#endregion

	#region 保护属性
	protected abstract IDataAccess Accessor { get; }
	protected virtual ClaimsPrincipal Principal => ApplicationContext.Current?.Principal;
	#endregion

	#region 获取方法
	public IAsyncEnumerable<IPrivilege> GetPrivilegesAsync(Identifier identifier, Parameters parameters, CancellationToken cancellation = default) => this.OnGetPrivilegesAsync(identifier, parameters, cancellation);
	public IAsyncEnumerable<IPrivilege> GetPrivilegesAsync(IEnumerable<Identifier> identifiers, Parameters parameters, CancellationToken cancellation = default) => this.OnGetPrivilegesAsync(identifiers, parameters, cancellation);
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

	#region 虚拟方法
	protected virtual IAsyncEnumerable<IPrivilege> OnGetPrivilegesAsync(Identifier identifier, Parameters parameters, CancellationToken cancellation)
	{
		if(identifier.IsEmpty)
			return Zongsoft.Collections.Enumerable.Empty<IPrivilege>();

		var criteria = this.GetCriteria(identifier);
		if(criteria == null)
			return Zongsoft.Collections.Enumerable.Empty<IPrivilege>();

		return this.Accessor.SelectAsync<TModel>(this.Name, criteria, cancellation).Map(model => (IPrivilege)this.Map(model));
	}

	protected virtual IAsyncEnumerable<IPrivilege> OnGetPrivilegesAsync(IEnumerable<Identifier> identifiers, Parameters parameters, CancellationToken cancellation)
	{
		if(identifiers == null)
			return Zongsoft.Collections.Enumerable.Empty<IPrivilege>();

		var criteria = this.GetCriteria(identifiers);
		if(criteria == null)
			return Zongsoft.Collections.Enumerable.Empty<IPrivilege>();

		return this.Accessor.SelectAsync<TModel>(this.Name, criteria, cancellation).Map(model => (IPrivilege)this.Map(model));
	}

	protected virtual ValueTask<int> OnResetPrivilegesAsync(Identifier identifier, Parameters parameters, CancellationToken cancellation)
	{
		if(identifier.IsEmpty)
			return ValueTask.FromResult(0);

		var criteria = this.GetCriteria(identifier);
		if(criteria == null)
			return ValueTask.FromResult(0);

		return this.Accessor.DeleteAsync(this.Name, criteria, cancellation: cancellation);
	}

	protected virtual async ValueTask<int> OnSetPrivilegesAsync(Identifier identifier, IEnumerable<IPrivilege> privileges, Parameters parameters, CancellationToken cancellation)
	{
		if(identifier.IsEmpty || privileges == null)
			return 0;

		int count = 0;

		//找出授权方式为空值的权限
		var requirements = privileges
			.Where(privilege => privilege.IsEmpty)
			.Select(privilege => privilege.Name).ToArray();

		//删除授权方式为空值的权限设置
		if(requirements.Length > 0)
		{
			var criteria = this.GetCriteria(identifier, requirements);

			if(criteria != null)
				count = await this.Accessor.DeleteAsync(this.Name, this.GetCriteria(identifier, requirements), cancellation: cancellation);
		}

		var models = privileges
			.Where(privilege => !privilege.IsEmpty)
			.Select(privilege => this.Map(identifier, privilege));

		return count + await this.Accessor.UpsertManyAsync(this.Name, models, cancellation);
	}
	#endregion

	#region 抽象方法
	protected abstract ICondition GetCriteria(Identifier identifier, params string[] privileges);
	protected abstract ICondition GetCriteria(IEnumerable<Identifier> identifiers);
	protected abstract IPrivilege Map(TModel model);
	protected abstract TModel Map(Identifier identifier, IPrivilege privilege);
	#endregion

	#region 服务匹配
	bool IMatchable.Match(object argument) => argument is ClaimsPrincipal principal && this.OnMatch(principal);
	bool IMatchable<ClaimsPrincipal>.Match(ClaimsPrincipal principal) => this.OnMatch(principal);
	protected virtual bool OnMatch(ClaimsPrincipal principal) => principal != null && principal.Identity != null && principal.Identity.IsAuthenticated;
	#endregion
}
