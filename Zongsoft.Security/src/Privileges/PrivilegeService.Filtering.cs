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

partial class PrivilegeService
{
	protected class FilteringService(PrivilegeService service) : PrivilegeServiceBase<FilteringService.Privilege>
	{
		#region 成员字段
		private readonly PrivilegeService _service = service ?? throw new ArgumentNullException(nameof(service));
		#endregion

		#region 重写属性
		protected override IDataAccess Accessor => Module.Current.Accessor;
		#endregion

		#region 重写方法
		protected override IAsyncEnumerable<Privilege> OnGetPrivilegesAsync(ICondition criteria, Parameters parameters, CancellationToken cancellation)
		{
			return this.Accessor.SelectAsync<PrivilegeFilteringModel>(criteria, new DataSelectOptions(parameters), cancellation).Map(model => new Privilege(model.PrivilegeName, model.PrivilegeFilter));
		}

		protected override ValueTask<int> OnResetPrivilegesAsync(ICondition criteria, Parameters parameters, CancellationToken cancellation)
		{
			return this.Accessor.DeleteAsync(Model.Naming.Get<PrivilegeFilteringModel>(), criteria, new DataDeleteOptions(parameters), cancellation);
		}

		protected override ValueTask<int> OnSetPrivilegesAsync(Identifier identifier, IEnumerable<Privilege> privileges, Parameters parameters, CancellationToken cancellation)
		{
			if(privileges == null)
				return ValueTask.FromResult(0);

			if(!TryGetMember(ref identifier, out var memberId, out var memberType))
				return ValueTask.FromResult(0);

			var models = privileges.Select(privilege => Model.Build<PrivilegeFilteringModel>(model =>
			{
				model.MemberId = memberId;
				model.MemberType = memberType;
				model.PrivilegeName = privilege.Name;
				model.PrivilegeFilter = privilege.Filter;
			}));

			return this.Accessor.UpsertManyAsync(models, new DataUpsertOptions(parameters), cancellation);
		}

		protected override ICondition GetCriteria(Identifier identifier, params string[] privileges) => _service.GetCriteria(identifier, privileges);
		protected override ICondition GetCriteria(IEnumerable<Identifier> identifiers) => _service.GetCriteria(identifiers);
		#endregion

		#region 嵌套子类
		public class Privilege : IPrivilege, IEquatable<IPrivilege>
		{
			#region 构造函数
			public Privilege() { }
			public Privilege(string name, string filter)
			{
				this.Name = name;
				this.Filter = filter;
			}
			#endregion

			#region 公共属性
			public string Name { get; set; }
			public string Filter { get; set; }
			bool IPrivilege.IsEmpty => string.IsNullOrEmpty(this.Name) || string.IsNullOrEmpty(this.Filter);
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
}
