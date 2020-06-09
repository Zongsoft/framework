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
using Zongsoft.Common;
using Zongsoft.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Security.Membership
{
	[Service(typeof(IRoleProvider), typeof(IMemberProvider))]
	public class RoleProvider : IRoleProvider, IMemberProvider
	{
		#region 事件定义
		public event EventHandler<ChangedEventArgs> Changed;
		#endregion

		#region 成员字段
		private IDataAccess _dataAccess;
		private ICensorship _censorship;
		private readonly IServiceProvider _services;
		#endregion

		#region 构造函数
		public RoleProvider(IServiceProvider serviceProvider)
		{
			_services = serviceProvider;
		}
		#endregion

		#region 公共属性
		public IDataAccess DataAccess
		{
			get => _dataAccess ?? (_dataAccess = _services.GetRequiredService<IDataAccessProvider>().GetAccessor(Modules.Security));
			set => _dataAccess = value ?? throw new ArgumentNullException();
		}

		[ServiceDependency]
		public ICensorship Censorship
		{
			get => _censorship;
			set => _censorship = value;
		}

		public ISequence Sequence
		{
			get => _dataAccess?.Sequence;
		}
		#endregion

		#region 角色管理
		public IRole GetRole(uint roleId)
		{
			return this.DataAccess.Select<IRole>(Condition.Equal(nameof(IRole.RoleId), roleId)).FirstOrDefault();
		}

		public IRole GetRole(string name, string @namespace = null)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			return this.DataAccess.Select<IRole>(Condition.Equal(nameof(IRole.Name), name) & this.GetNamespace(@namespace)).FirstOrDefault();
		}

		public IEnumerable<IRole> GetRoles(string @namespace, Paging paging = null)
		{
			return this.DataAccess.Select<IRole>(this.GetNamespace(@namespace), paging);
		}

		public bool Exists(uint roleId)
		{
			return this.DataAccess.Exists<IRole>(Condition.Equal(nameof(IRole.RoleId), roleId));
		}

		public bool Exists(string name, string @namespace = null)
		{
			if(string.IsNullOrWhiteSpace(name))
				return false;

			return this.DataAccess.Exists<IRole>(Condition.Equal(nameof(IRole.Name), name) & this.GetNamespace(@namespace));
		}

		public bool SetNamespace(uint roleId, string @namespace)
		{
			if(this.DataAccess.Update<IRole>(
				new { Namespace = string.IsNullOrWhiteSpace(@namespace) ? null : @namespace.Trim() },
				new Condition(nameof(IRole.RoleId), roleId)) > 0)
			{
				this.OnChanged(roleId, nameof(IRole.Namespace), @namespace);
				return true;
			}

			return false;
		}

		public int SetNamespaces(string oldNamespace, string newNamespace)
		{
			return this.DataAccess.Update<IRole>(new
				{
					Namespace = string.IsNullOrWhiteSpace(newNamespace) ? null : newNamespace.Trim(),
				}, new Condition(nameof(IRole.Namespace), oldNamespace));
		}

		public bool SetName(uint roleId, string name)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			//验证指定的名称是否为系统内置名
			if(string.Equals(name, Role.Administrators, StringComparison.OrdinalIgnoreCase) ||
			   string.Equals(name, Role.Security, StringComparison.OrdinalIgnoreCase))
				throw new SecurityException("rolename.illegality", "The role name specified to be update cannot be a built-in name.");

			//验证指定的名称是否合法
			this.OnValidateName(name);

			//确保角色名是审核通过的
			this.Censor(name);

			if(this.DataAccess.Update<IRole>(
				new
				{
					Name = name.Trim()
				},
				new Condition(nameof(IRole.RoleId), roleId)) > 0)
			{
				this.OnChanged(roleId, nameof(IRole.Name), name);
				return true;
			}

			return false;
		}

		public bool SetFullName(uint roleId, string fullName)
		{
			if(this.DataAccess.Update<IRole>(
				new
				{
					FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim(),
				},
				new Condition(nameof(IRole.RoleId), roleId)) > 0)
			{
				this.OnChanged(roleId, nameof(IRole.FullName), fullName);
				return true;
			}

			return false;
		}

		public bool SetDescription(uint roleId, string description)
		{
			if(this.DataAccess.Update<IRole>(
				new
				{
					Description = string.IsNullOrEmpty(description) ? null : description
				}, new Condition(nameof(IRole.RoleId), roleId)) > 0)
			{
				this.OnChanged(roleId, nameof(IRole.Description), description);
				return true;
			}

			return false;
		}

		public int Delete(params uint[] ids)
		{
			if(ids == null || ids.Length < 1)
				return 0;

			return this.DataAccess.Delete<IRole>(
				Condition.In(nameof(IRole.RoleId), ids) &
				Condition.NotIn(nameof(IRole.Name), Role.Administrators, Role.Security),
				"Members,Children,Permissions,PermissionFilters");
		}

		public IRole Create(string name, string @namespace, string fullName = null, string description = null)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			var role = Model.Build<IRole>();
			role.Name = name;
			role.FullName = fullName;
			role.Namespace = @namespace;
			role.Description = description;

			return this.Create(role) ? role : null;
		}

		public bool Create(IRole role)
		{
			if(role == null)
				throw new ArgumentNullException(nameof(role));

			return this.Create(new[] { role }) > 0;
		}

		public int Create(IEnumerable<IRole> roles)
		{
			if(roles == null)
				return 0;

			foreach(var role in roles)
			{
				if(role == null)
					continue;

				//如果未指定角色名，则为其设置一个随机名
				if(string.IsNullOrWhiteSpace(role.Name))
					role.Name = "R" + Randomizer.GenerateString();

				//如果当前用户的命名空间不为空，则新增角色的命名空间必须与当前用户一致
				var @namespace = ApplicationContext.Current.Principal.Identity.GetNamespace();

				if(string.IsNullOrEmpty(@namespace))
					role.Namespace = string.IsNullOrWhiteSpace(role.Namespace) ? null : role.Namespace.Trim();
				else
					role.Namespace = @namespace;

				//验证指定的名称是否合法
				this.OnValidateName(role.Name);

				//确保角色名是审核通过的
				this.Censor(role.Name);

				//确保角色全称不为空
				if(string.IsNullOrEmpty(role.FullName))
					role.FullName = role.Name;
			}

			return this.DataAccess.InsertMany<IRole>(roles);
		}

		public bool Update(uint roleId, IRole role)
		{
			if(role == null)
				throw new ArgumentNullException(nameof(role));

			if(!(role is IModel model) || !model.HasChanges())
				return false;

			if(model.HasChanges(nameof(IRole.Name)) && !string.IsNullOrWhiteSpace(role.Name))
			{
				//验证指定的名称是否为系统内置名
				if(string.Equals(role.Name, Role.Administrators, StringComparison.OrdinalIgnoreCase) ||
				   string.Equals(role.Name, Role.Security, StringComparison.OrdinalIgnoreCase))
					throw new SecurityException("rolename.illegality", "The role name specified to be update cannot be a built-in name.");

				//验证指定的名称是否合法
				this.OnValidateName(role.Name);

				//确保角色名是审核通过的
				this.Censor(role.Name);
			}

			//验证指定的命名空间是否合规
			if(model.HasChanges(nameof(IRole.Namespace)))
			{
				var @namespace = ApplicationContext.Current.Principal.Identity.GetNamespace();

				if(string.IsNullOrEmpty(@namespace))
					role.Namespace = string.IsNullOrWhiteSpace(role.Namespace) ? null : role.Namespace.Trim();
				else
					role.Namespace = @namespace;
			}

			if(this.DataAccess.Update(role, new Condition(nameof(IRole.RoleId), roleId)) > 0)
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

		#region 成员管理
		public IEnumerable<IRole> GetRoles(uint memberId, MemberType memberType)
		{
			return this.DataAccess.Select<Member>(
				Condition.Equal(nameof(Member.MemberId), memberId) & Condition.Equal(nameof(Member.MemberType), memberType),
				"*, Role{*}").Map(p => p.Role);
		}

		public IEnumerable<Member> GetMembers(uint roleId, string schema = null)
		{
			return this.DataAccess.Select<Member>(Condition.Equal(nameof(Member.RoleId), roleId), schema);
		}

		public bool SetMember(Member member)
		{
			if(member.RoleId == 0 || member.MemberId == 0)
				return false;

			return this.DataAccess.Upsert(member) > 0;
		}

		public int SetMembers(IEnumerable<Member> members)
		{
			if(members == null)
				return 0;

			return this.DataAccess.UpsertMany(members);
		}

		public int SetMembers(uint roleId, params Member[] members)
		{
			if(members == null || members.Length == 0)
				return 0;

			return this.SetMembers(roleId, members, false);
		}

		public int SetMembers(uint roleId, IEnumerable<Member> members, bool shouldResetting = false)
		{
			if(members == null)
				return 0;

			using(var transaction = new Zongsoft.Transactions.Transaction())
			{
				int count = 0;

				//清空指定角色的所有成员
				if(shouldResetting)
					count = this.DataAccess.Delete<Member>(Condition.Equal(nameof(Member.RoleId), roleId));

				//写入指定的角色成员集到数据库中
				count = this.DataAccess.UpsertMany<Member>(members.Select(m => new Member(roleId, m.MemberId, m.MemberType)));

				//提交事务
				transaction.Commit();

				return count;
			}
		}

		public bool RemoveMember(uint roleId, uint memberId, MemberType memberType)
		{
			return this.DataAccess.Delete<Member>(
				Condition.Equal(nameof(Member.RoleId), roleId) &
				Condition.Equal(nameof(Member.MemberId), memberId) &
				Condition.Equal(nameof(Member.MemberType), memberType)) > 0;
		}

		public int RemoveMembers(uint roleId)
		{
			return this.DataAccess.Delete<Member>(Condition.Equal(nameof(Member.RoleId), roleId));
		}
		#endregion

		#region 虚拟方法
		protected virtual void OnValidateName(string name)
		{
			var validator = _services?.GetMatchedService<IValidator<string>>("role.name");

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
		private void Censor(string name)
		{
			var censorship = this.Censorship;

			if(censorship != null && censorship.IsBlocked(name, Zongsoft.Security.Censorship.KEY_NAMES, Zongsoft.Security.Censorship.KEY_SENSITIVES))
				throw new CensorshipException(string.Format("Illegal '{0}' name of role.", name));
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private Condition GetNamespace(string @namespace)
		{
			if(string.IsNullOrEmpty(@namespace))
				return Condition.Equal(nameof(IRole.Namespace), ApplicationContext.Current.Principal.Identity.GetNamespace());
			else if(@namespace != "*")
				return Condition.Equal(nameof(IRole.Namespace), @namespace);

			return null;
		}
		#endregion
	}
}
