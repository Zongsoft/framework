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
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Web;
using Zongsoft.Upgrading.Models;
using Zongsoft.Upgrading.Services;

namespace Zongsoft.Upgrading.Web.Controllers;

[Authorize]
[Area("Upgrading")]
[ControllerName("Releases")]
public class ReleaseController : ServiceController<Models.Release, ReleaseService>
{
	#region 公共方法
	[HttpPost("{id}/[action]")]
	public async ValueTask<IActionResult> PublishAsync(uint id, CancellationToken cancellation = default)
	{
		if(id == 0)
			return this.BadRequest();

		return await this.DataService.PublishAsync(id, cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpPost("{id}/[action]")]
	public async ValueTask<IActionResult> DeprecateAsync(uint id, CancellationToken cancellation = default)
	{
		if(id == 0)
			return this.BadRequest();

		return await this.DataService.DeprecateAsync(id, cancellation) ? this.NoContent() : this.NotFound();
	}

	[HttpPost("{id}/Upload")]
	[HttpPost("Upload/{id}")]
	[DisableRequestSizeLimit]
	public async Task<IActionResult> UploadAsync(uint id, CancellationToken cancellation = default)
	{
		var path = await this.DataService.GetFilePathAsync(id, cancellation);
		if(string.IsNullOrEmpty(path))
			return this.NotFound();

		var info = await this.UploadAsync(path, (info, cancellation) => this.DataService.SetFilePathAsync(id, info?.Path.Url, info.Size, cancellation), cancellation);
		return info == null || string.IsNullOrEmpty(info.Url) ? this.NotFound() : this.Ok(info.Url);
	}
	#endregion

	#region 重写方法
	public override async ValueTask<IActionResult> ImportAsync(IFormFile file, [FromQuery] string format = null, CancellationToken cancellation = default)
	{
		if(file == null || file.Length == 0)
			return this.BadRequest();

		if(string.Equals(format, "manifest", StringComparison.OrdinalIgnoreCase))
		{
			var models = new System.Collections.Generic.List<Models.Release>();

			await foreach(var release in Release.LoadAsync(file.OpenReadStream(), cancellation))
			{
				var model = await this.DataService.ImportAsync(release, cancellation);

				if(model != null)
					models.Add(model);
			}

			return models != null && models.Count > 0 ? this.Ok(models) : this.NoContent();
		}

		return await base.ImportAsync(file, format, cancellation);
	}
	#endregion

	#region 嵌套子类
	[Authorize]
	[ControllerName("Properties")]
	public class PropertyController : SubserviceController<ReleaseProperty, ReleaseService.PropertyService>
	{
	}

	[Authorize]
	[ControllerName("Executors")]
	public class ExecutorController : SubserviceController<ReleaseExecutor, ReleaseService.ExecutorService>
	{
	}

	[Authorize]
	[ControllerName("Tracings")]
	public class TracingController : SubserviceController<ReleaseTracing, ReleaseService.TracingService>
	{
	}
	#endregion
}
