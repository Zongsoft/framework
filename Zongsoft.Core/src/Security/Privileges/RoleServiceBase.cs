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
using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

public abstract class RoleServiceBase<TRole> : IRoleService<TRole>, IRoleService, IMatchable, IMatchable<ClaimsPrincipal> where TRole : IRole
{
	#region 公共属性
	public virtual string Name => Model.Naming.Get<TRole>();
	#endregion

	#region 保护属性
	internal protected abstract IDataAccess Accessor { get; }
	protected virtual IServiceProvider Services => ApplicationContext.Current?.Services;
	protected virtual ClaimsPrincipal Principal => ApplicationContext.Current?.Principal;
	#endregion

	#region 公共方法
	public ValueTask<TRole> GetAsync(Identifier identifier, CancellationToken cancellation = default) => this.GetAsync(identifier, null, cancellation);
	public async ValueTask<TRole> GetAsync(Identifier identifier, string schema, CancellationToken cancellation = default)
	{
		if(identifier.IsEmpty)
			return default;

		var criteria = this.GetCriteria(identifier);
		if(criteria == null)
			return default;

		return await this.Accessor.SelectAsync<TRole>(criteria, schema, cancellation).FirstOrDefault(cancellation);
	}

	public IAsyncEnumerable<TRole> FindAsync(string keyword, string schema, Paging paging, CancellationToken cancellation = default) => this.FindAsync(this.GetCriteria(keyword), schema, paging, cancellation);
	public IAsyncEnumerable<TRole> FindAsync(ICondition criteria, string schema, Paging paging, CancellationToken cancellation = default) => this.Accessor.SelectAsync<TRole>(criteria, schema, paging, cancellation);

	public ValueTask<bool> ExistsAsync(Identifier identifier, CancellationToken cancellation = default)
	{
		var criteria = this.GetCriteria(identifier);
		if(criteria == null)
			return ValueTask.FromResult(false);

		return this.Accessor.ExistsAsync(this.Name, criteria, cancellation: cancellation);
	}

	public virtual async ValueTask<bool> EnableAsync(Identifier identifier, CancellationToken cancellation = default)
	{
		//确保不能设置内置角色
		var criteria = this.GetCriteria(identifier) & Condition.NotIn(nameof(IRole.Name), [IRole.Administrators, IRole.Security]);
		return criteria != null && await this.Accessor.UpdateAsync(this.Name, new { Enabled = true }, criteria, cancellation) > 0;
	}

	public virtual async ValueTask<bool> DisableAsync(Identifier identifier, CancellationToken cancellation = default)
	{
		//确保不能禁用内置角色
		var criteria = this.GetCriteria(identifier) & Condition.NotIn(nameof(IRole.Name), [IRole.Administrators, IRole.Security]);
		return criteria != null && await this.Accessor.UpdateAsync(this.Name, new { Enabled = false }, criteria, cancellation) > 0;
	}

	public async ValueTask<bool> RenameAsync(Identifier identifier, string name, CancellationToken cancellation = default)
	{
		if(identifier.IsEmpty || string.IsNullOrEmpty(name))
			return false;

		//验证指定的名称是否合法
		this.OnValidateName(name);

		//确保不能更名内置角色
		var criteria = this.GetCriteria(identifier) & Condition.NotIn(nameof(IRole.Name), [IRole.Administrators, IRole.Security]);
		if(criteria == null)
			return false;

		return await this.Accessor.UpdateAsync(this.Name, new { Name = name }, criteria, cancellation) > 0;
	}

	public async ValueTask<bool> CreateAsync(TRole role, CancellationToken cancellation = default)
	{
		if(role == null)
			return false;

		//确认待创建的角色实体
		this.OnCreating(role);

		//验证指定的名称是否合法
		this.OnValidateName(role.Name);

		if(await this.Accessor.InsertAsync(role, cancellation) > 0)
		{
			this.OnCreated(role);
			return true;
		}

		return false;
	}

	public async ValueTask<int> CreateAsync(IEnumerable<TRole> roles, CancellationToken cancellation = default)
	{
		if(roles == null)
			return 0;

		foreach(var role in roles)
		{
			if(role == null)
				continue;

			//确认待创建的角色实体
			this.OnCreating(role);

			//验证指定的名称是否合法
			this.OnValidateName(role.Name);
		}

		var count = await this.Accessor.InsertAsync(roles, cancellation);

		if(count > 0)
		{
			foreach(var role in roles)
			{
				if(role != null && role.Identifier.HasValue)
					this.OnCreated(role);
			}
		}

		return count;
	}

	public async ValueTask<bool> DeleteAsync(Identifier identifier, CancellationToken cancellation = default)
	{
		if(identifier.IsEmpty)
			return false;

		//确保不能删除内置角色
		var criteria = this.GetCriteria(identifier) & Condition.NotIn(nameof(IRole.Name), [IRole.Administrators, IRole.Security]);
		if(criteria == null)
			return false;

		if(await this.Accessor.DeleteAsync(this.Name, criteria, cancellation: cancellation) > 0)
		{
			//删除成员表中当前角色的所有子集
			await Authentication.Servicer.Members.RemoveAsync(identifier, null, cancellation);

			//删除成员表中当前角色的所有父级
			await Authentication.Servicer.Members.RemoveAsync(Member.Role(identifier), cancellation);

			return true;
		}

		return false;
	}

