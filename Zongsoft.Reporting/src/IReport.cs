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
 * This file is part of Zongsoft.Reporting library.
 *
 * The Zongsoft.Reporting is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Reporting is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Reporting library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;

namespace Zongsoft.Reporting
{
	public interface IReport
	{
		string Name { get; }
		string Type { get; }
		string Icon { get; set; }
		string Title { get; set; }
		string Description { get; set; }
		IReportParameterCollection Parameters { get; }
		IReportDataLocator Locator { get; set; }

		T AsReport<T>() where T : class;

		void Save(Stream stream);
		void Render(Stream stream, IReportRenderOptions options);
		void RenderToFile(string filePath, IReportRenderOptions options);

		void Export(Stream stream, IReportExportOptions options);
		void ExportToFile(string filePath, IReportExportOptions options);
	}
}
