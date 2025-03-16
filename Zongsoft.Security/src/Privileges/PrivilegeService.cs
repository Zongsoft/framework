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
public class PrivilegeService : PrivilegeServiceBase
{
	#region 重写属性
	protected override IDataAccess Accessor => Module.Current.Accessor;
	#endregion

	#region 获取方法
	protected override IAsyncEnumerable<IPrivilege> OnGetPrivilegesAsync(Identifier identifier, Parameters parameters, CancellationToken cancellation = default)
	{
		if(identifier.IsEmpty)
			return Zongsoft.Collections.Enumerable.Empty<IPrivilege>();

		if(!TryGetMember(ref identifier, out var memberId, out var memberType))
			return Zongsoft.Collections.Enumerable.Empty<IPrivilege>();

		if(IsFiltering(parameters))
		{
			var criteria =
				Condition.Equal(nameof(PrivilegeFilteringModel.MemberId), memberId) &
				Condition.Equal(nameof(PrivilegeFilteringModel.MemberType), memberType);

			return Module.Current.Accessor
				.SelectAsync<PrivilegeFilteringModel>(criteria, cancellation)
				.Map(model => new PrivilegeFilteringRequirement(model.PrivilegeName, model.PrivilegeFilter));
		}
		else
		{
			var criteria =
				Condition.Equal(nameof(PrivilegeModel.MemberId), memberId) &
				Condition.Equal(nameof(PrivilegeModel.MemberType), memberType);

			return Module.Current.Accessor
				.SelectAsync<PrivilegeModel>(criteria, cancellation)
				.Map(model => new PrivilegeRequirement(model.PrivilegeName, model.PrivilegeMode));
		}
	}
	#endregion

	#region 设置方法
	protected override ValueTask<int> OnResetPrivilegesAsync(Identifier identifier, Parameters parameters, CancellationToken cancellation)
	{
		if(!TryGetMember(ref identifier, out var memberId, out var memberType))
			return ValueTask.FromResult(0);

		if(IsFiltering(parameters))
			return Module.Current.Accessor.DeleteAsync<PrivilegeFilteringModel>(
					Condition.Equal(nameof(PrivilegeFilteringModel.MemberId), memberId) &
					Condition.Equal(nameof(PrivilegeFilteringModel.MemberType), memberType), string.Empty, cancellation);
		else
			return Module.Current.Accessor.DeleteAsync<PrivilegeModel>(
					Condition.Equal(nameof(PrivilegeModel.MemberId), memberId) &
					Condition.Equal(nameof(PrivilegeModel.MemberType), memberType), string.Empty, cancellation);
	}

	protected override async ValueTask<int> OnSetPrivilegesAsync(Identifier identifier, IEnumerable<IPrivilege> privileges, Parameters parameters, CancellationToken cancellation = default)
	{
		if(identifier.IsEmpty || privileges == null)
			return 0;

		if(!TryGetMember(ref identifier, out var memberId, out var memberType))
			return 0;

		if(IsFiltering(parameters))
		{
			//找出过滤字段为空值的权限
			var requirements = privileges
				.OfType<PrivilegeFilteringRequirement>()
				.Where(privilege => string.IsNullOrWhiteSpace(privilege.Filter))
				.Select(privilege => privilege.Name).ToArray();

			//删除过滤字段为空值的权限设置
			if(requirements.Length > 0)
				await Module.Current.Accessor.DeleteAsync<PrivilegeFilteringModel>(
					Condition.Equal(nameof(PrivilegeFilteringModel.MemberId), memberId) &
					Condition.Equal(nameof(PrivilegeFilteringModel.MemberType), memberType) &
					Condition.In(nameof(PrivilegeFilteringModel.PrivilegeName), requirements), cancellation: cancellation);

			var models = privileges
				.OfType<PrivilegeFilteringRequirement>()
				.Where(privilege => !string.IsNullOrWhiteSpace(privilege.Filter))
				.Select(privilege => Model.Build<PrivilegeFilteringModel>(model =>
				{
					model.MemberId = memberId;
					model.MemberType = memberType;
					model.PrivilegeName = privilege.Name;
					model.PrivilegeFilter = privilege.Filter;
				}));

			return await Module.Current.Accessor.UpsertManyAsync(models, cancellation);
		}
		else
		{
			//找出授权方式为空值的权限
			var requirements = privileges
				.OfType<PrivilegeRequirement>()
				.Where(privilege => privilege.Mode == null)
				.Select(privilege => privilege.Name).ToArray();

			//删除授权方式为空值的权限设置
			if(requirements.Length > 0)
				await Module.Current.Accessor.DeleteAsync<PrivilegeModel>(
					Condition.Equal(nameof(PrivilegeModel.MemberId), memberId) &
					Condition.Equal(nameof(PrivilegeModel.MemberType), memberType) &
					Condition.In(nameof(PrivilegeModel.PrivilegeName), requirements), cancellation: cancellation);

			var models = privileges
				.OfType<PrivilegeRequirement>()
				.Where(privilege => privilege.Mode.HasValue)
				.Select(privilege => Model.Build<PrivilegeModel>(model =>
				{
					model.MemberId = memberId;
					model.MemberType = memberType;
					model.PrivilegeName = privilege.Name;
					model.PrivilegeMode = privilege.Mode.Value;
				}));

			return await Module.Current.Accessor.UpsertManyAsync(models, cancellation);
		}
	}
	#endregion

	#region 私有方法
	private static bool IsFiltering(Parameters parameters) => parameters.Contains("type", "filter", StringComparer.OrdinalIgnoreCase);
	private static bool TryGetMember(ref Identifier identifier, out uint memberId, out MemberType memberType)
	{
		if(identifier.Validate<IUser, uint>(out memberId))
		{
			memberType = MemberType.User;
			return true;
		}

		if(identifier.Validate<IRole, uint>(out memberId))
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
	public class PrivilegeRequirement : IPrivilege, IEquatable<IPrivilege>
	{
		#region 构造函数
		public PrivilegeRequirement() { }
		public PrivilegeRequirement(string name, PrivilegeMode? mode = null)
		{
			this.Name = name;
			this.Mode = mode;
		}
		#endregion

		#region 公共属性
		public string Name { get; set; }
		public PrivilegeMode? Mode { get; set; }
		#endregion

		#region 重写方法
		public override string ToString() => this.Mode.HasValue ? $"{this.Name}({this.Mode})" : this.Name;
		#endregion

		#region 显式实现
		bool IEquatable<IPrivilege>.Equals(IPrivilege other) => other is not null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
		#endregion
	}

	public class PrivilegeFilteringRequirement : IPrivilege, IEquatable<IPrivilege>
	{
		#region 构造函数
		public PrivilegeFilteringRequirement() { }
		public PrivilegeFilteringRequirement(string name, string filter)
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