	public async ValueTask<int> DeleteAsync(IEnumerable<Identifier> identifiers, CancellationToken cancellation = default)
	{
		if(identifiers == null)
			return 0;

		//删除数量
		var total = 0;

		//创建事务
		using var transaction = new Zongsoft.Transactions.Transaction();

		foreach(var identifier in identifiers)
		{
			if(identifier.IsEmpty)
				continue;

			//确保不能删除内置角色
			var criteria = this.GetCriteria(identifier) & Condition.NotIn(nameof(IRole.Name), [IRole.Administrators, IRole.Security]);
			if(criteria == null)
				continue;

			var count = await this.Accessor.DeleteAsync(this.Name, criteria, cancellation: cancellation);

			if(count > 0)
			{
				total += count;

				//删除成员表中当前角色的所有子集
				await Authentication.Servicer.Members.RemoveAsync(identifier, null, cancellation);

				//删除成员表中当前角色的所有父级
				await Authentication.Servicer.Members.RemoveAsync(Member.Role(identifier), cancellation);
			}
		}

		//提交事务
		transaction.Commit();

		//返回删除数量
		return total;
	}

	public async ValueTask<bool> UpdateAsync(TRole role, CancellationToken cancellation = default)
	{
		if(role == null)
			return false;

		//确保修改的不是内置角色
		var criteria = this.GetCriteria(role.Identifier) & Condition.NotIn(nameof(IRole.Name), [IRole.Administrators, IRole.Security]);
		if(await this.Accessor.ExistsAsync(this.Name, criteria, cancellation: cancellation))
			return false;

		return await this.Accessor.UpdateAsync(role, cancellation) > 0;
	}
	#endregion

	#region 虚拟方法
	protected virtual void OnCreating(TRole role)
	{
		if(string.IsNullOrWhiteSpace(role.Name))
		{
			//如果未指定角色名，则为其设置一个随机名
			if(string.IsNullOrWhiteSpace(role.Name))
				role.Name = "R" + Randomizer.GenerateString();
		}

		if(string.IsNullOrWhiteSpace(role.Namespace))
			role.Namespace = null;
	}

	protected virtual void OnCreated(TRole role) { }

	protected virtual void OnValidateName(string name)
	{
		//验证指定的名称是否为系统内置名
		if(string.Equals(name, IRole.Administrators, StringComparison.OrdinalIgnoreCase) ||
		   string.Equals(name, IRole.Security, StringComparison.OrdinalIgnoreCase))
			throw new SecurityException("rolename.illegality", "The role name specified to be update cannot be a built-in name.");

		var validator = this.Services.Resolve<IValidator<string>>("role.name");
		validator?.Validate(name, message => throw new SecurityException("rolename.illegality", message));
	}

	protected virtual ICondition GetCriteria(string keyword)
	{
		if(string.IsNullOrEmpty(keyword) || keyword == "*")
			return null;

		var index = keyword.IndexOf(':');

		if(index >= 0)
			return Condition.Equal(nameof(IRole.Namespace), keyword[..index]) &
			(
				Condition.Like(nameof(IRole.Name), keyword[(index + 1)..]) |
				Condition.Like(nameof(IRole.Nickname), keyword[(index + 1)..])
			);

		return Condition.Like(nameof(IRole.Name), keyword) |
		       Condition.Like(nameof(IRole.Nickname), keyword);
	}

	protected virtual ICondition GetCriteria(Identifier identifier)
	{
		if(identifier.IsEmpty)
			return null;

		if(identifier.Validate(out string qualifiedName))
		{
			var index = qualifiedName.IndexOf(':');

			if(index >= 0)
				return Condition.Equal(nameof(IRole.Namespace), qualifiedName[..index]) & Condition.Equal(nameof(IRole.Name), qualifiedName[(index + 1)..]);
			else
				return Condition.Equal(nameof(IRole.Name), qualifiedName);
		}

		throw OperationException.Argument();
	}
	#endregion

	#region 显式实现
	async ValueTask<IRole> IRoleService.GetAsync(Identifier identifier, CancellationToken cancellation) => await this.GetAsync(identifier, cancellation);
	async ValueTask<IRole> IRoleService.GetAsync(Identifier identifier, string schema, CancellationToken cancellation) => await this.GetAsync(identifier, schema, cancellation);
	IAsyncEnumerable<IRole> IRoleService.FindAsync(string keyword, string schema, Paging paging, CancellationToken cancellation) => this.FindAsync(keyword, schema, paging, cancellation).Map(role => (IRole)role);
	IAsyncEnumerable<IRole> IRoleService.FindAsync(ICondition criteria, string schema, Paging paging, CancellationToken cancellation) => this.FindAsync(criteria, schema, paging, cancellation).Map(role => (IRole)role);
	ValueTask<bool> IRoleService.CreateAsync(IRole role, CancellationToken cancellation) => this.CreateAsync(role is TRole model ? model : default, cancellation);
	ValueTask<int> IRoleService.CreateAsync(IEnumerable<IRole> roles, CancellationToken cancellation) => this.CreateAsync(roles.Cast<TRole>(), cancellation);
	ValueTask<bool> IRoleService.UpdateAsync(IRole role, CancellationToken cancellation) => this.UpdateAsync(role is TRole model ? model : default, cancellation);
	#endregion

	#region 服务匹配
	bool IMatchable.Match(object argument) => argument is ClaimsPrincipal principal && this.OnMatch(principal);
	bool IMatchable<ClaimsPrincipal>.Match(ClaimsPrincipal argument) => this.OnMatch(argument);
	protected virtual bool OnMatch(ClaimsPrincipal principal) => principal != null && principal.Identity != null && principal.Identity.IsAuthenticated;
	#endregion
}
