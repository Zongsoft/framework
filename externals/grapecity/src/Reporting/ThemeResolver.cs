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
using System.Xml;
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

namespace Zongsoft.Externals.Grapecity.Reporting
{
	public static class ThemeResolver
	{
		#region 常量定义
		private const string XML_ROOT = "Theme";
		private const string XML_COLORS_SECTION = "Colors";
		private const string XML_FONTS_SECTION = "Fonts";
		private const string XML_IMAGES_SECTION = "Images";
		private const string XML_CONSTANTS_SECTION = "Constants";

		private const string XML_COLOR_DARK1_ELEMENT = "Dark1";
		private const string XML_COLOR_DARK2_ELEMENT = "Dark2";
		private const string XML_COLOR_LIGHT1_ELEMENT = "Light1";
		private const string XML_COLOR_LIGHT2_ELEMENT = "Light2";
		private const string XML_COLOR_ACCENT1_ELEMENT = "Accent1";
		private const string XML_COLOR_ACCENT2_ELEMENT = "Accent2";
		private const string XML_COLOR_ACCENT3_ELEMENT = "Accent3";
		private const string XML_COLOR_ACCENT4_ELEMENT = "Accent4";
		private const string XML_COLOR_ACCENT5_ELEMENT = "Accent5";
		private const string XML_COLOR_ACCENT6_ELEMENT = "Accent6";
		private const string XML_COLOR_HYPERLINK_ELEMENT = "Hyperlink";
		private const string XML_COLOR_HYPERLINKFOLLOWED_ELEMENT = "HyperlinkFollowed";

		private const string XML_MAJORFONT_ELEMENT = "MajorFont";
		private const string XML_MINORFONT_ELEMENT = "MinorFont";
		private const string XML_FONT_FAMILY_ELEMENT = "Family";
		private const string XML_FONT_STYLE_ELEMENT = "Style";
		private const string XML_FONT_SIZE_ELEMENT = "Size";
		private const string XML_FONT_WEIGHT_ELEMENT = "Weight";

		private const string XML_IMAGE_ELEMENT = "Image";
		private const string XML_IMAGE_NAME_ELEMENT = "Name";
		private const string XML_IMAGE_TYPE_ELEMENT = "MIMEType";
		private const string XML_IMAGE_DATA_ELEMENT = "ImageData";

		private const string XML_CONSTANT_ELEMENT = "Constant";
		private const string XML_CONSTANT_KEY_ELEMENT = "Key";
		private const string XML_CONSTANT_VALUE_ELEMENT = "Value";
		private const string XML_CONSTANT_DESCRIPTION_ELEMENT = "Description";
		#endregion

		#region 公共方法
		public static Theme Resolve(Stream stream)
		{
			if(stream == null)
				throw new ArgumentNullException(nameof(stream));

			if(stream.CanSeek)
				stream.Seek(0, SeekOrigin.Begin);

			using var reader = GetReader(stream);

			SkipUntilRootElement(reader);

			//确认根节点的名称是否合法
			if(reader.LocalName != XML_ROOT)
				return null;

			var theme = new Theme();

			while(reader.Read())
			{
				if(reader.NodeType != XmlNodeType.Element)
					continue;

				switch(reader.LocalName)
				{
					case XML_COLORS_SECTION:
						theme.Colors = ResolveColors(reader);
						break;
					case XML_FONTS_SECTION:
						theme.Fonts = ResolveFonts(reader);
						break;
					case XML_IMAGES_SECTION:
						theme.Images = ResolveImages(reader);
						break;
					case XML_CONSTANTS_SECTION:
						theme.Constants = ResolveConstants(reader);
						break;
				}
			}

			return theme;
		}
		#endregion

		#region 私有方法
		private static XmlReader GetReader(Stream stream)
		{
			return XmlReader.Create(stream, new XmlReaderSettings()
			{
				CloseInput = true,
				IgnoreComments = true,
				IgnoreWhitespace = true,
				IgnoreProcessingInstructions = true,
			});
		}

		private static void SkipUntilRootElement(XmlReader reader)
		{
			while(reader.Read())
			{
				if(reader.NodeType != XmlNodeType.XmlDeclaration && reader.NodeType != XmlNodeType.ProcessingInstruction)
					break;
			}
		}

