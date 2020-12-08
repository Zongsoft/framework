/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Security.Membership
{
	[Service(typeof(IPermissionProvider))]
	public class PermissionProvider : IPermissionProvider
	{
		#region 构造函数
		public PermissionProvider(IServiceProvider serviceProvider)
		{
			this.DataAccess = serviceProvider.ResolveRequired<IDataAccessProvider>()
				.GetAccessor(Mapping.Security) ?? serviceProvider.GetDataAccess(true);

			if(!string.IsNullOrEmpty(Mapping.Instance.Permission))
				this.DataAccess.Naming.Map<Member>(Mapping.Instance.Permission);
			if(!string.IsNullOrEmpty(Mapping.Instance.PermissionFilter))
				this.DataAccess.Naming.Map<Member>(Mapping.Instance.PermissionFilter);
		}
		#endregion

		#region 公共属性
		public IDataAccess DataAccess { get; }
		#endregion

		#region 公共方法
		public IEnumerable<Permission> GetPermissions(uint memberId, MemberType memberType, string schemaId = null)
		{
			var conditions = Condition.Equal(nameof(Permission.MemberId), memberId) & Condition.Equal(nameof(Permission.MemberType), memberType);

			if(!string.IsNullOrWhiteSpace(schemaId))
				conditions.Add(Condition.Equal(nameof(Permission.SchemaId), schemaId));

			return this.DataAccess.Select<Permission>(conditions);
		}

		public int SetPermissions(uint memberId, MemberType memberType, IEnumerable<Permission> permissions, bool shouldResetting = false)
		{
			return this.SetPermissions(memberId, memberType, null, permissions, shouldResetting);
		}

		public int SetPermissions(uint memberId, MemberType memberType, string schemaId, IEnumerable<Permission> permissions, bool shouldResetting = false)
		{
			var conditions = Condition.Equal(nameof(Permission.MemberId), memberId) & Condition.Equal(nameof(Permission.MemberType), memberType);

			if(!string.IsNullOrWhiteSpace(schemaId))
				conditions.Add(Condition.Equal(nameof(Permission.SchemaId), schemaId));

			using(var transaction = new Zongsoft.Transactions.Transaction())
			{
				int count = 0;

				//清空指定成员的所有权限设置
				if(shouldResetting || permissions == null)
					count = this.DataAccess.Delete<Permission>(conditions);

				//写入指定的权限设置集到数据库中
				if(permissions != null)
					count = this.DataAccess.UpsertMany(
						permissions.Select(p => new Permission(memberId, memberType, (string.IsNullOrEmpty(schemaId) ? p.SchemaId : schemaId), p.ActionId, p.Granted)));

				//提交事务
				transaction.Commit();

				return count;
			}
		}

		public int RemovePermissions(uint memberId, MemberType memberType, string schemaId = null, string actionId = null)
		{
			var criteria = Condition.Equal(nameof(Permission.MemberId), memberId) &
			               Condition.Equal(nameof(Permission.MemberType), memberType);

			if(schemaId != null && schemaId.Length > 0)
				criteria.Add(Condition.Equal(nameof(Permission.SchemaId), schemaId));

			if(actionId != null && actionId.Length > 0)
				criteria.Add(Condition.Equal(nameof(Permission.ActionId), actionId));

			return this.DataAccess.Delete<Permission>(criteria);
		}

		public IEnumerable<PermissionFilter> GetPermissionFilters(uint memberId, MemberType memberType, string schemaId = null)
		{
			var conditions = Condition.Equal(nameof(PermissionFilter.MemberId), memberId) & Condition.Equal(nameof(PermissionFilter.MemberType), memberType);

			if(!string.IsNullOrWhiteSpace(schemaId))
				conditions.Add(Condition.Equal(nameof(PermissionFilter.SchemaId), schemaId));

			return this.DataAccess.Select<PermissionFilter>(conditions);
		}

		public int SetPermissionFilters(uint memberId, MemberType memberType, IEnumerable<PermissionFilter> permissionFilters, bool shouldResetting = false)
		{
			return this.SetPermissionFilters(memberId, memberType, null, permissionFilters, shouldResetting);
		}

		public int SetPermissionFilters(uint memberId, MemberType memberType, string schemaId, IEnumerable<PermissionFilter> permissionFilters, bool shouldResetting = false)
		{
			var conditions = Condition.Equal(nameof(PermissionFilter.MemberId), memberId) & Condition.Equal(nameof(PermissionFilter.MemberType), memberType);

			if(!string.IsNullOrWhiteSpace(schemaId))
				conditions.Add(Condition.Equal(nameof(PermissionFilter.SchemaId), schemaId));

			using(var transaction = new Zongsoft.Transactions.Transaction())
			{
				int count = 0;

				//清空指定成员的所有权限设置
				if(shouldResetting || permissionFilters == null)
					count = this.DataAccess.Delete<PermissionFilter>(conditions);

				//插入指定的权限设置集到数据库中
				if(permissionFilters != null)
					count = this.DataAccess.UpsertMany(
						permissionFilters.Select(p => new PermissionFilter(memberId, memberType, (string.IsNullOrEmpty(schemaId) ? p.SchemaId : schemaId), p.ActionId, p.Filter)));

				//提交事务
				transaction.Commit();

				return count;
			}
		}

		public int RemovePermissionFilters(uint memberId, MemberType memberType, string schemaId = null, string actionId = null)
		{
			var criteria = Condition.Equal(nameof(Permission.MemberId), memberId) &
			               Condition.Equal(nameof(Permission.MemberType), memberType);

			if(schemaId != null && schemaId.Length > 0)
				criteria.Add(Condition.Equal(nameof(Permission.SchemaId), schemaId));

			if(actionId != null && actionId.Length > 0)
				criteria.Add(Condition.Equal(nameof(Permission.ActionId), actionId));

			return this.DataAccess.Delete<PermissionFilter>(criteria);
		}
		#endregion
	}
}
