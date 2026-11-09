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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
using Zongsoft.Web.Http;
using Zongsoft.Data;
using Zongsoft.Components;
using Zongsoft.Security.Privileges;

namespace Zongsoft.Security.Web.Controllers;

[Area(Module.NAME)]
[ControllerName("Roles")]
[Authorize(Roles = $"{Roles.Administrators},{Roles.Security}")]
public partial class RoleController : ControllerBase
{
	#region 公共属性
	public IRoleService Service => Authentication.Servicer.Roles;
	#endregion

	#region 公共方法
	[HttpGet("{id?}")]
	public async Task<IActionResult> Get(string id, [FromQuery]string keyword = null, [FromQuery]Paging page = null, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.Paginate(page ??= Paging.First(), this.Service.FindAsync(keyword, this.Request.Headers.GetDataSchema(), page, cancellation));

		var result = await this.Service.GetAsync(new Identifier(Roles.Identifiers.Role, id), this.Request.Headers.GetDataSchema(), cancellation);
		return result != null ? this.Ok(result) : this.NoContent();
	}

	[HttpPost]
	public async Task<IActionResult> Create(CancellationToken cancellation = default)
	{
		var model = await this.GetModelAsync(cancellation);
		if(model is IActionResult result)
			return result;

		if(await this.Service.CreateAsync((IRole)model, cancellation))
			return this.CreatedAtAction(nameof(Get), new { id = ((IIdentifiable)model).Identifier.Value }, model);

		return this.Conflict();
	}

	[HttpDelete("{id:required}")]
	public async Task<IActionResult> Delete(string id, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.BadRequest();

		return await this.Service.DeleteAsync(new Identifier(Roles.Identifiers.Role, id), cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpDelete]
	public async Task<IActionResult> Delete(CancellationToken cancellation = default)
	{
		var content = await this.Request.ReadAsStringAsync(cancellation);
		if(string.IsNullOrWhiteSpace(content))
			return this.BadRequest();

		var ids = Zongsoft.Common.StringExtension.Slice<uint>(content, [',', ';', '\n'], uint.TryParse).ToArray();
		if(ids == null || ids.Length == 0)
			return this.BadRequest();

		var count = await this.Service.DeleteAsync(ids.Select(id => new Identifier(Roles.Identifiers.Role, id)), cancellation);
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

		((IRole)model).Identifier = new(Roles.Identifiers.Role, id);
		return await this.Service.UpdateAsync((IRole)model, cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpPatch("{id:required}/Name")]
	public async Task<IActionResult> Rename(string id, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.BadRequest();

		var content = await this.Request.ReadAsStringAsync(cancellation);
		if(string.IsNullOrWhiteSpace(content))
			return this.BadRequest();

		return await this.Service.RenameAsync(new Identifier(Roles.Identifiers.Role, id), content, cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpPost("{id:required}/[action]")]
	public async Task<IActionResult> EnableAsync(string id, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.BadRequest();

		return await this.Service.EnableAsync(new Identifier(Roles.Identifiers.Role, id), cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpPost("{id:required}/[action]")]
	public async Task<IActionResult> DisableAsync(string id, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.BadRequest();

		return await this.Service.DisableAsync(new Identifier(Roles.Identifiers.Role, id), cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpHead("{id:required}")]
	[HttpGet("{id:required}/[action]")]
	[HttpGet("[action]/{id:required}")]
	public async Task<IActionResult> Exists(string id, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.BadRequest();

		return await this.Service.ExistsAsync(new Identifier(Roles.Identifiers.Role, id), cancellation) ? this.NoContent() : this.NotFound();
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
