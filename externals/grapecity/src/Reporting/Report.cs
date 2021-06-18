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
 * This file is part of Zongsoft.Externals.GrapeCity library.
 *
 * The Zongsoft.Externals.GrapeCity is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.GrapeCity is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.GrapeCity library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;

using GrapeCity.ActiveReports;
using GrapeCity.ActiveReports.Document;

using Zongsoft.IO;
using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Reporting
{
	public class Report : IReport, IDisposable
	{
		#region 成员字段
		private bool _disposed;
		private PageReport _report;
		#endregion

		#region 私有构造
		private Report(PageReport report)
		{
			_report = report ?? throw new ArgumentNullException(nameof(report));
			report.Document.LocateDataSource += Document_LocateDataSource;
			report.Document.LocateCredentials += Document_LocateCredentials;
			this.Parameters = new ReportParameterCollection(report.Report.ReportParameters);
		}
		#endregion

		#region 公共属性
		public string Name => _report.ReportName;
		public string Icon { get; set; }
		public string Title { get => this.Parameters.TryGetValue("Title", out var parameter) && parameter.Value is string title ? title : null; set => throw new NotSupportedException(); }
		public string Description { get => _report.Report.Description; set => _report.Report.Description = value; }
		public IReportParameterCollection Parameters { get; }
		public IReportDataLocator Locator { get => null; set => throw new NotImplementedException(); }
		#endregion

		#region 公共方法
		public static Report Open(string filePath)
		{
			if(string.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));

			using var stream = FileSystem.File.Open(filePath, FileMode.Open, FileAccess.Read);
			using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
			return new Report(new PageReport(reader));
		}

		public static Report Open(Stream stream)
		{
			if(stream == null)
				throw new ArgumentNullException(nameof(stream));

			using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
			return new Report(new PageReport(reader));
		}

		public static Report Open(IReportDescriptor descriptor)
		{
			if(descriptor == null)
				throw new ArgumentNullException(nameof(descriptor));

			using var stream = descriptor.Open();
			return stream == null ? null : Open(stream);
		}

		public T AsReport<T>() where T : class
		{
			if(typeof(T) == typeof(PageReport))
				return _report as T;
			else if(typeof(T) == typeof(GrapeCity.ActiveReports.PageReportModel.Report))
				return _report.Report as T;

			return null;
		}

		public void Export(Stream stream, IReportExportOptions options)
		{
		}

		public void ExportToFile(string filePath, IReportExportOptions options)
		{
			this.Export(FileSystem.File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write), options);
		}

		public void Render(Stream stream, IReportRenderOptions options)
		{
			throw new NotImplementedException();
		}

		public void RenderToFile(string filePath, IReportRenderOptions options)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region 事件处理
		private void Document_LocateCredentials(object sender, LocateCredentialsEventArgs args)
		{
		}

		private void Document_LocateDataSource(object sender, LocateDataSourceEventArgs args)
		{
			foreach(var dataSource in args.Report.DataSources)
			{
				if(dataSource.Name.Equals(args.DataSet.Query.DataSourceName, StringComparison.OrdinalIgnoreCase))
				{
				}
			}
		}
		#endregion

		#region 释放资源
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(_disposed)
				return;

			if(disposing)
			{
				var report = _report;

				if(report != null)
				{
					if(report.Document != null)
						report.Document.LocateDataSource -= Document_LocateDataSource;

					report.Dispose();
				}
			}

			_disposed = true;
		}
		#endregion
	}
}
