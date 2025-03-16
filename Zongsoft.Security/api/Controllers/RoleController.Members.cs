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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using Zongsoft.Components;
using Zongsoft.Web.Http;
using Zongsoft.Security.Privileges;
using Zongsoft.Security.Privileges.Models;

namespace Zongsoft.Security.Controllers;

partial class RoleController
{
	[ControllerName("Members")]
	[Authorize(Roles = $"{IRole.Administrators},{IRole.Security}")]
	public class MemberController : ControllerBase
	{
		public readonly IMemberService<IRole, IMember<IRole>> Service;

		[HttpGet("{id}/Ancestors")]
		public IAsyncEnumerable<IRole> GetAncestors(uint id, CancellationToken cancellation = default)
		{
			return this.Service.GetAncestorsAsync(Member.Role(id), cancellation);
		}

		[HttpGet("{id}/Roles")]
		public IAsyncEnumerable<IRole> GetRoles(uint id, CancellationToken cancellation = default)
		{
			return this.Service.GetRolesAsync(Member.Role(id), cancellation);
		}

		[HttpPut("{id}/Role")]
		public async ValueTask<IActionResult> SetRole(uint id, uint roleId, CancellationToken cancellation = default)
		{
			return await this.Service.SetRoleAsync(Member.User(id), new Identifier(typeof(IRole), roleId), cancellation) ? this.NoContent() : this.NotFound();
		}

		[HttpPut("{id}/Roles")]
		public async ValueTask<IActionResult> SetRoles(uint id, CancellationToken cancellation)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			var roles = Zongsoft.Common.StringExtension.Slice<uint>(content, [',', ';', '\n'], uint.TryParse).Select(id => new Identifier(typeof(IRole), id)).ToArray();
			var count = await this.Service.SetRolesAsync(Member.User(id), roles, cancellation);
			return count > 0 ? this.Content(count.ToString()) : this.NoContent();
		}

		[HttpGet("{id}")]
		public IAsyncEnumerable<IMember<IRole>> Get(uint id, CancellationToken cancellation = default)
		{
			return this.Service.GetAsync(new Identifier(typeof(IRole), id), this.Request.Headers.GetDataSchema(), cancellation);
		}

		[HttpPut("{id}/{memberType}:{memberId}")]
		public async Task<IActionResult> Set(uint id, MemberType memberType, uint memberId, CancellationToken cancellation = default)
		{
			return await this.Service.SetAsync(new Identifier(typeof(IRole), id), Member.Create(memberType, memberId), cancellation) ?
				this.CreatedAtAction(nameof(Get), new { id }, null) : this.NoContent();
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> Set(uint id, [FromBody]IEnumerable<Member> members, [FromQuery]bool reset = false, CancellationToken cancellation = default)
		{
			return await this.Service.SetAsync(new Identifier(typeof(IRole), id), members, reset, cancellation) > 0 ?
				this.CreatedAtAction(nameof(Get), new { id }, null) :
				this.NoContent();
		}

		[HttpDelete("{id}/{memberType}:{memberId}")]
		public async Task<IActionResult> Remove(uint id, MemberType memberType, uint memberId, CancellationToken cancellation = default)
		{
			return await this.Service.RemoveAsync(new Identifier(typeof(IRole), id), Member.Create(memberType, memberId), cancellation) ? this.NoContent() : this.NotFound();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Remove(uint id, CancellationToken cancellation = default)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			var members = content == "*" ? null : content
				.Split([',', ';', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
				.Select(part => Member.Parse(part));

			var count = await this.Service.RemoveAsync(new Identifier(typeof(IRole), id), members, cancellation);
			return count > 0 ? this.Content(count.ToString()) : this.NoContent();
		}
	}
}
