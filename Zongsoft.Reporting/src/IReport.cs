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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Reporting;

public interface IReport
{
	#region 公共属性
	string Name { get; }
	string Type { get; }
	string Icon { get; set; }
	string Title { get; set; }
	string Description { get; set; }
	IReportParameterCollection Parameters { get; }
	IReportDataLocator Locator { get; set; }
	#endregion

	#region 公共方法
	T AsReport<T>() where T : class;

	ValueTask SaveAsync(Stream stream, CancellationToken cancellation = default);
	void Render(Stream stream, IReportRenderOptions options);
	void RenderToFile(string filePath, IReportRenderOptions options);

	void Export(Stream stream, IReportExportOptions options);
	void ExportToFile(string filePath, IReportExportOptions options);
	#endregion
}
