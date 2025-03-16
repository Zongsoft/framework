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

[Area(Module.NAME)]
[ControllerName("Roles")]
[Authorize(Roles = $"{IRole.Administrators},{IRole.Security}")]
public partial class RoleController : ControllerBase
{
	#region 构造函数
	public RoleController(IRoleService<IRole> service) => this.Service = service;
	#endregion

	#region 公共属性
	public IRoleService<IRole> Service { get; }
	#endregion

	#region 公共方法
	[HttpGet("{identifier?}")]
	public async Task<IActionResult> Get(string identifier, [FromQuery]Paging page = null, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(identifier) || identifier == "*")
			return this.Paginate(page ??= Paging.First(), this.Service.FindAsync(identifier, this.Request.Headers.GetDataSchema(), page, cancellation));

		var result = await this.Service.GetAsync(new Identifier(typeof(IRole), identifier), this.Request.Headers.GetDataSchema(), cancellation);
		return result != null ? this.Ok(result) : this.NoContent();
	}

	[HttpPost]
	public async Task<ActionResult> Create([FromBody]RoleModel model, CancellationToken cancellation = default)
	{
		if(model == null)
			return this.BadRequest();

		if(await this.Service.CreateAsync(model, cancellation))
			return this.CreatedAtAction(nameof(Get), new { id = model.RoleId }, model);

		return this.Conflict();
	}

	[HttpDelete("{id}")]
	public async Task<IActionResult> Delete(uint id, CancellationToken cancellation = default)
	{
		if(id == 0)
			return this.BadRequest();

		return await this.Service.DeleteAsync(new Identifier(typeof(IRole), id), cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpDelete]
	public async Task<IActionResult> Delete(CancellationToken cancellation = default)
	{
		var content = await this.Request.ReadAsStringAsync();

		if(string.IsNullOrWhiteSpace(content))
			return this.BadRequest();

		var ids = Zongsoft.Common.StringExtension.Slice<uint>(content, [',', '|'], uint.TryParse).ToArray();

		if(ids == null || ids.Length == 0)
			return this.BadRequest();

		return await this.Service.DeleteAsync(ids.Select(id => new Identifier(typeof(IRole), id)), cancellation) > 0 ? this.NoContent() : this.NotFound();
	}

	[HttpPut("{id:required}")]
	[HttpPatch("{id:required}")]
	public async Task<IActionResult> Update(uint id, [FromBody]RoleModel model, CancellationToken cancellation = default)
	{
		if(id == 0)
			return this.BadRequest();

		model.RoleId = id;
		return await this.Service.UpdateAsync(model, cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpPatch("{id}/Name")]
	public async Task<IActionResult> Rename(uint id, CancellationToken cancellation = default)
	{
		var content = await this.Request.ReadAsStringAsync();

		if(string.IsNullOrWhiteSpace(content))
			return this.BadRequest();

		return await this.Service.RenameAsync(new Identifier(typeof(IRole), id), content, cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpHead("{id:required}")]
	[HttpGet("{id}/[action]")]
	public async Task<IActionResult> Exists(uint id, CancellationToken cancellation = default)
	{
		return await this.Service.ExistsAsync(new Identifier(typeof(IRole), id), cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpHead("{namespace}:{name}")]
	public async Task<IActionResult> Exists(string @namespace, string name)
	{
		if(string.IsNullOrWhiteSpace(name))
			return this.BadRequest();

		return await this.Service.ExistsAsync(new Identifier(typeof(IRole), $"{@namespace}:{name}")) ? this.NoContent() : this.NotFound();
	}
	#endregion
}
