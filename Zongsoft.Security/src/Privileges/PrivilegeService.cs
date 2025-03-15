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
using System.Security.Claims;
using System.Security.Principal;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

using Zongsoft.Security.Privileges.Models;

namespace Zongsoft.Security.Privileges;

[Service<IPrivilegeService>]
public class PrivilegeService : IPrivilegeService
{
	#region 获取方法
	public IAsyncEnumerable<IPrivilege> GetPrivilegesAsync(Identifier identifier, Parameters parameters, CancellationToken cancellation = default)
	{
		if(identifier.IsEmpty)
			return Zongsoft.Collections.Enumerable.Empty<IPrivilege>();

		if(!TryGetMember(ref identifier, out var memberId, out var memberType))
			return Zongsoft.Collections.Enumerable.Empty<IPrivilege>();

		if(parameters.Contains("type", "filter", StringComparer.OrdinalIgnoreCase))
		{
			var criteria =
				Condition.Equal(nameof(PrivilegeFilterModel.MemberId), memberId) &
				Condition.Equal(nameof(PrivilegeFilterModel.MemberType), memberType);

			return Module.Current.Accessor
				.SelectAsync<PrivilegeFilterModel>(criteria, cancellation)
				.Map(model => new PrivilegeFilterRequirement(model.Privilege, model.Filter));
		}
		else
		{
			var criteria =
				Condition.Equal(nameof(PrivilegeModel.MemberId), memberId) &
				Condition.Equal(nameof(PrivilegeModel.MemberType), memberType);

			return Module.Current.Accessor
				.SelectAsync<PrivilegeModel>(criteria, cancellation)
				.Map(model => new PrivilegeRequirement(model.Privilege, model.Granted));
		}
	}
	#endregion

	#region 设置方法
	public ValueTask<int> SetPrivilegesAsync(Identifier identifier, IEnumerable<IPrivilege> privileges, Parameters parameters, CancellationToken cancellation = default) => this.SetPrivilegesAsync(identifier, privileges, false, parameters, cancellation);
	public async ValueTask<int> SetPrivilegesAsync(Identifier identifier, IEnumerable<IPrivilege> privileges, bool shouldResetting, Parameters parameters, CancellationToken cancellation = default)
	{
		if(identifier.IsEmpty || privileges == null)
			return 0;

		if(!TryGetMember(ref identifier, out var memberId, out var memberType))
			return 0;

		if(parameters.Contains("type", "filter", StringComparer.OrdinalIgnoreCase))
		{
			if(shouldResetting)
				await Module.Current.Accessor.DeleteAsync<PrivilegeFilterModel>(
					Condition.Equal(nameof(PrivilegeFilterModel.MemberId), memberId) &
					Condition.Equal(nameof(PrivilegeFilterModel.MemberType), memberType), string.Empty, cancellation);

			var models = privileges
				.OfType<PrivilegeFilterRequirement>()
				.Select(requirement => Model.Build<PrivilegeFilterModel>(model =>
				{
					model.MemberId = memberId;
					model.MemberType = memberType;
					model.Privilege = requirement.Name;
					model.Filter = requirement.Filter;
				}));

			return await Module.Current.Accessor.InsertManyAsync(models, cancellation);
		}
		else
		{
			if(shouldResetting)
				await Module.Current.Accessor.DeleteAsync<PrivilegeModel>(
					Condition.Equal(nameof(PrivilegeModel.MemberId), memberId) &
					Condition.Equal(nameof(PrivilegeModel.MemberType), memberType), string.Empty, cancellation);

			var models = privileges
				.OfType<PrivilegeRequirement>()
				.Select(requirement => Model.Build<PrivilegeModel>(model =>
				{
					model.MemberId = memberId;
					model.MemberType = memberType;
					model.Privilege = requirement.Name;
					model.Granted = requirement.Granted;
				}));

			return await Module.Current.Accessor.InsertManyAsync(models, cancellation);
		}
	}
	#endregion

	#region 私有方法
	private static bool TryGetMember(ref Identifier identifier, out uint memberId, out MemberType memberType)
	{
		if(identifier.Validate<IUser, uint>(out memberId))
		{
			memberType = MemberType.User;
			return true;
		}

		if(identifier.Validate(out memberId))
		{
			memberType = MemberType.Role;
			return true;
		}

		memberId = 0;
		memberType = MemberType.User;
		return false;
	}
	#endregion

	#region 嵌套子类
	public class PrivilegeRequirement : IPrivilege, IEquatable<IPrivilege>
	{
		#region 构造函数
		public PrivilegeRequirement() { }
		public PrivilegeRequirement(string name, bool granted)
		{
			this.Name = name;
			this.Granted = granted;
		}
		#endregion

		#region 公共属性
		public string Name { get; set; }
		public bool Granted { get; set; }
		#endregion

		#region 重写方法
		public override string ToString() => this.Granted ? $"{this.Name}(Granted)" : $"{this.Name}(Denied)";
		#endregion

		#region 显式实现
		bool IEquatable<IPrivilege>.Equals(IPrivilege other) => other is not null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
		#endregion
	}

	public class PrivilegeFilterRequirement : IPrivilege, IEquatable<IPrivilege>
	{
		#region 构造函数
		public PrivilegeFilterRequirement() { }
		public PrivilegeFilterRequirement(string name, string filter)
		{
			this.Name = name;
			this.Filter = filter;
		}
		#endregion

		#region 公共属性
		public string Name { get; set; }
		public string Filter { get; set; }
		#endregion

		#region 重写方法
		public override string ToString() => string.IsNullOrEmpty(this.Filter) ? this.Name : $"{this.Name}({this.Filter})";
		#endregion

		#region 显式实现
		bool IEquatable<IPrivilege>.Equals(IPrivilege other) => other is not null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
		#endregion
	}
	#endregion
}
