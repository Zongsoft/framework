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
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Security.Membership;

namespace Zongsoft.Security.Web.Controllers
{
	[ApiController]
	[Area(Modules.Security)]
	[Authorize(Roles = "Administrators,Security,Securers")]
	[Route("{area}/Roles")]
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
		[HttpGet("{id:long}")]
		public Task<IActionResult> Get(uint id)
		{
			var role = this.RoleProvider.GetRole(id);

			return role != null ?
				Task.FromResult((IActionResult)this.Ok(role)) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpGet("{identity?}")]
		[HttpGet("{namespace:required}:{identity:required}")]
		public Task<IActionResult> Get(string @namespace, string identity, [FromQuery]Paging paging = null)
		{
			object result;

			if(string.IsNullOrEmpty(identity) || identity == "*")
				result = this.RoleProvider.GetRoles(@namespace, paging);
			else
				result = this.RoleProvider.GetRole(identity, @namespace);

			return result != null ?
				Task.FromResult((IActionResult)this.Ok(result)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpDelete("{id:long}")]
		public Task<IActionResult> Delete(uint id)
		{
			return this.RoleProvider.Delete(id) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpDelete("{ids:required}")]
		public Task<IActionResult> Delete(uint[] ids)
		{
			if(ids == null || ids.Length == 0)
				return Task.FromResult((IActionResult)this.BadRequest());

			return this.RoleProvider.Delete(ids) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpPost]
		[ProducesResponseType(StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status409Conflict)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public ActionResult<IRole> Post([FromBody]IRole model)
		{
			if(model == null)
				return this.BadRequest();

			if(this.RoleProvider.Create(model))
				return this.CreatedAtAction(nameof(Get), new { id = model.RoleId }, model);

			return this.Conflict();
		}

		[HttpPatch("{id:long}/Namespace")]
		[HttpPatch("Namespace/{id:long}")]
		public async Task<IActionResult> SetNamespace(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.RoleProvider.SetNamespace(id, content) ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpPatch("{id:long}/Name")]
		[HttpPatch("Name/{id:long}")]
		public async Task<IActionResult> SetName(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.RoleProvider.SetName(id, content) ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpPatch("{id:long}/FullName")]
		[HttpPatch("FullName/{id:long}")]
		public async Task<IActionResult> SetFullName(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.RoleProvider.SetFullName(id, content) ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpPatch("{id:long}/Description")]
		[HttpPatch("Description/{id:long}")]
		public async Task<IActionResult> SetDescription(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.RoleProvider.SetDescription(id, content) ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpHead("{id:long}")]
		[HttpGet("{id:long}/exists")]
		public Task<IActionResult> Exists(uint id)
		{
			return this.RoleProvider.Exists(id) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpHead("{namespace}:{identity}")]
		[HttpGet("exists/{namespace}:{identity}")]
		public Task<IActionResult> Exists(string @namespace, string identity)
		{
			if(string.IsNullOrWhiteSpace(identity))
				return Task.FromResult((IActionResult)this.BadRequest());

			return this.RoleProvider.Exists(identity, @namespace) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}
		#endregion

		#region 成员方法
		[HttpGet("{id:long}/Roles")]
		[HttpGet("Roles/{id:long:required}")]
		public IEnumerable<IRole> GetRoles(uint id)
		{
			return this.MemberProvider.GetRoles(id, MemberType.Role);
		}

		[HttpGet("{id:long}/Members")]
		[HttpGet("Members/{id:long:required}")]
		public IEnumerable<Member> GetMembers(uint id)
		{
			return this.MemberProvider.GetMembers(id, this.Request.GetDataSchema());
		}

		[HttpPost("{id:long}/Member/{memberType}:{memberId:int}")]
		public Task<IActionResult> SetMember(uint id, MemberType memberType, uint memberId)
		{
			return this.MemberProvider.SetMembers(id, new[] { new Member(id, memberId, memberType) }, false) > 0 ?
				Task.FromResult((IActionResult)this.CreatedAtAction(nameof(GetMembers), new { id })) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpPost("{id:long}/Members")]
		[HttpPost("Members/{id:long}")]
		public Task<IActionResult> SetMembers(uint id, [FromBody]IEnumerable<Member> members, [FromQuery]bool reset = false)
		{
			return this.MemberProvider.SetMembers(id, members, reset) > 0 ?
				Task.FromResult((IActionResult)this.CreatedAtAction(nameof(GetMembers), new { id })) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpDelete("{id:long}/Member/{memberType}:{memberId:int}")]
		public Task<IActionResult> RemoveMember(uint id, MemberType memberType, uint memberId)
		{
			return this.MemberProvider.RemoveMember(id, memberId, memberType) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpDelete("{id:long}/Members")]
		[HttpDelete("Members/{id:long}")]
		public Task<IActionResult> RemoveMembers(uint id)
		{
			return this.MemberProvider.RemoveMembers(id) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NoContent());
		}
		#endregion

		#region 授权方法
		[HttpGet("{id:long}/Authorizes")]
		[HttpGet("Authorizes/{id:long}")]
		public IEnumerable<AuthorizationToken> Authorizes(uint id)
		{
			return this.Authorizer.Authorizes(id, MemberType.Role);
		}
		#endregion

		#region 权限方法
		[HttpGet("{id:long}/Permissions/{schemaId?}")]
		public IEnumerable<Permission> GetPermissions(uint id, string schemaId = null)
		{
			return this.PermissionProvider.GetPermissions(id, MemberType.Role, schemaId);
		}

		[HttpPost("{id:long}/Permissions/{schemaId}")]
		public Task<IActionResult> SetPermissions(uint id, string schemaId, [FromBody]IEnumerable<Permission> permissions, [FromQuery]bool reset = false)
		{
			return this.PermissionProvider.SetPermissions(id, MemberType.Role, schemaId, permissions, reset) > 0 ?
				Task.FromResult((IActionResult)this.CreatedAtAction(nameof(GetPermissions), new { id })) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpDelete("{id:long}/Permission/{schemaId}:{actionId}")]
		public Task<IActionResult> RemovePermission(uint id, string schemaId, string actionId)
		{
			if(string.IsNullOrEmpty(schemaId) || string.IsNullOrEmpty(actionId))
				return Task.FromResult((IActionResult)this.BadRequest());

			return this.PermissionProvider.RemovePermissions(id, MemberType.Role, schemaId, actionId) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpDelete("{id:long}/Permissions/{schemaId?}")]
		public Task<IActionResult> RemovePermissions(uint id, string schemaId = null)
		{
			return this.PermissionProvider.RemovePermissions(id, MemberType.Role, schemaId) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpGet("{id:long}/Permission.Filters/{schemaId?}")]
		public IEnumerable<PermissionFilter> GetPermissionFilters(uint id, string schemaId = null)
		{
			return this.PermissionProvider.GetPermissionFilters(id, MemberType.Role, schemaId);
		}

		[HttpPost("{id:long}/Permission.Filters/{schemaId}")]
		public Task<IActionResult> SetPermissionFilters(uint id, string schemaId, [FromBody]IEnumerable<PermissionFilter> permissions, [FromQuery]bool reset = false)
		{
			return this.PermissionProvider.SetPermissionFilters(id, MemberType.Role, schemaId, permissions, reset) > 0 ?
				Task.FromResult((IActionResult)this.CreatedAtAction(nameof(GetPermissionFilters), new { id })) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpDelete("{id:long}/Permission.Filter/{schemaId}:{actionId}")]
		public Task<IActionResult> RemovePermissionFilter(uint id, string schemaId, string actionId)
		{
			if(string.IsNullOrEmpty(schemaId) || string.IsNullOrEmpty(actionId))
				return Task.FromResult((IActionResult)this.BadRequest());

			return this.PermissionProvider.RemovePermissionFilters(id, MemberType.Role, schemaId, actionId) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpDelete("{id:long}/Permission.Filters/{schemaId?}")]
		public Task<IActionResult> RemovePermissionFilters(uint id, string schemaId = null)
		{
			return this.PermissionProvider.RemovePermissionFilters(id, MemberType.Role, schemaId) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}
		#endregion
	}
}
