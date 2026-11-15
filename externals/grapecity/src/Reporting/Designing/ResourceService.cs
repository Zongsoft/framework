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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using GrapeCity.ActiveReports;
using GrapeCity.ActiveReports.Document;
using GrapeCity.ActiveReports.Rdl.Tools;
using GrapeCity.ActiveReports.PageReportModel;
using GrapeCity.ActiveReports.Aspnetcore.Designer;
using GrapeCity.ActiveReports.Aspnetcore.Designer.Services;
using GrapeCity.ActiveReports.Aspnetcore.Designer.Utilities;

using Zongsoft.Services;
using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Reporting.Designing;

[Service(typeof(IResourcesService))]
public class ResourceService : IResourcesService
{
	#region 成员字段
	private readonly IServiceProvider _serviceProvider;
	#endregion

	#region 构造函数
	public ResourceService(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}
	#endregion

	#region 公共方法
	public Uri GetBaseUri() => null;

	public byte[] GetImage(string id, out string mimeType)
	{
		if(string.IsNullOrEmpty(id))
		{
			mimeType = null;
			return null;
		}

		var result = Utility.RunForSynchronousCallback(GetImageAsync);
		mimeType = result.MimeType;
		return result.Data;

		async Task<(byte[] Data, string MimeType)> GetImageAsync()
		{
			var locator = _serviceProvider.ResolveRequired<IReportArchiveLocator>();
			var archive = await locator.OpenAsync(id);
			await using var stream = archive.Stream;
			if(stream == null)
				return (null, archive.Archive.Type);

			using var buffer = new MemoryStream();
			await stream.CopyToAsync(buffer);
			return (buffer.ToArray(), archive.Archive.Type);
		}
	}

	public IImageInfo[] GetImagesList() => Utility.RunForSynchronousCallback(async () =>
	{
		var locator = _serviceProvider.ResolveRequired<IReportArchiveLocator>();
		var images = new List<ImageInfo>();

		await foreach(var file in locator.FindAsync("image/*"))
			images.Add(new ImageInfo(file.Name, file.Name, file.Type));

		return images.ToArray();
	});

	public Theme GetTheme(string id)
	{
		if(string.IsNullOrEmpty(id))
			return null;

		return Utility.RunForSynchronousCallback(async () =>
		{
			var locator = _serviceProvider.ResolveRequired<IReportResourceLocator>();
			return ThemeMapper.Map(await locator.GetResourceAsync(id));
		});
	}

	public IThemeInfo[] GetThemesList() => Utility.RunForSynchronousCallback(async () =>
	{
		var locator = _serviceProvider.ResolveRequired<IReportResourceLocator>();
		var themes = new List<ThemeInfo>();

		await foreach(var resource in locator.GetResourcesAsync("theme"))
			themes.Add(ThemeInfo.Populate(resource.Name, resource.Title, resource.Extra));

		return themes.ToArray();
	});

	public GrapeCity.ActiveReports.PageReportModel.Report GetReport(string id) => Utility.RunForSynchronousCallback(async () =>
	{
		var providers = _serviceProvider.ResolveAll<IReportLocator>().OrderByDescending(p => p.Priority);

		foreach(var provider in providers)
		{
			var descriptor = await provider.GetReportAsync(id);
			if(descriptor == null)
				continue;

			var report = await Report.OpenAsync(descriptor);
			if(report != null)
				return report.AsReport<GrapeCity.ActiveReports.PageReportModel.Report>();
		}

		return null;
	});

	public IReportInfo[] GetReportsList() => Utility.RunForSynchronousCallback(async () =>
	{
		var providers = _serviceProvider.ResolveAll<IReportLocator>().OrderByDescending(p => p.Priority);
		var reports = new List<IReportInfo>();

		foreach(var provider in providers)
		{
			await foreach(var descriptor in provider.GetReportsAsync())
				reports.Add(new ReportInfo(descriptor.Key, descriptor.Name, descriptor.Type));
		}

		return reports.ToArray();
	});

	public string SaveReport(string name, GrapeCity.ActiveReports.PageReportModel.Report report, bool isTemporary = false)
	{
		var service = _serviceProvider.ResolveRequired<IReportRepository>();
		var data = ReportConverter.ToXml(report);
		var type = report.IsFixedPageReport ? "FPL" : "CPL";
		var options = new ReportRepositoryOptions() { IsTemporary = isTemporary };
		return Utility.RunForSynchronousCallback(() => service.CreateAsync(name, type, data, options).AsTask());
	}

	public string UpdateReport(string id, GrapeCity.ActiveReports.PageReportModel.Report report)
	{
		if(string.IsNullOrEmpty(id))
			return null;

		if(id.Length > 5 && id.EndsWith(".rdlx", StringComparison.OrdinalIgnoreCase))
			id = id.Substring(0, id.Length - 5);

		var service = _serviceProvider.ResolveRequired<IReportRepository>();
		var data = ReportConverter.ToXml(report);
		var type = report.IsFixedPageReport ? "FPL" : "CPL";
		return Utility.RunForSynchronousCallback(() => service.UpdateAsync(id, type, data).AsTask());
	}

	public void DeleteReport(string id) => Utility.RunForSynchronousCallback(async () =>
	{
		var repositories = _serviceProvider.ResolveAll<IReportRepository>();

		foreach(var repository in repositories)
		{
			if(await repository.DeleteAsync(id))
				return;
		}
	});
	#endregion
}
