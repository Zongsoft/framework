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

public abstract class PrivilegeServiceBase<TPrivilege> : IPrivilegeService<TPrivilege>, IPrivilegeService, IMatchable, IMatchable<ClaimsPrincipal> where TPrivilege : IPrivilege
{
	#region 公共属性
	public IPrivilegeService Filtering { get; protected init; }
	#endregion

	#region 保护属性
	protected abstract IDataAccess Accessor { get; }
	protected virtual ClaimsPrincipal Principal => ApplicationContext.Current?.Principal;
	#endregion

	#region 获取方法
	public IAsyncEnumerable<IPrivilege> GetPrivilegesAsync(Identifier identifier, Parameters parameters, CancellationToken cancellation = default)
	{
		if(identifier.IsEmpty)
			return Zongsoft.Collections.Enumerable.Empty<IPrivilege>();

		var criteria = this.GetCriteria(identifier);
		if(criteria == null)
			return Zongsoft.Collections.Enumerable.Empty<IPrivilege>();

		return this.OnGetPrivilegesAsync(criteria, parameters, cancellation).Map(privilege => (IPrivilege)privilege);
	}

	public IAsyncEnumerable<IPrivilege> GetPrivilegesAsync(IEnumerable<Identifier> identifiers, Parameters parameters, CancellationToken cancellation = default)
	{
		if(identifiers == null)
			return Zongsoft.Collections.Enumerable.Empty<IPrivilege>();

		var criteria = this.GetCriteria(identifiers);
		if(criteria == null)
			return Zongsoft.Collections.Enumerable.Empty<IPrivilege>();

		return this.OnGetPrivilegesAsync(criteria, parameters, cancellation).Map(privilege => (IPrivilege)privilege);
	}
	#endregion

	#region 设置方法
	ValueTask<int> IPrivilegeService.SetPrivilegesAsync(Identifier identifier, IEnumerable<IPrivilege> privileges, Parameters parameters, CancellationToken cancellation) => this.SetPrivilegesAsync(identifier, privileges.Cast<TPrivilege>(), false, parameters, cancellation);
	ValueTask<int> IPrivilegeService.SetPrivilegesAsync(Identifier identifier, IEnumerable<IPrivilege> privileges, bool shouldResetting, Parameters parameters, CancellationToken cancellation) => this.SetPrivilegesAsync(identifier, privileges.Cast<TPrivilege>(), shouldResetting, parameters, cancellation);

	public ValueTask<int> SetPrivilegesAsync(Identifier identifier, IEnumerable<TPrivilege> privileges, Parameters parameters, CancellationToken cancellation = default) => this.SetPrivilegesAsync(identifier, privileges, false, parameters, cancellation);
	public async ValueTask<int> SetPrivilegesAsync(Identifier identifier, IEnumerable<TPrivilege> privileges, bool shouldResetting, Parameters parameters, CancellationToken cancellation = default)
	{
		if(identifier.IsEmpty || privileges == null)
			return 0;

		int count = 0;

		if(shouldResetting)
		{
			var criteria = this.GetCriteria(identifier);

			if(criteria != null)
				await this.OnResetPrivilegesAsync(criteria, parameters, cancellation);
		}
		else
		{
			//找出授权方式为空值的权限
			var requirements = privileges
				.Where(privilege => privilege != null && privilege.IsEmpty)
				.Select(privilege => privilege.Name).ToArray();

			//删除授权方式为空值的权限设置
			if(requirements.Length > 0)
			{
				var criteria = this.GetCriteria(identifier, requirements);

				if(criteria != null)
					count = await this.OnResetPrivilegesAsync(criteria, parameters, cancellation);
			}
		}

		return count + await this.OnSetPrivilegesAsync(identifier, privileges.Where(privilege => privilege != null && !privilege.IsEmpty), parameters, cancellation);
	}
	#endregion

	#region 虚拟方法
	protected virtual IAsyncEnumerable<TPrivilege> OnGetPrivilegesAsync(ICondition criteria, Parameters parameters, CancellationToken cancellation)
	{
		return this.Accessor.SelectAsync<TPrivilege>(criteria, new DataSelectOptions(parameters), cancellation);
	}

	protected virtual ValueTask<int> OnResetPrivilegesAsync(ICondition criteria, Parameters parameters, CancellationToken cancellation)
	{
		return this.Accessor.DeleteAsync<TPrivilege>(criteria, new DataDeleteOptions(parameters), cancellation);
	}

	protected virtual ValueTask<int> OnSetPrivilegesAsync(Identifier identifier, IEnumerable<TPrivilege> privileges, Parameters parameters, CancellationToken cancellation)
	{
		if(identifier.IsEmpty || privileges == null)
			return ValueTask.FromResult(0);

		return this.Accessor.UpsertManyAsync(privileges, cancellation);
	}
	#endregion

	#region 抽象方法
	protected abstract ICondition GetCriteria(Identifier identifier, params string[] privileges);
	protected abstract ICondition GetCriteria(IEnumerable<Identifier> identifiers);
	#endregion

	#region 服务匹配
	bool IMatchable.Match(object argument) => argument is ClaimsPrincipal principal && this.OnMatch(principal);
	bool IMatchable<ClaimsPrincipal>.Match(ClaimsPrincipal principal) => this.OnMatch(principal);
	protected virtual bool OnMatch(ClaimsPrincipal principal) => principal != null && principal.Identity != null && principal.Identity.IsAuthenticated;
	#endregion
}
