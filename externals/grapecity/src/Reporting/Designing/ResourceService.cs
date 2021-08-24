/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Grapecity library.
 *
 * The Zongsoft.Externals.Grapecity is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Grapecity is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Grapecity library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using GrapeCity.ActiveReports;
using GrapeCity.ActiveReports.Document;
using GrapeCity.ActiveReports.Rdl.Tools;
using GrapeCity.ActiveReports.PageReportModel;
using GrapeCity.ActiveReports.Aspnetcore.Designer;
using GrapeCity.ActiveReports.Aspnetcore.Designer.Services;
using GrapeCity.ActiveReports.Aspnetcore.Designer.Utilities;

using Zongsoft.IO;
using Zongsoft.Services;
using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Reporting.Designing
{
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

			var locator = _serviceProvider.ResolveRequired<IReportArchiveLocator>();
			var stream = locator.Open(id, out var info);
			mimeType = info.Type;
			return stream == null ? null : Utility.ReadAll(stream);
		}

		public IImageInfo[] GetImagesList()
		{
			var locator = _serviceProvider.ResolveRequired<IReportArchiveLocator>();
			var files = locator.Find("image/*");
			var images = new List<ImageInfo>();

			foreach(var file in files)
			{
				images.Add(new ImageInfo(file.Name, file.Name, file.Type));
			}

			return images.ToArray();
		}

		public Theme GetTheme(string id)
		{
			if(string.IsNullOrEmpty(id))
				return null;

			var locator = _serviceProvider.ResolveRequired<IReportResourceLocator>();
			var resource = locator.GetResource(id);
			return ThemeMapper.Map(resource);
		}

		public IThemeInfo[] GetThemesList()
		{
			var locator = _serviceProvider.ResolveRequired<IReportResourceLocator>();
			var resources = locator.GetResources("theme");
			var themes = new List<ThemeInfo>();

			foreach(var resource in resources)
			{
				themes.Add(ThemeInfo.Populate(resource.Name, resource.Title, resource.Extra));
			}

			return themes.ToArray();
		}

		public GrapeCity.ActiveReports.PageReportModel.Report GetReport(string id)
		{
			var providers = _serviceProvider.ResolveAll<IReportLocator>().OrderByDescending(p => p.Priority);

			foreach(var provider in providers)
			{
				var descriptor = provider.GetReport(id);

				if(descriptor != null)
				{
					var report = Report.Open(descriptor);

					if(report != null)
						return report.AsReport<GrapeCity.ActiveReports.PageReportModel.Report>();
				}
			}

			return null;
		}

		public IReportInfo[] GetReportsList()
		{
			var providers = _serviceProvider.ResolveAll<IReportLocator>().OrderByDescending(p => p.Priority);
			var reports = new List<IReportInfo>();

			foreach(var provider in providers)
			{
				reports.AddRange(provider.GetReports().Select(descriptor => new ReportInfo(descriptor.Key, descriptor.Name, descriptor.Type)));
			}

			return reports.ToArray();
		}

		public string SaveReport(string name, GrapeCity.ActiveReports.PageReportModel.Report report, bool isTemporary = false)
		{
			var service = _serviceProvider.ResolveRequired<IReportRepository>();
			var data = ReportConverter.ToXml(report);
			return service.Create(name, report.IsFixedPageReport ? "FPL" : "CPL", data, new ReportRepositoryOptions() { IsTemporary = isTemporary });
		}

		public string UpdateReport(string id, GrapeCity.ActiveReports.PageReportModel.Report report)
		{
			if(string.IsNullOrEmpty(id))
				return null;

			if(id.Length > 5 && id.EndsWith(".rdlx", StringComparison.OrdinalIgnoreCase))
				id = id.Substring(0, id.Length - 5);

			var service = _serviceProvider.ResolveRequired<IReportRepository>();
			var data = ReportConverter.ToXml(report);
			return service.Update(id, report.IsFixedPageReport ? "FPL" : "CPL", data);
		}

		public void DeleteReport(string id)
		{
			var repositories = _serviceProvider.ResolveAll<IReportRepository>();

			foreach(var repository in repositories)
			{
				if(repository.Delete(id))
					return;
			}
		}
		#endregion
	}
}
