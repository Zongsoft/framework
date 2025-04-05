﻿/*
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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Security.Membership
{
	public abstract class RoleProviderBase<TRole> : IRoleProvider<TRole> where TRole : IRoleModel
	{
		#region 事件定义
		public event EventHandler<ChangedEventArgs> Changed;
		#endregion

		#region 成员字段
		private IDataAccess _dataAccess;
		#endregion

		#region 构造函数
		protected RoleProviderBase(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 公共属性
		public IDataAccess DataAccess
		{
			get
			{
				if(_dataAccess == null)
				{
					_dataAccess = this.ServiceProvider.ResolveRequired<IDataAccessProvider>().GetAccessor(MembershipUtility.Security);

					if(_dataAccess != null && !string.IsNullOrEmpty(Mapping.Instance.Role))
					{
						Model.Naming.Map<TRole>(Mapping.Instance.Role);
					}
				}

				return _dataAccess;
			}
		}

		public IServiceProvider ServiceProvider { get; }
		#endregion

		#region 角色管理
		public TRole GetRole(uint roleId)
		{
			return this.DataAccess.Select<TRole>(Condition.Equal(nameof(IRoleModel.RoleId), roleId)).FirstOrDefault();
		}

		public TRole GetRole(string name, string @namespace = null)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			return this.DataAccess.Select<TRole>(Condition.Equal(nameof(IRoleModel.Name), name) & this.GetNamespace(@namespace)).FirstOrDefault();
		}

		public IEnumerable<TRole> GetRoles(string @namespace, Paging paging = null)
		{
			return this.DataAccess.Select<TRole>(this.GetNamespace(@namespace), paging);
		}

		public virtual IEnumerable<TRole> Find(string keyword, Paging paging = null)
		{
			return this.DataAccess.Select<TRole>(Condition.Like(nameof(IRoleModel.Name), keyword), paging);
		}

		public bool Exists(uint roleId)
		{
			return this.DataAccess.Exists<TRole>(Condition.Equal(nameof(IRoleModel.RoleId), roleId));
		}

		public bool Exists(string name, string @namespace = null)
		{
			if(string.IsNullOrWhiteSpace(name))
				return false;

			return this.DataAccess.Exists<IRoleModel>(Condition.Equal(nameof(IRoleModel.Name), name) & this.GetNamespace(@namespace));
		}

		public bool SetNamespace(uint roleId, string @namespace)
		{
			if(this.DataAccess.Update<TRole>(
				new { Namespace = string.IsNullOrWhiteSpace(@namespace) ? null : @namespace.Trim() },
				new Condition(nameof(IRoleModel.RoleId), roleId)) > 0)
			{
				this.OnChanged(roleId, nameof(IRoleModel.Namespace), @namespace);
				return true;
			}

			return false;
		}

		public int SetNamespaces(string oldNamespace, string newNamespace)
		{
			return this.DataAccess.Update<TRole>(new
			{
				Namespace = string.IsNullOrWhiteSpace(newNamespace) ? null : newNamespace.Trim(),
			}, new Condition(nameof(IRoleModel.Namespace), oldNamespace));
		}

		public bool SetName(uint roleId, string name)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			//验证指定的名称是否为系统内置名
			if(string.Equals(name, IRoleModel.Administrators, StringComparison.OrdinalIgnoreCase) ||
			   string.Equals(name, IRoleModel.Security, StringComparison.OrdinalIgnoreCase))
				throw new SecurityException("rolename.illegality", "The role name specified to be update cannot be a built-in name.");

			//验证指定的名称是否合法
			this.OnValidateName(name);

			if(this.DataAccess.Update<TRole>(
				new
				{
					Name = name.Trim()
				},
				new Condition(nameof(IRoleModel.RoleId), roleId)) > 0)
			{
				this.OnChanged(roleId, nameof(IRoleModel.Name), name);
				return true;
			}

			return false;
		}

		public bool SetNickname(uint roleId, string nickname)
		{
			if(this.DataAccess.Update<TRole>(
				new
				{
					Nickname = string.IsNullOrWhiteSpace(nickname) ? null : nickname.Trim(),
				},
				new Condition(nameof(IRoleModel.RoleId), roleId)) > 0)
			{
				this.OnChanged(roleId, nameof(IRoleModel.Nickname), nickname);
				return true;
			}

			return false;
		}

		public bool SetDescription(uint roleId, string description)
		{
			if(this.DataAccess.Update<TRole>(
				new
				{
					Description = string.IsNullOrEmpty(description) ? null : description
				}, new Condition(nameof(IRoleModel.RoleId), roleId)) > 0)
			{
				this.OnChanged(roleId, nameof(IRoleModel.Description), description);
				return true;
			}

			return false;
		}

		public int Delete(params uint[] ids)
		{
			if(ids == null || ids.Length < 1)
				return 0;

			return this.DataAccess.Delete<TRole>(
				Condition.In(nameof(IRoleModel.RoleId), ids) &
				Condition.NotIn(nameof(IRoleModel.Name), IRoleModel.Administrators, IRoleModel.Security),
				"Members,Permissions,PermissionFilters");
		}

		public TRole Create(string name, string @namespace, string fullName = null, string description = null)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			var role = this.CreateRole();

			role.Name = name;
			role.Nickname = fullName;
			role.Namespace = @namespace;
			role.Description = string.IsNullOrEmpty(description) ? null : description;

			return this.Create(role) ? role : default;
		}

		public bool Create(TRole role)
		{
			if(role == null)
				throw new ArgumentNullException(nameof(role));

			return this.Create(new[] { role }) > 0;
		}

		public int Create(IEnumerable<TRole> roles)
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

				//确保角色全称不为空
				if(string.IsNullOrEmpty(role.Nickname))
					role.Nickname = role.Name;
			}

			var count = this.DataAccess.InsertMany(roles);

			if(count > 0)
			{
				foreach(var role in roles)
				{
					if(role != null && role.RoleId > 0)
						this.OnCreated(role);
				}
			}

			return count;
		}

		public bool Update(uint roleId, TRole role)
		{
			if(role == null)
				throw new ArgumentNullException(nameof(role));

			if(!(role is IModel model) || !model.HasChanges())
				return false;

			if(model.HasChanges(nameof(IRoleModel.Name)) && !string.IsNullOrWhiteSpace(role.Name))
			{
				//验证指定的名称是否为系统内置名
				if(string.Equals(role.Name, IRoleModel.Administrators, StringComparison.OrdinalIgnoreCase) ||
				   string.Equals(role.Name, IRoleModel.Security, StringComparison.OrdinalIgnoreCase))
					throw new SecurityException("rolename.illegality", "The role name specified to be update cannot be a built-in name.");

				//验证指定的名称是否合法
				this.OnValidateName(role.Name);
			}

			//验证指定的命名空间是否合规
			if(model.HasChanges(nameof(IRoleModel.Namespace)))
			{
				var @namespace = ApplicationContext.Current.Principal.Identity.GetNamespace();

				if(string.IsNullOrEmpty(@namespace))
					role.Namespace = string.IsNullOrWhiteSpace(role.Namespace) ? null : role.Namespace.Trim();
				else
					role.Namespace = @namespace;
			}

			if(this.DataAccess.Update(role, new Condition(nameof(IRoleModel.RoleId), roleId)) > 0)
			{
				foreach(var entry in model.GetChanges())
				{
					this.OnChanged(roleId, entry.Key, entry.Value);
				}

				return true;
			}

			return false;
		}
		#endregion

		#region 抽象方法
		protected abstract TRole CreateRole();
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
			var validator = this.ServiceProvider.Resolve<IValidator<string>>("role.name");

			if(validator != null)
				validator.Validate(name, message => throw new SecurityException("rolename.illegality", message));
		}
		#endregion

		#region 激发事件
		protected virtual void OnChanged(uint roleId, string propertyName, object propertyValue)
		{
			this.Changed?.Invoke(this, new ChangedEventArgs(roleId, propertyName, propertyValue));
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private Condition GetNamespace(string @namespace)
		{
			return Mapping.Instance.Namespace.GetCondition(Model.Naming.Get<TRole>(), @namespace);
		}
		#endregion
	}
}
