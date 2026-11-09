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
using Zongsoft.Collections;
using Zongsoft.Security.Privileges;

namespace Zongsoft.Security.Web.Controllers;

[Area(Module.NAME)]
[ControllerName("Users")]
[Authorize(Roles = $"{Roles.Administrators},{Roles.Security}")]
public partial class UserController : ControllerBase
{
	#region 公共属性
	public IUserService Service => Authentication.Servicer.Users;
	#endregion

	#region 公共方法
	[HttpGet("{id?}")]
	public async Task<IActionResult> Get(string id, [FromQuery]string keyword = null, [FromQuery]Paging page = null, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.Paginate(page ??= Paging.First(), this.Service.FindAsync(keyword, this.Request.Headers.GetDataSchema(), page, cancellation));

		var result = await this.Service.GetAsync(new Identifier(Users.Identifiers.User, id), this.Request.Headers.GetDataSchema(), cancellation);
		return result != null ? this.Ok(result) : this.NoContent();
	}

	[HttpPost]
	public async Task<IActionResult> Create(CancellationToken cancellation = default)
	{
		var model = await this.GetModelAsync(cancellation);
		if(model is IActionResult result)
			return result;

		if(await this.Service.CreateAsync((IUser)model, cancellation))
			return this.CreatedAtAction(nameof(Get), new { id = ((IIdentifiable)model).Identifier.Value }, model);

		return this.Conflict();
	}

	[HttpDelete("{id:required}")]
	public async Task<IActionResult> Delete(string id, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.BadRequest();

		return await this.Service.DeleteAsync(new Identifier(Users.Identifiers.User, id), cancellation) ? this.NoContent() : this.NotFound();
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

		var count = await this.Service.DeleteAsync(ids.Select(id => new Identifier(Users.Identifiers.User, id)), cancellation);
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

		((IUser)model).Identifier = new(Users.Identifiers.User, id);
		return await this.Service.UpdateAsync((IUser)model, cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpPatch("{id:required}/Name")]
	public async Task<IActionResult> Rename(string id, CancellationToken cancellation = default)
	{
		var content = await this.Request.ReadAsStringAsync(cancellation);
		if(string.IsNullOrWhiteSpace(content))
			return this.BadRequest();

		return await this.Service.RenameAsync(new Identifier(Users.Identifiers.User, string.IsNullOrEmpty(id) ? null : id), content, cancellation) ? this.NoContent() : this.NotFound();
	}

	[ActionName("Email")]
	[HttpPatch("{id}/[action]")]
	public async Task<IActionResult> SetEmail(string id, CancellationToken cancellation = default)
	{
		var email = await this.Request.ReadAsStringAsync(cancellation);
		if(string.IsNullOrWhiteSpace(email))
			return this.BadRequest();

		return await this.Service.SetEmailAsync(new Identifier(Users.Identifiers.User, string.IsNullOrEmpty(id) ? null : id), email, cancellation) ? this.NoContent() : this.NotFound();
	}

	[ActionName("Email")]
	[HttpPost("{id}/[action]/Verify")]
	public async Task<IActionResult> SetEmailVerify(string id, CancellationToken cancellation = default)
	{
		var email = await this.Request.ReadAsStringAsync(cancellation);
		if(string.IsNullOrWhiteSpace(email))
			return this.BadRequest();

		var token = await this.Service.SetEmailAsync(
			new Identifier(Users.Identifiers.User, string.IsNullOrEmpty(id) ? null : id),
			email,
			new Parameters(this.Request.GetParameters()),
			cancellation);

		return string.IsNullOrEmpty(token) ? this.NotFound() : this.Content(token);
	}

	[ActionName("Email")]
	[HttpPost("[action]/{token:required}")]
	public async Task<IActionResult> SetEmail(string token, [FromQuery]string secret, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(token) || string.IsNullOrEmpty(secret))
			return this.BadRequest();

		return await this.Service.SetEmailAsync(token, secret, cancellation) ? this.NoContent() : this.NotFound();
	}

	[ActionName("Phone")]
	[HttpPatch("{id}/[action]")]
	public async Task<IActionResult> SetPhone(string id, CancellationToken cancellation = default)
	{
		var phone = await this.Request.ReadAsStringAsync(cancellation);
		if(string.IsNullOrWhiteSpace(phone))
			return this.BadRequest();

		return await this.Service.SetPhoneAsync(new Identifier(Users.Identifiers.User, string.IsNullOrEmpty(id) ? null : id), phone, cancellation) ? this.NoContent() : this.NotFound();
	}

	[ActionName("Phone")]
	[HttpPost("{id}/[action]/Verify")]
	public async Task<IActionResult> SetPhoneVerify(string id, CancellationToken cancellation = default)
	{
		var phone = await this.Request.ReadAsStringAsync(cancellation);
		if(string.IsNullOrWhiteSpace(phone))
			return this.BadRequest();

		var token = await this.Service.SetPhoneAsync(
			new Identifier(Users.Identifiers.User, string.IsNullOrEmpty(id) ? null : id),
			phone,
			new Parameters(this.Request.GetParameters()),
			cancellation);

		return string.IsNullOrEmpty(token) ? this.NotFound() : this.Content(token);
	}

	[ActionName("Phone")]
	[HttpPost("[action]/{token:required}")]
	public async Task<IActionResult> SetPhone(string token, [FromQuery]string secret, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(token) || string.IsNullOrEmpty(secret))
			return this.BadRequest();

		return await this.Service.SetPhoneAsync(token, secret, cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpPost("{id:required}/[action]")]
	public async Task<IActionResult> EnableAsync(string id, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.BadRequest();

		return await this.Service.EnableAsync(new Identifier(Users.Identifiers.User, id), cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpPost("{id:required}/[action]")]
	public async Task<IActionResult> DisableAsync(string id, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.BadRequest();

		return await this.Service.DisableAsync(new Identifier(Users.Identifiers.User, id), cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpHead("{id:required}")]
	[HttpGet("{id:required}/[action]")]
	[HttpGet("[action]/{id:required}")]
	public async Task<IActionResult> Exists(string id, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(id))
			return this.BadRequest();

		return await this.Service.ExistsAsync(new Identifier(Users.Identifiers.User, id), cancellation) ? this.NoContent() : this.NotFound();
	}
	#endregion

	#region 保护方法
	protected async ValueTask<object> GetModelAsync(CancellationToken cancellation)
	{
		var modelType = Utility.GetModelType(this.Service, typeof(IUser), typeof(IUserService<>));
		if(modelType == null)
			return this.StatusCode(StatusCodes.Status501NotImplemented);

		return await Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, modelType, cancellation) is IUser model ? model : this.BadRequest();
	}
	#endregion
}
