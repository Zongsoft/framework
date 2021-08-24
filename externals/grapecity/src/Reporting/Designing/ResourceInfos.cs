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
using GrapeCity.ActiveReports.PageReportModel;
using GrapeCity.ActiveReports.Aspnetcore.Designer;
using GrapeCity.ActiveReports.Aspnetcore.Designer.Services;
using GrapeCity.ActiveReports.Aspnetcore.Designer.Utilities;

using Zongsoft.IO;
using Zongsoft.Services;
using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Reporting.Designing
{
	public class ReportInfo : IReportInfo
	{
		public ReportInfo(string id, string name, string type)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Id = id;
			this.Name = name;
			this.Type = type;
		}

		public string Id { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
	}

	public class ImageInfo : IImageInfo
	{
		public ImageInfo() { }
		public ImageInfo(string id, string name, string type = null)
		{
			this.Id = id;
			this.Name = name;
			this.ContentType = type;
		}

		public string Id { get; set; }
		public string Name { get; set; }
		public byte[] Thumbnail { get; set; }
		public string ContentType { get; set; }
	}

	public class ThemeInfo : IThemeInfo
	{
		public ThemeInfo() { }
		public ThemeInfo(string id, string title = null)
		{
			this.Id = id;
			this.Title = title;
		}

		public string Id { get; set; }
		public string Title { get; set; }
		public string Dark1 { get; set; }
		public string Dark2 { get; set; }
		public string Light1 { get; set; }
		public string Light2 { get; set; }
		public string Accent1 { get; set; }
		public string Accent2 { get; set; }
		public string Accent3 { get; set; }
		public string Accent4 { get; set; }
		public string Accent5 { get; set; }
		public string Accent6 { get; set; }
		public string MajorFontFamily { get; set; }
		public string MinorFontFamily { get; set; }

		public static ThemeInfo Populate(string id, string title, string content)
		{
			if(string.IsNullOrEmpty(id))
				throw new ArgumentNullException(nameof(id));

			var theme = new ThemeInfo(id, title ?? id);

			if(string.IsNullOrEmpty(content))
				return theme;

			var pairs = Zongsoft.Common.StringExtension.Slice(content, new[] { ',', '|', ';' });

			foreach(var pair in pairs)
			{
				if(string.IsNullOrEmpty(pair))
					continue;

				var index = pair.IndexOfAny(new[] { ':', '=' });

				if(index > 0 && index < pair.Length - 1)
				{
					var key = pair.AsSpan(0, index);
					var value = pair.AsSpan(index + 1);

					if(key.Equals(nameof(Dark1), StringComparison.OrdinalIgnoreCase))
						theme.Dark1 = value.ToString();
					else if(key.Equals(nameof(Dark2), StringComparison.OrdinalIgnoreCase))
						theme.Dark2 = value.ToString();
					else if(key.Equals(nameof(Light1), StringComparison.OrdinalIgnoreCase))
						theme.Light1 = value.ToString();
					else if(key.Equals(nameof(Light2), StringComparison.OrdinalIgnoreCase))
						theme.Light2 = value.ToString();
					else if(key.Equals(nameof(Accent1), StringComparison.OrdinalIgnoreCase))
						theme.Accent1 = value.ToString();
					else if(key.Equals(nameof(Accent2), StringComparison.OrdinalIgnoreCase))
						theme.Accent2 = value.ToString();
					else if(key.Equals(nameof(Accent3), StringComparison.OrdinalIgnoreCase))
						theme.Accent3 = value.ToString();
					else if(key.Equals(nameof(Accent4), StringComparison.OrdinalIgnoreCase))
						theme.Accent4 = value.ToString();
					else if(key.Equals(nameof(Accent5), StringComparison.OrdinalIgnoreCase))
						theme.Accent5 = value.ToString();
					else if(key.Equals(nameof(Accent6), StringComparison.OrdinalIgnoreCase))
						theme.Accent6 = value.ToString();
					else if(key.Equals(nameof(MajorFontFamily), StringComparison.OrdinalIgnoreCase))
						theme.MajorFontFamily = value.ToString();
					else if(key.Equals(nameof(MinorFontFamily), StringComparison.OrdinalIgnoreCase))
						theme.MinorFontFamily = value.ToString();
				}
			}

			return theme;
		}
	}
}
