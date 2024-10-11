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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Web;
using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Security.Membership;

namespace Zongsoft.Security.Web.Controllers
{
	[Area(Module.NAME)]
	[Authorize(Roles = IRoleModel.Administrators + "," + IRoleModel.Security)]
	[Route("{area}/Roles")]
	public abstract class RoleControllerBase<TRole> : ControllerBase where TRole : IRoleModel
	{
		#region 成员字段
		private IAuthorizer _authorizer;
		private IRoleProvider<TRole> _roleProvider;
		private IMemberProvider<IRoleModel, IUserModel> _memberProvider;
		private IPermissionProvider _permissionProvider;
		#endregion

		#region 构造函数
		protected RoleControllerBase() { }
		#endregion

		#region 公共属性
		[ServiceDependency(IsRequired = true)]
		public IAuthorizer Authorizer
		{
			get => _authorizer;
			set => _authorizer = value ?? throw new ArgumentNullException();
		}

		[ServiceDependency(IsRequired = true)]
		public IRoleProvider<TRole> RoleProvider
		{
			get => _roleProvider;
			set => _roleProvider = value ?? throw new ArgumentNullException();
		}

		[ServiceDependency(IsRequired = true)]
		public IMemberProvider<IRoleModel, IUserModel> MemberProvider
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

		[HttpGet("{name?}")]
		[HttpGet("{namespace:required}:{name:required}")]
		public Task<IActionResult> Get(string @namespace, string name, [FromQuery]Paging page = null)
		{
			if(string.IsNullOrEmpty(name) || name == "*")
				return Task.FromResult(this.Paginate(page ??= Paging.First(), this.RoleProvider.GetRoles(@namespace, page)));

			var result = this.RoleProvider.GetRole(name, @namespace);

			return result != null ?
				Task.FromResult((IActionResult)this.Ok(result)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpDelete("{id}")]
		public Task<IActionResult> Delete(uint id)
		{
			if(id == 0)
				return Task.FromResult((IActionResult)this.BadRequest());

			return this.RoleProvider.Delete(id) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpDelete]
		public async Task<IActionResult> Delete()
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			var ids = Common.StringExtension.Slice<uint>(content, new[] { ',', '|' }, uint.TryParse).ToArray();

			if(ids == null || ids.Length == 0)
				return this.BadRequest();

			return this.RoleProvider.Delete(ids) > 0 ?
				(IActionResult)this.NoContent() :
				(IActionResult)this.NotFound();
		}

		[HttpPost]
		public ActionResult<TRole> Create([FromBody] TRole model)
		{
			if(model == null)
				return this.BadRequest();

			if(this.RoleProvider.Create(model))
				return this.CreatedAtAction(nameof(Get), new { id = model.RoleId }, model);

			return this.Conflict();
		}

		[HttpPut("{id:required}")]
		[HttpPatch("{id:required}")]
		public Task<IActionResult> Update(uint id, [FromBody] TRole model)
		{
			if(id == 0)
				return Task.FromResult((IActionResult)this.BadRequest());

			return this.RoleProvider.Update(id, model) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpPatch("{id}/Namespace")]
		[HttpPatch("Namespace/{id}")]
		public async Task<IActionResult> SetNamespace(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.RoleProvider.SetNamespace(id, content) ? (IActionResult)this.NoContent() : this.NotFound();
		}

		[HttpPatch("{id}/Name")]
		[HttpPatch("Name/{id}")]
		public async Task<IActionResult> SetName(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.RoleProvider.SetName(id, content) ? (IActionResult)this.NoContent() : this.NotFound();
		}

		[HttpPatch("{id}/Nickname")]
		[HttpPatch("Nickname/{id}")]
		public async Task<IActionResult> SetNickname(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.RoleProvider.SetNickname(id, content) ? (IActionResult)this.NoContent() : this.NotFound();
		}

		[HttpPatch("{id}/Description")]
		[HttpPatch("Description/{id}")]
		public async Task<IActionResult> SetDescription(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.RoleProvider.SetDescription(id, content) ? (IActionResult)this.NoContent() : this.NotFound();
		}

		[HttpHead("{id}")]
		[HttpGet("{id}/exists")]
		public Task<IActionResult> Exists(uint id)
		{
			return this.RoleProvider.Exists(id) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpHead("{namespace}:{name}")]
		[HttpGet("exists/{namespace}:{name}")]
		public Task<IActionResult> Exists(string @namespace, string name)
		{
			if(string.IsNullOrWhiteSpace(name))
				return Task.FromResult((IActionResult)this.BadRequest());

			return this.RoleProvider.Exists(name, @namespace) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}
		#endregion

