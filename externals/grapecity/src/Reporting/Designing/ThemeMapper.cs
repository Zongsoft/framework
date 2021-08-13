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
using Zongsoft.Reporting.Resources;

namespace Zongsoft.Externals.Grapecity.Reporting.Designing
{
	public class ThemeMapper
	{
		#region 常量定义
		internal const string COLORS_DARK1_KEY = "Colors:Dark1";
		internal const string COLORS_DARK2_KEY = "Colors:Dark2";
		internal const string COLORS_LIGHT1_KEY = "Colors:Light1";
		internal const string COLORS_LIGHT2_KEY = "Colors:Light2";
		internal const string COLORS_ACCENT1_KEY = "Colors:Accent1";
		internal const string COLORS_ACCENT2_KEY = "Colors:Accent2";
		internal const string COLORS_ACCENT3_KEY = "Colors:Accent3";
		internal const string COLORS_ACCENT4_KEY = "Colors:Accent4";
		internal const string COLORS_ACCENT5_KEY = "Colors:Accent5";
		internal const string COLORS_ACCENT6_KEY = "Colors:Accent6";
		internal const string COLORS_HYPERLINK_KEY = "Colors:Hyperlink";
		internal const string COLORS_HYPERLINKFOLLOWED_KEY = "Colors:HyperlinkFollowed";

		internal const string FONTS_MAJOR_KEY = "Fonts:Major";
		internal const string FONTS_MINOR_KEY = "Fonts:Minor";
		#endregion

		#region 公共方法
		public static Theme Map(IResource resource)
		{
			if(resource == null)
				return null;

			return new Theme()
			{
				Colors = GetColors(resource.Dictionary),
				Fonts = GetFonts(resource.Dictionary),
				Images = GetImages(resource.Dictionary),
				Constants = GetConstants(resource.Dictionary),
			};
		}
		#endregion

		#region 私有方法
		private static ThemeColors GetColors(IDictionary<string, ResourceEntry> dictionary)
		{
			if(dictionary == null)
				return null;

			var result = new ThemeColors();

			if(dictionary.TryGetValue(COLORS_DARK1_KEY, out var entry) && entry.Value != null)
				result.Dark1 = entry.Value.ToString();

			if(dictionary.TryGetValue(COLORS_DARK2_KEY, out entry) && entry.Value != null)
				result.Dark2 = entry.Value.ToString();

			if(dictionary.TryGetValue(COLORS_LIGHT1_KEY, out entry) && entry.Value != null)
				result.Light1 = entry.Value.ToString();

			if(dictionary.TryGetValue(COLORS_LIGHT2_KEY, out entry) && entry.Value != null)
				result.Light2 = entry.Value.ToString();

			if(dictionary.TryGetValue(COLORS_ACCENT1_KEY, out entry) && entry.Value != null)
				result.Accent1 = entry.Value.ToString();

			if(dictionary.TryGetValue(COLORS_ACCENT2_KEY, out entry) && entry.Value != null)
				result.Accent2 = entry.Value.ToString();

			if(dictionary.TryGetValue(COLORS_ACCENT3_KEY, out entry) && entry.Value != null)
				result.Accent3 = entry.Value.ToString();

			if(dictionary.TryGetValue(COLORS_ACCENT4_KEY, out entry) && entry.Value != null)
				result.Accent4 = entry.Value.ToString();

			if(dictionary.TryGetValue(COLORS_ACCENT5_KEY, out entry) && entry.Value != null)
				result.Accent5 = entry.Value.ToString();

			if(dictionary.TryGetValue(COLORS_ACCENT6_KEY, out entry) && entry.Value != null)
				result.Accent6 = entry.Value.ToString();

			if(dictionary.TryGetValue(COLORS_HYPERLINK_KEY, out entry) && entry.Value != null)
				result.Hyperlink = entry.Value.ToString();

			if(dictionary.TryGetValue(COLORS_HYPERLINKFOLLOWED_KEY, out entry) && entry.Value != null)
				result.HyperlinkFollowed = entry.Value.ToString();

			return result;
		}

		private static ThemeFonts GetFonts(IDictionary<string, ResourceEntry> dictionary)
		{
			if(dictionary == null)
				return null;

			ThemeFont major = null, minor = null;

			if(dictionary.TryGetValue(FONTS_MAJOR_KEY, out var entry) && entry.Value != null)
				major = GetFont(entry.Value.ToString());

			if(dictionary.TryGetValue(FONTS_MINOR_KEY, out entry) && entry.Value != null)
				minor = GetFont(entry.Value.ToString());

			return new ThemeFonts() { MajorFont = major, MinorFont = minor };

			static ThemeFont GetFont(string text)
			{
				if(string.IsNullOrEmpty(text))
					return null;

				var parts = Zongsoft.Common.StringExtension.Slice(text, ',').ToArray();
				var font = new ThemeFont() { Family = parts[0] };

				switch(parts.Length)
				{
					case 2:
						font.Style = parts[1];
						break;
					case 3:
						font.Style = parts[1];
						font.Size = parts[2];
						break;
					case 4:
						font.Style = parts[1];
						font.Size = parts[2];
						font.Weight = parts[3];
						break;
				}

				return font;
			}
		}

		private static ThemeImage[] GetImages(IDictionary<string, ResourceEntry> dictionary)
		{
			const int NAME_POSITION = 7;

			if(dictionary == null || dictionary.Count == 0)
				return null;

			var images = new List<ThemeImage>(dictionary.Count);

			foreach(var entry in dictionary.Values)
			{
				if(entry.Value is byte[] data && entry.Name.StartsWith("Images:", StringComparison.OrdinalIgnoreCase))
				{
					images.Add(new ThemeImage()
					{
						Name = entry.Name.Substring(NAME_POSITION),
						MIMEType = entry.Type,
						ImageData = System.Convert.ToBase64String(data)
					});
				}
			}

			return images.ToArray();
		}

		private static ThemeConstant[] GetConstants(IDictionary<string, ResourceEntry> dictionary)
		{
			const int NAME_POSITION = 10;

			if(dictionary == null || dictionary.Count == 0)
				return null;

			var constants = new List<ThemeConstant>(dictionary.Count);

			foreach(var entry in dictionary.Values)
			{
				if(entry.Name.StartsWith("Constants:", StringComparison.OrdinalIgnoreCase))
				{
					constants.Add(new ThemeConstant()
					{
						Key = entry.Name.Substring(NAME_POSITION),
						Value = entry.Value?.ToString(),
					});
				}
			}

			return constants.ToArray();
		}
		#endregion
	}
}
