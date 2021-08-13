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
using System.Text;
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
	[Service(typeof(IResourceResolver))]
	public class ThemeResolver : IResourceResolver
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

		#region 公共属性
		public string Name { get => "Theme"; }
		#endregion

		#region 公共方法
		public Theme Load(Stream stream)
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

		public IResource Resolve(string name, Stream stream, string title = null, string description = null)
		{
			var theme = this.Load(stream);

			if(theme == null)
				return null;

			var resource = new Zongsoft.Reporting.Resources.Resource(string.IsNullOrEmpty(name) ? "Unnamed" : name, this.Name, title, null, description);
			StringBuilder builder = null;

			if(theme.Colors != null)
			{
				if(builder == null)
					builder = new StringBuilder();

				builder.Append($"{nameof(IThemeInfo.Dark1)}:{theme.Colors.Dark1};");
				builder.Append($"{nameof(IThemeInfo.Dark2)}:{theme.Colors.Dark2};");
				builder.Append($"{nameof(IThemeInfo.Light1)}:{theme.Colors.Light1};");
				builder.Append($"{nameof(IThemeInfo.Light2)}:{theme.Colors.Light2};");
				builder.Append($"{nameof(IThemeInfo.Accent1)}:{theme.Colors.Accent1};");
				builder.Append($"{nameof(IThemeInfo.Accent2)}:{theme.Colors.Accent2};");
				builder.Append($"{nameof(IThemeInfo.Accent3)}:{theme.Colors.Accent3};");
				builder.Append($"{nameof(IThemeInfo.Accent4)}:{theme.Colors.Accent4};");
				builder.Append($"{nameof(IThemeInfo.Accent5)}:{theme.Colors.Accent5};");
				builder.Append($"{nameof(IThemeInfo.Accent6)}:{theme.Colors.Accent6};");

				if(resource.Dictionary == null)
					resource.Dictionary = new Dictionary<string, ResourceEntry>(StringComparer.OrdinalIgnoreCase);

				resource.Dictionary.Add(ThemeMapper.COLORS_DARK1_KEY, new ResourceEntry(ThemeMapper.COLORS_DARK1_KEY, null, theme.Colors.Dark1));
				resource.Dictionary.Add(ThemeMapper.COLORS_DARK2_KEY, new ResourceEntry(ThemeMapper.COLORS_DARK2_KEY, null, theme.Colors.Dark2));
				resource.Dictionary.Add(ThemeMapper.COLORS_LIGHT1_KEY, new ResourceEntry(ThemeMapper.COLORS_LIGHT1_KEY, null, theme.Colors.Light1));
				resource.Dictionary.Add(ThemeMapper.COLORS_LIGHT2_KEY, new ResourceEntry(ThemeMapper.COLORS_LIGHT2_KEY, null, theme.Colors.Light2));
				resource.Dictionary.Add(ThemeMapper.COLORS_ACCENT1_KEY, new ResourceEntry(ThemeMapper.COLORS_ACCENT1_KEY, null, theme.Colors.Accent1));
				resource.Dictionary.Add(ThemeMapper.COLORS_ACCENT2_KEY, new ResourceEntry(ThemeMapper.COLORS_ACCENT2_KEY, null, theme.Colors.Accent2));
				resource.Dictionary.Add(ThemeMapper.COLORS_ACCENT3_KEY, new ResourceEntry(ThemeMapper.COLORS_ACCENT3_KEY, null, theme.Colors.Accent3));
				resource.Dictionary.Add(ThemeMapper.COLORS_ACCENT4_KEY, new ResourceEntry(ThemeMapper.COLORS_ACCENT4_KEY, null, theme.Colors.Accent4));
				resource.Dictionary.Add(ThemeMapper.COLORS_ACCENT5_KEY, new ResourceEntry(ThemeMapper.COLORS_ACCENT5_KEY, null, theme.Colors.Accent5));
				resource.Dictionary.Add(ThemeMapper.COLORS_ACCENT6_KEY, new ResourceEntry(ThemeMapper.COLORS_ACCENT6_KEY, null, theme.Colors.Accent6));
				resource.Dictionary.Add(ThemeMapper.COLORS_HYPERLINK_KEY, new ResourceEntry(ThemeMapper.COLORS_HYPERLINK_KEY, null, theme.Colors.Hyperlink));
				resource.Dictionary.Add(ThemeMapper.COLORS_HYPERLINKFOLLOWED_KEY, new ResourceEntry(ThemeMapper.COLORS_HYPERLINKFOLLOWED_KEY, null, theme.Colors.HyperlinkFollowed));
			}

			if(theme.Fonts != null && (theme.Fonts.MajorFont != null || theme.Fonts.MinorFont != null))
			{
				if(builder == null)
					builder = new StringBuilder();

				if(theme.Fonts.MajorFont != null)
					builder.Append($"{nameof(IThemeInfo.MajorFontFamily)}:{theme.Fonts.MajorFont.Family};");
				if(theme.Fonts.MinorFont != null)
					builder.Append($"{nameof(IThemeInfo.MinorFontFamily)}:{theme.Fonts.MinorFont.Family};");

				if(resource.Dictionary == null)
					resource.Dictionary = new Dictionary<string, ResourceEntry>(StringComparer.OrdinalIgnoreCase);

				resource.Dictionary.Add(ThemeMapper.FONTS_MAJOR_KEY, new ResourceEntry(ThemeMapper.FONTS_MAJOR_KEY, "String", GetFontText(theme.Fonts.MajorFont)));
				resource.Dictionary.Add(ThemeMapper.FONTS_MINOR_KEY, new ResourceEntry(ThemeMapper.FONTS_MINOR_KEY, "String", GetFontText(theme.Fonts.MinorFont)));
			}

			if(builder != null)
				resource.Extra = builder.ToString();

			if(theme.Images != null && theme.Images.Length > 0)
			{
				if(resource.Dictionary == null)
					resource.Dictionary = new Dictionary<string, ResourceEntry>(StringComparer.OrdinalIgnoreCase);

				for(int i = 0; i < theme.Images.Length; i++)
				{
					var image = theme.Images[i];

					if(image == null || string.IsNullOrEmpty(image.ImageData))
						continue;

					var entry = new ResourceEntry(
						"Images:" + image.Name,
						image.MIMEType?.Replace('\\', '/'),
						Convert.FromBase64String(image.ImageData));

					resource.Dictionary.Add(entry.Name, entry);
				}
			}

			if(theme.Constants != null && theme.Constants.Length > 0)
			{
				if(resource.Dictionary == null)
					resource.Dictionary = new Dictionary<string, ResourceEntry>(StringComparer.OrdinalIgnoreCase);

				for(int i = 0; i < theme.Constants.Length; i++)
				{
					var constant = theme.Constants[i];

					if(constant == null || string.IsNullOrEmpty(constant.Value))
						continue;

					var entry = new ResourceEntry("Constants:" + constant.Key, null, constant.Value);
					resource.Dictionary.Add(entry.Name, entry);
				}
			}

			static string GetFontText(ThemeFont font)
			{
				if(font == null)
					return null;

				return $"{font.Family},{font.Style},{font.Size},{font.Weight}";
			}

			return resource;
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

		private static string GetElementContent(XmlReader reader)
		{
			if(reader.NodeType != XmlNodeType.Element || reader.IsEmptyElement)
				return null;

			if(reader.Read() && reader.NodeType == XmlNodeType.Text)
				return reader.Value;

			return null;
		}

		private static ThemeColors ResolveColors(XmlReader reader)
		{
			var result = new ThemeColors();
			var depth = reader.Depth;

			while(reader.Read())
			{
				if(reader.Depth <= depth)
					return result;

				if(reader.NodeType != XmlNodeType.Element || reader.IsEmptyElement)
					continue;

				switch(reader.LocalName)
				{
					case XML_COLOR_DARK1_ELEMENT:
						result.Dark1 = GetElementContent(reader);
						break;
					case XML_COLOR_DARK2_ELEMENT:
						result.Dark2 = GetElementContent(reader);
						break;
					case XML_COLOR_LIGHT1_ELEMENT:
						result.Light1 = GetElementContent(reader);
						break;
					case XML_COLOR_LIGHT2_ELEMENT:
						result.Light2 = GetElementContent(reader);
						break;
					case XML_COLOR_ACCENT1_ELEMENT:
						result.Accent1 = GetElementContent(reader);
						break;
					case XML_COLOR_ACCENT2_ELEMENT:
						result.Accent2 = GetElementContent(reader);
						break;
					case XML_COLOR_ACCENT3_ELEMENT:
						result.Accent3 = GetElementContent(reader);
						break;
					case XML_COLOR_ACCENT4_ELEMENT:
						result.Accent4 = GetElementContent(reader);
						break;
					case XML_COLOR_ACCENT5_ELEMENT:
						result.Accent5 = GetElementContent(reader);
						break;
					case XML_COLOR_ACCENT6_ELEMENT:
						result.Accent6 = GetElementContent(reader);
						break;
					case XML_COLOR_HYPERLINK_ELEMENT:
						result.Hyperlink = GetElementContent(reader);
						break;
					case XML_COLOR_HYPERLINKFOLLOWED_ELEMENT:
						result.HyperlinkFollowed = GetElementContent(reader);
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

				if(reader.NodeType != XmlNodeType.Element || reader.IsEmptyElement)
					continue;

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

				if(reader.NodeType != XmlNodeType.Element || reader.IsEmptyElement)
					continue;

				switch(reader.LocalName)
				{
					case XML_FONT_FAMILY_ELEMENT:
						result.Family = GetElementContent(reader);
						break;
					case XML_FONT_STYLE_ELEMENT:
						result.Style = GetElementContent(reader);
						break;
					case XML_FONT_SIZE_ELEMENT:
						result.Size = GetElementContent(reader);
						break;
					case XML_FONT_WEIGHT_ELEMENT:
						result.Weight = GetElementContent(reader);
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

				if(reader.NodeType != XmlNodeType.Element || reader.IsEmptyElement)
					continue;

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

				if(reader.NodeType != XmlNodeType.Element || reader.IsEmptyElement)
					continue;

				switch(reader.LocalName)
				{
					case XML_IMAGE_NAME_ELEMENT:
						result.Name = GetElementContent(reader);
						break;
					case XML_IMAGE_TYPE_ELEMENT:
						result.MIMEType = GetElementContent(reader);
						break;
					case XML_IMAGE_DATA_ELEMENT:
						result.ImageData = GetElementContent(reader);
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

				if(reader.NodeType != XmlNodeType.Element || reader.IsEmptyElement)
					continue;

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

				if(reader.NodeType != XmlNodeType.Element || reader.IsEmptyElement)
					continue;

				switch(reader.LocalName)
				{
					case XML_CONSTANT_KEY_ELEMENT:
						result.Key = GetElementContent(reader);
						break;
					case XML_CONSTANT_VALUE_ELEMENT:
						result.Value = GetElementContent(reader);
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