		#region 成员操作
		[HttpGet("{id}/Ancestors")]
		[HttpGet("Ancestors/{id:required}")]
		public Task<IActionResult> GetAncestors(uint id)
		{
			var roles = this.MemberProvider.GetAncestors(id, MemberType.Role);

			return roles != null ?
				Task.FromResult((IActionResult)this.Ok(roles)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpGet("{id}/Roles")]
		[HttpGet("Roles/{id:required}")]
		public Task<IActionResult> GetRoles(uint id)
		{
			var roles = this.MemberProvider.GetRoles(id, MemberType.Role);

			return roles != null ?
				Task.FromResult((IActionResult)this.Ok(roles)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpPut("{id}/Roles")]
		[HttpPut("Roles/{id}")]
		public async Task<IActionResult> SetRoles(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			var members = Zongsoft.Common.StringExtension.Slice<uint>(content, ',', uint.TryParse).Select(roleId => new Member(roleId, id, MemberType.Role));
			return this.MemberProvider.SetMembers(members) > 0 ? (IActionResult)this.CreatedAtAction(nameof(GetRoles), new { id }, members) : this.NotFound();
		}

		[HttpGet("{id}/Members")]
		[HttpGet("Members/{id:required}")]
		public Task<IActionResult> GetMembers(uint id)
		{
			var members = this.MemberProvider.GetMembers(id, this.Request.GetDataSchema());

			return members != null ?
				Task.FromResult((IActionResult)this.Ok(members)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpPut("{id}/Member/{memberType}:{memberId:int}")]
		public Task<IActionResult> SetMember(uint id, MemberType memberType, uint memberId)
		{
			return this.MemberProvider.SetMember(id, memberId, memberType) ?
				Task.FromResult((IActionResult)this.CreatedAtAction(nameof(GetMembers), new { id }, null)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpPut("{id}/Members")]
		[HttpPut("Members/{id}")]
		public Task<IActionResult> SetMembers(uint id, [FromBody] IEnumerable<Member> members, [FromQuery] bool reset = false)
		{
			return this.MemberProvider.SetMembers(id, members, reset) > 0 ?
				Task.FromResult((IActionResult)this.CreatedAtAction(nameof(GetMembers), new { id }, null)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpDelete("{id}/Member/{memberType}:{memberId:int}")]
		public Task<IActionResult> RemoveMember(uint id, MemberType memberType, uint memberId)
		{
			return this.MemberProvider.RemoveMember(id, memberId, memberType) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpDelete("{id}/Members")]
		[HttpDelete("Members/{id}")]
		public Task<IActionResult> RemoveMembers(uint id)
		{
			return this.MemberProvider.RemoveMembers(id) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NoContent());
		}
		#endregion

		#region 授权操作
		[HttpGet("{id}/Authorizes")]
		[HttpGet("Authorizes/{id}")]
		public IActionResult Authorizes(uint id)
		{
			if(id == 0)
				return this.BadRequest();

			return this.Ok(this.Authorizer.Authorizes(id, MemberType.Role).Select(p =>
				p.Target + ":" + string.Join(',', p.Actions.Select(a => a.Name).ToArray())
			));
		}
		#endregion

		#region 权限操作
		[HttpGet("{id}/Permissions/{target?}")]
		public Task<IActionResult> GetPermissions(uint id, string target = null)
		{
			var result = this.PermissionProvider.GetPermissions(id, MemberType.Role, target)
				.GroupBy(p => p.Target)
				.ToDictionary(group => group.Key, elements => elements.Select(element => element.Action + ":" + element.Granted.ToString()));

			return (result != null && result.Count > 0) ?
				Task.FromResult((IActionResult)this.Ok(result)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpPut("{id}/Permissions")]
		public Task<IActionResult> SetPermissions(uint id, [FromBody] IEnumerable<PermissionModel> permissions, [FromQuery] bool reset = false)
		{
			return this.PermissionProvider.SetPermissions(id, MemberType.Role, permissions, reset) > 0 ?
				Task.FromResult((IActionResult)this.CreatedAtAction(nameof(GetPermissions), new { id }, null)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpDelete("{id}/Permission/{target}:{actionId}")]
		public Task<IActionResult> RemovePermission(uint id, string target, string actionId)
		{
			if(string.IsNullOrEmpty(target) || string.IsNullOrEmpty(actionId))
				return Task.FromResult((IActionResult)this.BadRequest());

			return this.PermissionProvider.RemovePermissions(id, MemberType.Role, target, actionId) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpDelete("{id}/Permissions/{target?}")]
		public Task<IActionResult> RemovePermissions(uint id, string target = null)
		{
			return this.PermissionProvider.RemovePermissions(id, MemberType.Role, target) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpGet("{id}/Permission.Filters/{target?}")]
		public Task<IActionResult> GetPermissionFilters(uint id, string target = null)
		{
			var result = this.PermissionProvider.GetPermissionFilters(id, MemberType.Role, target)
				.GroupBy(p => p.Target)
				.ToDictionary(group => group.Key, elements => elements.Select(element => element.Action + ":" + element.Filter));

			return (result != null && result.Count > 0) ?
				Task.FromResult((IActionResult)this.Ok(result)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpPut("{id}/Permission.Filters")]
		public Task<IActionResult> SetPermissionFilters(uint id, [FromBody] IEnumerable<PermissionFilterModel> permissions, [FromQuery] bool reset = false)
		{
			return this.PermissionProvider.SetPermissionFilters(id, MemberType.Role, permissions, reset) > 0 ?
				Task.FromResult((IActionResult)this.CreatedAtAction(nameof(GetPermissionFilters), new { id }, null)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpDelete("{id}/Permission.Filter/{target}:{actionId}")]
		public Task<IActionResult> RemovePermissionFilter(uint id, string target, string actionId)
		{
			if(string.IsNullOrEmpty(target) || string.IsNullOrEmpty(actionId))
				return Task.FromResult((IActionResult)this.BadRequest());

			return this.PermissionProvider.RemovePermissionFilters(id, MemberType.Role, target, actionId) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpDelete("{id}/Permission.Filters/{target?}")]
		public Task<IActionResult> RemovePermissionFilters(uint id, string target = null)
		{
			return this.PermissionProvider.RemovePermissionFilters(id, MemberType.Role, target) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}
		#endregion
	}
}
