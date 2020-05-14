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
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Security.Membership;

namespace Zongsoft.Security.Web.Controllers
{
	[Area(Modules.Security)]
	[Authorize(Roles = "Administrators, Security, Securities")]
	[Authorization(Roles = "Security, Securities")]
	public class RoleController : ControllerBase
	{
		#region 成员字段
		private IAuthorizer _authorizer;
		private IRoleProvider _roleProvider;
		private IMemberProvider _memberProvider;
		private IPermissionProvider _permissionProvider;
		#endregion

		#region 公共属性
		[ServiceDependency(IsRequired = true)]
		public IAuthorizer Authorizer
		{
			get => _authorizer;
			set => _authorizer = value ?? throw new ArgumentNullException();
		}

		[ServiceDependency(IsRequired = true)]
		public IRoleProvider RoleProvider
		{
			get => _roleProvider;
			set => _roleProvider = value ?? throw new ArgumentNullException();
		}

		[ServiceDependency(IsRequired = true)]
		public IMemberProvider MemberProvider
		{
			get => _memberProvider;
			set => _memberProvider = value ?? throw new ArgumentNullException();
		}

		[ServiceDependency(IsRequired = true)]
		public IPermissionProvider PermissionProvider
		{
			get => _permissionProvider;
			set => _permissionProvider = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 公共方法
		[Authorization]
		public virtual IActionResult Get(string id = null, [FromQuery]Paging paging = null)
		{
			IRole role;

			//如果标识为空或星号，则进行多角色查询
			if(string.IsNullOrEmpty(id) || id == "*")
				return this.Ok(this.RoleProvider.GetRoles(id, paging));

			//确认角色编号及标识
			var roleId = Utility.ResolvePattern(id, out var identity, out var @namespace, out _);

			//如果ID参数是数字则以编号方式返回唯一的角色信息
			if(roleId > 0)
			{
				role = this.RoleProvider.GetRole(roleId);
				return role == null ? (IActionResult)this.NotFound() : this.Ok(role);
			}

			//如果角色标识为空或星号，则进行命名空间查询
			if(string.IsNullOrEmpty(identity) || identity == "*")
				return this.Ok(this.RoleProvider.GetRoles(@namespace, paging));

			//返回指定标识的角色信息
			role = this.RoleProvider.GetRole(identity, @namespace);
			return role == null ? (IActionResult)this.NotFound() : this.Ok(role);
		}

		public virtual IActionResult Delete(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return this.BadRequest();

			var count = this.RoleProvider.Delete(Common.StringExtension.Slice<uint>(id, chr => chr == ',' || chr == '|', uint.TryParse).Where(p => p > 0).ToArray());
			return count > 0 ? (IActionResult)this.Ok(count) : this.NotFound();
		}

		public virtual IActionResult Post(IRole model)
		{
			if(model == null)
				return this.BadRequest();

			if(this.RoleProvider.Create(model))
				return this.CreatedAtAction(nameof(Get), model.RoleId);

			return this.Conflict();
		}

		[HttpPatch]
		[ActionName("Namespace")]
		public async Task<IActionResult> SetNamespace(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.RoleProvider.SetNamespace(id, content) ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpPatch]
		[ActionName("Name")]
		public async Task<IActionResult> SetName(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.RoleProvider.SetName(id, content) ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpPatch]
		[ActionName("FullName")]
		public async Task<IActionResult> SetFullName(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.RoleProvider.SetFullName(id, content) ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpPatch]
		[ActionName("Description")]
		public async Task<IActionResult> SetDescription(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.RoleProvider.SetDescription(id, content) ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpGet]
		[AllowAnonymous]
		[Authorization(Suppressed = true)]
		public virtual IActionResult Exists(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return this.BadRequest();

			var userId = Utility.ResolvePattern(id, out var identity, out var @namespace, out _);
			var existed = userId > 0 ?
				this.RoleProvider.Exists(userId) :
				this.RoleProvider.Exists(identity, @namespace);

			return existed ? (IActionResult)this.Ok() : this.NotFound();
		}
		#endregion

		#region 成员方法
		[HttpGet]
		[ActionName("Roles")]
		public IEnumerable<IRole> GetRoles(uint id)
		{
			return this.MemberProvider.GetRoles(id, MemberType.Role);
		}

		[HttpGet]
		[ActionName("Members")]
		public IEnumerable<Member> GetMembers(uint id)
		{
			return this.MemberProvider.GetMembers(id, this.Request.GetDataSchema());
		}

		[HttpPost]
		[ActionName("Member")]
		public IActionResult SetMember(uint id, string args)
		{
			if(string.IsNullOrEmpty(args))
				return this.BadRequest();

			var parts = args.Split(':');

			if(!Enum.TryParse<MemberType>(parts[0], true, out var memberType))
				return this.BadRequest("Invalid value of the member-type argument.");

			if(!uint.TryParse(parts[1], out var memberId))
				return this.BadRequest("Invalid value of the member-id argument.");

			return this.MemberProvider.SetMembers(id, new[] { new Member(id, memberId, memberType) }, false) > 0 ?
				(IActionResult)this.CreatedAtAction(nameof(GetMembers), id) : this.NoContent();
		}

		[HttpPost]
		[ActionName("Members")]
		public IActionResult SetMembers(uint id, [FromBody]IEnumerable<Member> members, [FromQuery]bool reset = false)
		{
			var count = this.MemberProvider.SetMembers(id, members, reset);
			return count > 0 ? (IActionResult)this.CreatedAtAction(nameof(GetMembers), id) : this.NoContent();
		}

		[HttpDelete]
		[ActionName("Member")]
		public IActionResult RemoveMember(uint id, string args)
		{
			if(string.IsNullOrEmpty(args))
				return this.BadRequest();

			var parts = args.Split(':');

			if(!Enum.TryParse<MemberType>(parts[0], true, out var memberType))
				return this.BadRequest("Invalid value of the member-type argument.");

			if(!uint.TryParse(parts[1], out var memberId))
				return this.BadRequest("Invalid value of the member-id argument.");

			return this.MemberProvider.RemoveMember(id, memberId, memberType) ?
				(IActionResult)this.NoContent() : this.NotFound();
		}

		[HttpDelete]
		[ActionName("Members")]
		public IActionResult RemoveMembers(uint id)
		{
			var count = this.MemberProvider.RemoveMembers(id);
			return count > 0 ? (IActionResult)this.Ok(count) : this.NoContent();
		}
		#endregion

		#region 授权方法
		[HttpGet]
		public IEnumerable<AuthorizationToken> Authorizes(uint id)
		{
			return this.Authorizer.Authorizes(id, MemberType.Role);
		}
		#endregion

		#region 权限方法
		[HttpGet]
		[ActionName("Permissions")]
		[Route("{id}/{schemaId}")]
		public IEnumerable<Permission> GetPermissions(uint id, [FromRoute]string schemaId = null)
		{
			return this.PermissionProvider.GetPermissions(id, MemberType.Role, schemaId);
		}

		[HttpPost]
		[ActionName("Permissions")]
		public IActionResult SetPermissions(uint id, [FromRoute()]string schemaId, [FromBody]IEnumerable<Permission> permissions, [FromQuery]bool reset = false)
		{
			var count = this.PermissionProvider.SetPermissions(id, MemberType.Role, schemaId, permissions, reset);
			return count > 0 ? (IActionResult)this.CreatedAtAction(nameof(GetPermissions), id) : this.NoContent();
		}

		[HttpDelete]
		[ActionName("Permission")]
		public IActionResult RemovePermission(uint id, [FromRoute()]string schemaId, [FromRoute()]string actionId)
		{
			if(string.IsNullOrEmpty(schemaId) || string.IsNullOrEmpty(actionId))
				return this.BadRequest();

			return this.PermissionProvider.RemovePermissions(id, MemberType.Role, schemaId, actionId) > 0 ?
				(IActionResult)this.NoContent() : this.NotFound();
		}

		[HttpDelete]
		[ActionName("Permissions")]
		public IActionResult RemovePermissions(uint id, [FromRoute()]string schemaId = null)
		{
			var count = this.PermissionProvider.RemovePermissions(id, MemberType.Role, schemaId);
			return count > 0 ? (IActionResult)this.Ok(count) : this.NotFound();
		}

		[HttpGet]
		[ActionName("PermissionFilters")]
		public IEnumerable<PermissionFilter> GetPermissionFilters(uint id, [FromRoute()]string schemaId = null)
		{
			return this.PermissionProvider.GetPermissionFilters(id, MemberType.Role, schemaId);
		}

		[HttpPost]
		[ActionName("PermissionFilters")]
		public IActionResult SetPermissionFilters(uint id, [FromRoute()]string schemaId, [FromBody]IEnumerable<PermissionFilter> permissions, [FromQuery]bool reset = false)
		{
			var count = this.PermissionProvider.SetPermissionFilters(id, MemberType.Role, schemaId, permissions, reset);
			return count > 0 ? (IActionResult)this.CreatedAtAction(nameof(GetPermissionFilters), id) : this.NoContent();
		}

		[HttpDelete]
		[ActionName("PermissionFilter")]
		public IActionResult RemovePermissionFilter(uint id, [FromRoute()]string schemaId, [FromRoute()]string actionId)
		{
			if(string.IsNullOrEmpty(schemaId) || string.IsNullOrEmpty(actionId))
				return this.BadRequest();

			return this.PermissionProvider.RemovePermissionFilters(id, MemberType.Role, schemaId, actionId) > 0 ?
				(IActionResult)this.NoContent() : this.NotFound();
		}

		[HttpDelete]
		[ActionName("PermissionFilters")]
		public IActionResult RemovePermissionFilters(uint id, [FromRoute()]string schemaId = null)
		{
			var count = this.PermissionProvider.RemovePermissionFilters(id, MemberType.Role, schemaId);
			return count > 0 ? (IActionResult)this.Ok(count) : this.NotFound();
		}
		#endregion
	}
}
