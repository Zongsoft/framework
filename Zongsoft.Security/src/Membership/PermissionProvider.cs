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

namespace Zongsoft.Security.Membership
{
	[Service(typeof(IPermissionProvider))]
	public class PermissionProvider : IPermissionProvider
	{
		#region 成员字段
		private IDataAccess _dataAccess;
		#endregion

		#region 构造函数
		public PermissionProvider() { }
		#endregion

		#region 公共属性
		public IDataAccess DataAccess
		{
			get
			{
				if(_dataAccess == null)
				{
					_dataAccess = Module.Current.Accessor;

					if(_dataAccess != null)
					{
						if(!string.IsNullOrEmpty(Mapping.Instance.Permission))
							_dataAccess.Naming.Map<PermissionModel>(Mapping.Instance.Permission);
						if(!string.IsNullOrEmpty(Mapping.Instance.PermissionFilter))
							_dataAccess.Naming.Map<PermissionFilterModel>(Mapping.Instance.PermissionFilter);
					}
				}

				return _dataAccess;
			}
		}
		#endregion

		#region 公共方法
		public IEnumerable<PermissionModel> GetPermissions(uint memberId, MemberType memberType, string target = null)
		{
			var conditions = Condition.Equal(nameof(PermissionModel.MemberId), memberId) & Condition.Equal(nameof(PermissionModel.MemberType), memberType);

			if(!string.IsNullOrWhiteSpace(target))
				conditions.Add(Condition.Equal(nameof(PermissionModel.Target), target));

			return this.DataAccess.Select<PermissionModel>(conditions);
		}

		public int SetPermissions(uint memberId, MemberType memberType, IEnumerable<PermissionModel> permissions, bool shouldResetting = false) =>
			this.SetPermissions(memberId, memberType, null, permissions, shouldResetting);

		public int SetPermissions(uint memberId, MemberType memberType, string target, IEnumerable<PermissionModel> permissions, bool shouldResetting = false)
		{
			var conditions = Condition.Equal(nameof(PermissionModel.MemberId), memberId) & Condition.Equal(nameof(PermissionModel.MemberType), memberType);

			if(!string.IsNullOrWhiteSpace(target))
				conditions.Add(Condition.Equal(nameof(PermissionModel.Target), target));

			using(var transaction = new Zongsoft.Transactions.Transaction())
			{
				int count = 0;

				//清空指定成员的所有权限设置
				if(shouldResetting || permissions == null)
					count = this.DataAccess.Delete<PermissionModel>(conditions);

				//写入指定的权限设置集到数据库中
				if(permissions != null)
					count = this.DataAccess.UpsertMany(
						permissions.Select(p => new PermissionModel(memberId, memberType, (string.IsNullOrEmpty(target) ? p.Target : target), p.Action, p.Granted)));

				//提交事务
				transaction.Commit();

				return count;
			}
		}

		public int RemovePermissions(uint memberId, MemberType memberType, string target = null, string action = null)
		{
			var criteria = Condition.Equal(nameof(PermissionModel.MemberId), memberId) &
			               Condition.Equal(nameof(PermissionModel.MemberType), memberType);

			if(target != null && target.Length > 0)
				criteria.Add(Condition.Equal(nameof(PermissionModel.Target), target));

			if(action != null && action.Length > 0)
				criteria.Add(Condition.Equal(nameof(PermissionModel.Action), action));

			return this.DataAccess.Delete<PermissionModel>(criteria);
		}

		public IEnumerable<PermissionFilterModel> GetPermissionFilters(uint memberId, MemberType memberType, string target = null)
		{
			var conditions = Condition.Equal(nameof(PermissionFilterModel.MemberId), memberId) & Condition.Equal(nameof(PermissionFilterModel.MemberType), memberType);

			if(!string.IsNullOrWhiteSpace(target))
				conditions.Add(Condition.Equal(nameof(PermissionFilterModel.Target), target));

			return this.DataAccess.Select<PermissionFilterModel>(conditions);
		}

		public int SetPermissionFilters(uint memberId, MemberType memberType, IEnumerable<PermissionFilterModel> permissionFilters, bool shouldResetting = false) =>
			this.SetPermissionFilters(memberId, memberType, null, permissionFilters, shouldResetting);

		public int SetPermissionFilters(uint memberId, MemberType memberType, string target, IEnumerable<PermissionFilterModel> permissionFilters, bool shouldResetting = false)
		{
			var conditions = Condition.Equal(nameof(PermissionFilterModel.MemberId), memberId) & Condition.Equal(nameof(PermissionFilterModel.MemberType), memberType);

			if(!string.IsNullOrWhiteSpace(target))
				conditions.Add(Condition.Equal(nameof(PermissionFilterModel.Target), target));

			using(var transaction = new Zongsoft.Transactions.Transaction())
			{
				int count = 0;

				//清空指定成员的所有权限设置
				if(shouldResetting || permissionFilters == null)
					count = this.DataAccess.Delete<PermissionFilterModel>(conditions);

				//插入指定的权限设置集到数据库中
				if(permissionFilters != null)
					count = this.DataAccess.UpsertMany(
						permissionFilters.Select(p => new PermissionFilterModel(memberId, memberType, (string.IsNullOrEmpty(target) ? p.Target : target), p.Action, p.Filter)));

				//提交事务
				transaction.Commit();

				return count;
			}
		}

		public int RemovePermissionFilters(uint memberId, MemberType memberType, string target = null, string action = null)
		{
			var criteria = Condition.Equal(nameof(PermissionModel.MemberId), memberId) &
			               Condition.Equal(nameof(PermissionModel.MemberType), memberType);

			if(target != null && target.Length > 0)
				criteria.Add(Condition.Equal(nameof(PermissionModel.Target), target));

			if(action != null && action.Length > 0)
				criteria.Add(Condition.Equal(nameof(PermissionModel.Action), action));

			return this.DataAccess.Delete<PermissionFilterModel>(criteria);
		}
		#endregion
	}
}
