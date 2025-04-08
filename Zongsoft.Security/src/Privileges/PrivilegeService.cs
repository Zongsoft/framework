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
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

using Zongsoft.Security.Privileges.Models;

namespace Zongsoft.Security.Privileges;

[Service<IPrivilegeService>]
public partial class PrivilegeService : PrivilegeServiceBase<PrivilegeService.Privilege>
{
	#region 构造函数
	public PrivilegeService() => this.Filtering = new FilteringService(this);
	#endregion

	#region 重写属性
	protected override IDataAccess Accessor => Module.Current.Accessor;
	#endregion

	#region 重写方法
	protected override IAsyncEnumerable<Privilege> OnGetPrivilegesAsync(ICondition criteria, Parameters parameters, CancellationToken cancellation)
	{
		return this.Accessor.SelectAsync<PrivilegeModel>(criteria, new DataSelectOptions(parameters), cancellation).Map(model => new Privilege(model.PrivilegeName, model.PrivilegeMode));
	}

	protected override ValueTask<int> OnResetPrivilegesAsync(ICondition criteria, Parameters parameters, CancellationToken cancellation)
	{
		return this.Accessor.DeleteAsync(Model.Naming.Get<PrivilegeModel>(), criteria, new DataDeleteOptions(parameters), cancellation);
	}

	protected override ValueTask<int> OnSetPrivilegesAsync(Identifier identifier, IEnumerable<Privilege> privileges, Parameters parameters, CancellationToken cancellation)
	{
		if(privileges == null)
			return ValueTask.FromResult(0);

		if(!TryGetMember(ref identifier, out var memberId, out var memberType))
			return ValueTask.FromResult(0);

		var models = privileges.Select(privilege => Model.Build<PrivilegeModel>(model =>
		{
			model.MemberId = memberId;
			model.MemberType = memberType;
			model.PrivilegeName = privilege.Name;
			model.PrivilegeMode = privilege.Mode.Value;
		}));

		return this.Accessor.UpsertManyAsync(models, new DataUpsertOptions(parameters), cancellation);
	}

	protected override ICondition GetCriteria(Identifier identifier, params string[] privileges)
	{
		if(identifier.IsEmpty)
			return privileges == null || privileges.Length == 0 ? null : Condition.In(nameof(PrivilegeModel.PrivilegeName), privileges);

		if(!TryGetMember(ref identifier, out var memberId, out var memberType))
			return null;

		return privileges == null || privileges.Length == 0 ?
				Condition.Equal(nameof(PrivilegeModel.MemberId), memberId) &
				Condition.Equal(nameof(PrivilegeModel.MemberType), memberType):
				Condition.Equal(nameof(PrivilegeModel.MemberId), memberId) &
				Condition.Equal(nameof(PrivilegeModel.MemberType), memberType) &
				Condition.In(nameof(PrivilegeModel.PrivilegeName), privileges);
	}

	protected override ICondition GetCriteria(IEnumerable<Identifier> identifiers)
	{
		if(identifiers == null)
			return null;

		ICondition criteria = null;
		var users = identifiers.Select(identifier => identifier.IsUser(out uint userId) ? userId : 0U).Where(id => id > 0).Distinct().ToArray();
		var roles = identifiers.Select(identifier => identifier.IsRole(out uint roleId) ? roleId : 0U).Where(id => id > 0).Distinct().ToArray();

		if(users != null && users.Length > 0)
			criteria &= Condition.Equal(nameof(PrivilegeModel.MemberType), MemberType.User) & Condition.In(nameof(PrivilegeModel.MemberId), users);
		if(roles != null && roles.Length > 0)
			criteria &= Condition.Equal(nameof(PrivilegeModel.MemberType), MemberType.Role) & Condition.In(nameof(PrivilegeModel.MemberId), roles);

		return criteria;
	}
	#endregion

	#region 私有方法
	private static bool TryGetMember(ref Identifier identifier, out uint memberId, out MemberType memberType)
	{
		if(identifier.IsUser(out memberId))
		{
			memberType = MemberType.User;
			return true;
		}

		if(identifier.IsRole(out memberId))
		{
			memberType = MemberType.Role;
			return true;
		}

		memberId = 0;
		memberType = 0;
		return false;
	}
	#endregion

	#region 嵌套子类
	public class Privilege : IPrivilege, IPrivilegable, IEquatable<IPrivilege>
	{
		#region 构造函数
		public Privilege() { }
		public Privilege(string name, PrivilegeMode? mode = null)
		{
			this.Name = name;
			this.Mode = mode;
		}
		#endregion

		#region 公共属性
		public string Name { get; set; }
		public PrivilegeMode? Mode { get; set; }
		bool IPrivilege.IsEmpty => string.IsNullOrEmpty(this.Name) || this.Mode == null || this.Mode == PrivilegeMode.Revoked;
		PrivilegeMode IPrivilegable.Mode => this.Mode ?? PrivilegeMode.Revoked;
		#endregion

		#region 重写方法
		public override string ToString() => this.Mode.HasValue ? $"{this.Name}({this.Mode})" : this.Name;
		#endregion

		#region 显式实现
		bool IEquatable<IPrivilege>.Equals(IPrivilege other) => other is not null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
		#endregion
	}
	#endregion
}
