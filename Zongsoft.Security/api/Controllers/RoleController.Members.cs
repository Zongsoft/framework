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
using System.Collections;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Web;
using Zongsoft.Web.Http;
using Zongsoft.Components;
using Zongsoft.Security.Privileges;

namespace Zongsoft.Security.Web.Controllers;

partial class RoleController
{
	[ControllerName("Members")]
	[Authorize(Roles = $"{IRole.Administrators},{IRole.Security}")]
	public class MemberController : ControllerBase
	{
		#region 公共属性
		public IMemberService Service => Authentication.Servicer.Members;
		#endregion

		#region 上级角色
		[ActionName("Ancestors")]
		[HttpGet("/[area]/{id}/[action]")]
		public IActionResult GetAncestors(string id, CancellationToken cancellation = default)
		{
			if(this.Request.Query.TryGetValue("depth", out var text) && int.TryParse(text, out var depth))
				return this.Ok(this.Service.GetAncestorsAsync(Member.Role(id), depth, cancellation));
			else
				return this.Ok(this.Service.GetAncestorsAsync(Member.Role(id), cancellation));
		}

		[ActionName("Parents")]
		[HttpGet("/[area]/{id}/Roles")]
		[HttpGet("/[area]/{id}/[action]")]
		public IAsyncEnumerable<IRole> GetParents(string id, CancellationToken cancellation = default)
		{
			return this.Service.GetParentsAsync(Member.Role(id), cancellation);
		}

		[ActionName("Parent")]
		[HttpPut("/[area]/{id}/Role")]
		[HttpPut("/[area]/{id}/[action]")]
		public async ValueTask<IActionResult> SetParent(string id, string roleId, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(id) || string.IsNullOrEmpty(roleId))
				return this.BadRequest();

			return await this.Service.SetParentAsync(Member.Role(id), new Identifier(typeof(IRole), roleId), cancellation) ? this.NoContent() : this.NotFound();
		}

		[ActionName("Parents")]
		[HttpPut("/[area]/{id}/Roles")]
		[HttpPut("/[area]/{id}/[action]")]
		public async ValueTask<IActionResult> SetParents(string id, CancellationToken cancellation)
		{
			if(string.IsNullOrEmpty(id))
				return this.BadRequest();

			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			var roles = Zongsoft.Common.StringExtension.Slice<uint>(content, [',', ';', '\n'], uint.TryParse).Select(id => new Identifier(typeof(IRole), id)).ToArray();
			var count = await this.Service.SetParentsAsync(Member.Role(id), roles, cancellation);
			return count > 0 ? this.Content(count.ToString()) : this.NoContent();
		}
		#endregion

		#region 下级成员
		[HttpGet("/[area]/{id}/[controller]")]
		public IAsyncEnumerable<IMember> Get(string id, CancellationToken cancellation = default)
		{
			return this.Service.GetAsync(new Identifier(typeof(IRole), id), this.Request.Headers.GetDataSchema(), cancellation);
		}

		[HttpPut("/[area]/{id}/Member/{argument}")]
		public async Task<IActionResult> Set(string id, string argument, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(id) || string.IsNullOrEmpty(argument))
				return this.BadRequest();

			if(!Member.TryParse(argument, out var member))
				return this.BadRequest();

			return await this.Service.SetAsync(new Identifier(typeof(IRole), id), member, cancellation) ?
				this.CreatedAtAction(nameof(Get), new { id }, null) : this.NoContent();
		}

		[HttpPut("/[area]/{id}/[controller]")]
		public async Task<IActionResult> Set(string id, [FromQuery]bool reset = false, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(id))
				return this.BadRequest();

			var members = await this.GetRequestAsync(cancellation);
			if(members == null || !members.Any())
				return this.BadRequest();

			return await this.Service.SetAsync(new Identifier(typeof(IRole), id), members, reset, cancellation) > 0 ?
				this.CreatedAtAction(nameof(Get), new { id }, null) :
				this.NoContent();
		}

		[HttpDelete("/[area]/{id}/Member/{argument}")]
		public async Task<IActionResult> Remove(string id, string argument, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(id) || string.IsNullOrEmpty(argument))
				return this.BadRequest();

			if(!Member.TryParse(argument, out var member))
				return this.BadRequest();

			return await this.Service.RemoveAsync(new Identifier(typeof(IRole), id), member, cancellation) ?
				this.NoContent() : this.NotFound();
		}

		[HttpDelete("/[area]/{id}/[controller]")]
		public async Task<IActionResult> Remove(string id, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(id))
				return this.BadRequest();

			var members = await this.GetRequestAsync(cancellation);
			var count = await this.Service.RemoveAsync(new Identifier(typeof(IRole), id), members, cancellation);
			return count > 0 ? this.Content(count.ToString()) : this.NoContent();
		}
		#endregion

		#region 私有方法
		private async ValueTask<IEnumerable<Member>> GetRequestAsync(CancellationToken cancellation)
		{
			if(this.Request.HasJsonContentType())
			{
				var modelType = Utility.GetModelType(this.Service, typeof(IMember), typeof(IMemberService<,>), 1) ?? throw new NotSupportedException();
				var data = await Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, modelType.MakeArrayType(), cancellation);
				return GetMembers(data);
			}

			var content = await this.Request.ReadAsStringAsync();
			if(string.IsNullOrEmpty(content))
				return null;

			return Common.StringExtension.Slice<Member>(content, [',', ';', '\n'], Member.TryParse);
		}

		private static IEnumerable<Member> GetMembers(object data)
		{
			if(data is IEnumerable entries)
			{
				foreach(var entry in entries.OfType<IMember>())
				{
					if(entry.Member is Member member)
						yield return member;
					else if(entry.MemberId.HasValue)
						yield return new Member(entry.MemberId, entry.MemberType);
				}
			}
		}
		#endregion
	}
}
