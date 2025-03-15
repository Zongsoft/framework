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
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

public abstract class RoleServiceBase<TRole, TMember> : IRoleService<TRole, TMember>, IMatchable, IMatchable<string> where TRole : IRole where TMember : IMember<TRole>
{
	#region 构造函数
	protected RoleServiceBase(IMemberService<TRole, TMember> members = null) => this.Members = members;
	#endregion

	#region 公共属性
	public virtual string Name => this.Accessor.Naming.Get<TRole>();
	public IMemberService<TRole, TMember> Members { get; protected init; }
	#endregion

	#region 保护属性
	internal protected abstract IDataAccess Accessor { get; }
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

	public async ValueTask<bool> Rename(Identifier identifier, string name, CancellationToken cancellation = default)
	{
		if(identifier.IsEmpty || string.IsNullOrEmpty(name))
			return false;

		var criteria = this.GetCriteria(identifier);
		if(criteria == null)
			return false;

		return await this.Accessor.UpdateAsync(this.Name, new { Name = name }, criteria, cancellation) > 0;
	}

	public async ValueTask<bool> CreateAsync(TRole role, CancellationToken cancellation = default)
	{
		if(role == null)
			return false;

		return await this.Accessor.InsertAsync(role, cancellation) > 0;
	}

	public ValueTask<int> CreateAsync(IEnumerable<TRole> roles, CancellationToken cancellation = default)
	{
		if(roles == null)
			return ValueTask.FromResult(0);

		return this.Accessor.InsertAsync(roles, cancellation);
	}

	public async ValueTask<bool> DeleteAsync(Identifier identifier, CancellationToken cancellation = default)
	{
		if(identifier.IsEmpty)
			return false;

		var criteria = this.GetCriteria(identifier);
		if(criteria == null)
			return false;

		return await this.Accessor.DeleteAsync(this.Name, criteria, cancellation: cancellation) > 0;
	}

	public async ValueTask<int> DeleteAsync(IEnumerable<Identifier> identifiers, CancellationToken cancellation = default)
	{
		if(identifiers == null)
			return 0;

		//删除数量
		var count = 0;

		//创建事务
		using var transaction = new Zongsoft.Transactions.Transaction();

		foreach(var identifier in identifiers)
		{
			if(identifier.IsEmpty)
				continue;

			var criteria = this.GetCriteria(identifier);
			if(criteria == null)
				continue;

			count += await this.Accessor.DeleteAsync(this.Name, criteria, cancellation: cancellation);
		}

		//提交事务
		transaction.Commit();

		//返回删除数量
		return count;
	}

	public async ValueTask<bool> UpdateAsync(TRole role, CancellationToken cancellation = default)
	{
		if(role == null)
			return false;

		return await this.Accessor.UpdateAsync(role, cancellation) > 0;
	}
	#endregion

	#region 虚拟方法
	protected virtual ICondition GetCriteria(string keyword) => string.IsNullOrEmpty(keyword) ? null :
		Condition.Like(nameof(IRole.Name), keyword) |
		Condition.Like(nameof(IRole.Nickname), keyword);

	protected virtual ICondition GetCriteria(Identifier identifier)
	{
		if(identifier.IsEmpty)
			return null;

		if(identifier.Validate(out string qualifiedName))
		{
			var parts = qualifiedName.Split(':');

			if(parts.Length == 2)
				return Condition.Equal(nameof(IRole.Namespace), parts[0]) & Condition.Equal(nameof(IRole.Name), parts[1]);
			else
				return Condition.Equal(nameof(IRole.Name), parts[0]);
		}

		throw OperationException.Argument();
	}
	#endregion

	#region 服务匹配
	bool IMatchable.Match(object argument) => argument is string name && this.OnMatch(name);
	bool IMatchable<string>.Match(string argument) => this.OnMatch(argument);
	protected virtual bool OnMatch(string argument) => string.Equals(this.Name, argument, StringComparison.OrdinalIgnoreCase);
	#endregion
}
