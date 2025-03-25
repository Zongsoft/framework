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

namespace Zongsoft.Security.Web.Controllers;

[Area(Module.NAME)]
[ControllerName("Roles")]
[Authorize(Roles = $"{IRole.Administrators},{IRole.Security}")]
public partial class RoleController : ControllerBase
{
	#region 公共属性
	public IRoleService Service => Authentication.Servicer.Roles;
	#endregion

	#region 公共方法
	[HttpGet("{id?}")]
	public async Task<IActionResult> Get(string id, [FromQuery]Paging page = null, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id) || id == "*" || id.Contains(':'))
			return this.Paginate(page ??= Paging.First(), this.Service.FindAsync(id, this.Request.Headers.GetDataSchema(), page, cancellation));

		var result = await this.Service.GetAsync(new Identifier(typeof(IRole), id), this.Request.Headers.GetDataSchema(), cancellation);
		return result != null ? this.Ok(result) : this.NoContent();
	}

	[HttpPost]
	[HttpPost("Role")]
	public async Task<IActionResult> Create(CancellationToken cancellation = default)
	{
		var model = await this.GetModelAsync(cancellation);
		if(model is IActionResult result)
			return result;

		if(await this.Service.CreateAsync((IRole)model, cancellation))
			return this.CreatedAtAction(nameof(Get), new { id = ((IIdentifiable)model).Identifier.Value }, model);

		return this.Conflict();
	}

	[HttpDelete("{id}")]
	public async Task<IActionResult> Delete(string id, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.BadRequest();

		return await this.Service.DeleteAsync(new Identifier(typeof(IRole), id), cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpDelete]
	public async Task<IActionResult> Delete(CancellationToken cancellation = default)
	{
		var content = await this.Request.ReadAsStringAsync();

		if(string.IsNullOrWhiteSpace(content))
			return this.BadRequest();

		var ids = Zongsoft.Common.StringExtension.Slice<uint>(content, [',', ';', '\n'], uint.TryParse).ToArray();

		if(ids == null || ids.Length == 0)
			return this.BadRequest();

		var count = await this.Service.DeleteAsync(ids.Select(id => new Identifier(typeof(IRole), id)), cancellation);
		return count > 0 ? this.Content(count.ToString()) : this.NotFound();
	}

	[HttpPut("{id:required}")]
	[HttpPatch("{id:required}")]
	public async Task<IActionResult> Update(string id, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.BadRequest();

		var model = await this.GetModelAsync(cancellation);
		if(model is IActionResult result)
			return result;

		((IRole)model).Identifier = new(typeof(IRole), id);
		return await this.Service.UpdateAsync((IRole)model, cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpPatch("{id}/Name")]
	public async Task<IActionResult> Rename(string id, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.BadRequest();

		var content = await this.Request.ReadAsStringAsync();

		if(string.IsNullOrWhiteSpace(content))
			return this.BadRequest();

		return await this.Service.RenameAsync(new Identifier(typeof(IRole), id), content, cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpHead("{id:required}")]
	[HttpGet("{id}/[action]")]
	[HttpGet("[action]/{id}")]
	public async Task<IActionResult> Exists(string id, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.BadRequest();

		return await this.Service.ExistsAsync(new Identifier(typeof(IRole), id), cancellation) ? this.NoContent() : this.NotFound();
	}
	#endregion

	#region 保护方法
	protected async ValueTask<object> GetModelAsync(CancellationToken cancellation)
	{
		var modelType = Utility.GetModelType(this.Service, typeof(IRole), typeof(IRoleService<>));
		if(modelType == null)
			return this.StatusCode(StatusCodes.Status501NotImplemented);

		return await Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, modelType, cancellation) is IRole model ? model : this.BadRequest();
	}
	#endregion
}
