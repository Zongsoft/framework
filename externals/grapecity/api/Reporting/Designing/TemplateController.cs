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

using Zongsoft.Services;
using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Web.Reporting.Designing;

[Route("Grapecity/Reporting/Templates")]
public class TemplateController : ControllerBase
{
	#region 常量定义
	private const string THUMBNAIL_NAME = "template_thumbnail";
	#endregion

	#region 成员字段
	private readonly IServiceProvider _serviceProvider;
	#endregion

	#region 构造函数
	public TemplateController(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}
	#endregion

	#region 公共方法
	[HttpGet("List")]
	public async Task<IActionResult> GetTemplatesAsync(CancellationToken cancellation = default)
	{
		var providers = _serviceProvider.ResolveAll<IReportArchiveLocator>();
		var archives = new List<ReportArchive>();

		foreach(var provider in providers)
		{
			await foreach(var archive in provider.FindAsync("Template", cancellation))
				archives.Add(archive);
		}

		//注意：不能返回204(NoContent)或无内容的200(OK)，因为那会导致报表设计器JS端的脚本错误
		return archives.Count > 0 ?
			this.Ok(archives.Select(archive => new { Id = archive.Key, archive.Name })) :
			this.Ok(Array.Empty<string>());
	}

	[HttpGet("{key}/Content")]
	public async Task<IActionResult> GetTemplateContentAsync(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			return this.BadRequest();

		var report = await this.GetReportAsync(key, cancellation);

		if(report != null)
		{
			var thumbnail = report.EmbeddedImages.FirstOrDefault(image => image.Name == THUMBNAIL_NAME);

			if(thumbnail != null)
				report.EmbeddedImages.Remove(thumbnail);

			return this.File(GrapeCity.ActiveReports.Aspnetcore.Designer.Utilities.ReportConverter.ToJson(report), "application/json");
		}

		return this.NotFound();
	}

	[HttpGet("{key}/Thumbnail")]
	public async Task<IActionResult> GetTemplateThumbnailAsync(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			return this.BadRequest();

		var report = await this.GetReportAsync(key, cancellation);

		if(report != null)
		{
			var thumbnail = report.EmbeddedImages.FirstOrDefault(image => image.Name == THUMBNAIL_NAME);

			if(thumbnail != null)
				return this.Ok(new { Data = thumbnail.ImageData, thumbnail.MIMEType });
		}

		return this.NotFound();
	}
	#endregion

	#region 私有方法
	private async Task<GrapeCity.ActiveReports.PageReportModel.Report> GetReportAsync(string key, CancellationToken cancellation)
	{
		var providers = _serviceProvider.ResolveAll<IReportArchiveLocator>();

		foreach(var provider in providers)
		{
			var archive = await provider.OpenAsync(key, cancellation);
			if(archive.Stream != null)
				return (await Grapecity.Reporting.Report.OpenAsync(archive.Stream, cancellation))?.AsReport();
		}

		return null;
	}
	#endregion
}