		private static ThemeColors ResolveColors(XmlReader reader)
		{
			var result = new ThemeColors();
			var depth = reader.Depth;

			while(reader.Read())
			{
				if(reader.Depth <= depth)
					return result;

				switch(reader.LocalName)
				{
					case XML_COLOR_DARK1_ELEMENT:
						result.Dark1 = reader.HasValue ? reader.Value : null;
						break;
					case XML_COLOR_DARK2_ELEMENT:
						result.Dark2 = reader.HasValue ? reader.Value : null;
						break;
					case XML_COLOR_LIGHT1_ELEMENT:
						result.Light1 = reader.HasValue ? reader.Value : null;
						break;
					case XML_COLOR_LIGHT2_ELEMENT:
						result.Light2 = reader.HasValue ? reader.Value : null;
						break;
					case XML_COLOR_ACCENT1_ELEMENT:
						result.Accent1 = reader.HasValue ? reader.Value : null;
						break;
					case XML_COLOR_ACCENT2_ELEMENT:
						result.Accent2 = reader.HasValue ? reader.Value : null;
						break;
					case XML_COLOR_ACCENT3_ELEMENT:
						result.Accent3 = reader.HasValue ? reader.Value : null;
						break;
					case XML_COLOR_ACCENT4_ELEMENT:
						result.Accent4 = reader.HasValue ? reader.Value : null;
						break;
					case XML_COLOR_ACCENT5_ELEMENT:
						result.Accent5 = reader.HasValue ? reader.Value : null;
						break;
					case XML_COLOR_ACCENT6_ELEMENT:
						result.Accent6 = reader.HasValue ? reader.Value : null;
						break;
					case XML_COLOR_HYPERLINK_ELEMENT:
						result.Hyperlink = reader.HasValue ? reader.Value : null;
						break;
					case XML_COLOR_HYPERLINKFOLLOWED_ELEMENT:
						result.HyperlinkFollowed = reader.HasValue ? reader.Value : null;
						break;
				}
			}

			return result;
		}

		private static ThemeFonts ResolveFonts(XmlReader reader)
		{
			var result = new ThemeFonts();
			var depth = reader.Depth;

			while(reader.Read())
			{
				if(reader.Depth <= depth)
					return result;

				switch(reader.LocalName)
				{
					case XML_MAJORFONT_ELEMENT:
						result.MajorFont = ResolveFont(reader);
						break;
					case XML_MINORFONT_ELEMENT:
						result.MinorFont = ResolveFont(reader);
						break;
				}
			}

			return result;
		}

		private static ThemeFont ResolveFont(XmlReader reader)
		{
			var result = new ThemeFont();
			var depth = reader.Depth;

			while(reader.Read())
			{
				if(reader.Depth <= depth)
					return result;

				switch(reader.LocalName)
				{
					case XML_FONT_FAMILY_ELEMENT:
						result.Family = reader.HasValue ? reader.Value : null;
						break;
					case XML_FONT_STYLE_ELEMENT:
						result.Style = reader.HasValue ? reader.Value : null;
						break;
					case XML_FONT_SIZE_ELEMENT:
						result.Size = reader.HasValue ? reader.Value : null;
						break;
					case XML_FONT_WEIGHT_ELEMENT:
						result.Weight = reader.HasValue ? reader.Value : null;
						break;
				}
			}

			return result;
		}

		private static ThemeImage[] ResolveImages(XmlReader reader)
		{
			var list = new List<ThemeImage>();
			var depth = reader.Depth;

			while(reader.Read())
			{
				if(reader.Depth <= depth)
					return list.ToArray();

				switch(reader.LocalName)
				{
					case XML_IMAGE_ELEMENT:
						var image = ResolveImage(reader);

						if(image != null && !string.IsNullOrEmpty(image.Name) && !string.IsNullOrEmpty(image.ImageData))
							list.Add(image);

						break;
				}
			}

			return list.ToArray();
		}

		private static ThemeImage ResolveImage(XmlReader reader)
		{
			var result = new ThemeImage();
			var depth = reader.Depth;

			while(reader.Read())
			{
				if(reader.Depth <= depth)
					return result;

				switch(reader.LocalName)
				{
					case XML_IMAGE_NAME_ELEMENT:
						result.Name = reader.HasValue ? reader.Value : null;
						break;
					case XML_IMAGE_TYPE_ELEMENT:
						result.MIMEType = reader.HasValue ? reader.Value : null;
						break;
					case XML_IMAGE_DATA_ELEMENT:
						result.ImageData = reader.HasValue ? reader.Value : null;
						break;
				}
			}

			return result;
		}

		private static ThemeConstant[] ResolveConstants(XmlReader reader)
		{
			var list = new List<ThemeConstant>();
			var depth = reader.Depth;

			while(reader.Read())
			{
				if(reader.Depth <= depth)
					return list.ToArray();

				switch(reader.LocalName)
				{
					case XML_CONSTANT_ELEMENT:
						var constant = ResolveConstant(reader);

						if(constant != null && !string.IsNullOrEmpty(constant.Key))
							list.Add(constant);

						break;
				}
			}

			return list.ToArray();
		}

		private static ThemeConstant ResolveConstant(XmlReader reader)
		{
			var result = new ThemeConstant();
			var depth = reader.Depth;

			while(reader.Read())
			{
				if(reader.Depth <= depth)
					return result;

				switch(reader.LocalName)
				{
					case XML_CONSTANT_KEY_ELEMENT:
						result.Key = reader.HasValue ? reader.Value : null;
						break;
					case XML_CONSTANT_VALUE_ELEMENT:
						result.Value = reader.HasValue ? reader.Value : null;
						break;
					case XML_CONSTANT_DESCRIPTION_ELEMENT:
						break;
				}
			}

			return result;
		}
		#endregion
	}
}
