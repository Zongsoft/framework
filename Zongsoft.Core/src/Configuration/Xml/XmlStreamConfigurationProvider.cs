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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration.Xml
{
	public class XmlStreamConfigurationProvider(XmlStreamConfigurationSource source) : StreamConfigurationProvider(source)
	{
		#region 常量定义
		private const string XML_OPTION_ELEMENT = "option";
		private const string XML_OPTION_PATH_ATTRIBUTE = "path";

		private const string XML_KEY_ATTRIBUTE = "key";
		private const string XML_NAME_ATTRIBUTE = "name";

		private static readonly char[] ILLEGAL_CHARACTERS = [':', ';', '/', '\\', '*', '?', '[', ']', '{', '}'];

		#endregion

		#region 重写方法
		public override void Load(Stream stream) => this.Data = Read(stream);
		#endregion

		#region 公共方法
		public static IDictionary<string, string> Read(Stream stream)
		{
			using var reader = XmlReader.Create(stream, new XmlReaderSettings()
			{
				CloseInput = false,
				IgnoreComments = true,
				IgnoreWhitespace = true
			});

			//移动到根节点
			SkipUntilRootElement(reader);

			//确认根节点的名称是否合法
			if(reader.LocalName != "configuration" && reader.LocalName != "options")
				throw new FormatException(string.Format(Properties.Resources.Error_IllegalRootNodeName, reader.LocalName, GetLineInfo(reader)));

			var context = new Context(reader);

			while(reader.Read())
			{
				switch(reader.NodeType)
				{
					case XmlNodeType.Element:
						if(reader.Depth == 1)
							context.DoOption();
						else
							context.DoElement();

						break;
					case XmlNodeType.EndElement:
						context.DoElementEnd();
						break;
					case XmlNodeType.CDATA:
					case XmlNodeType.Text:
						context.DoText();
						break;
					case XmlNodeType.XmlDeclaration:
					case XmlNodeType.ProcessingInstruction:
					case XmlNodeType.Comment:
					case XmlNodeType.Whitespace:
						break;
					default:
						throw new FormatException(string.Format(Properties.Resources.Error_UnsupportedNodeType, reader.NodeType, GetLineInfo(reader)));
				}

				context.Next();
			}

			return context.Data;
		}
		#endregion

		#region 私有方法
		private static void SkipUntilRootElement(XmlReader reader)
		{
			while(reader.Read())
			{
				if(reader.NodeType != XmlNodeType.XmlDeclaration && reader.NodeType != XmlNodeType.ProcessingInstruction)
					break;
			}
		}

		private static string GetLineInfo(XmlReader reader)
		{
			if(reader is IXmlLineInfo info)
				return string.Format(Properties.Resources.LinePositionInfo, info.LineNumber, info.LinePosition);

			return string.Empty;
		}
		#endregion

		private partial struct Context
		{
			public Context(XmlReader reader)
			{
				this.Reader = reader;
				this.Previous = reader.NodeType;
				this.Data = new(StringComparer.OrdinalIgnoreCase);
				this.Paths = new();
			}

			public readonly Dictionary<string, string> Data;
			public readonly XmlReader Reader;
			public readonly Stack<string> Paths;

			public XmlNodeType Previous;
			public readonly int Depth => this.Reader.Depth;

			public void Next()
			{
				if(this.Reader.NodeType == XmlNodeType.Element && this.Reader.IsEmptyElement)
					this.Previous = XmlNodeType.EndElement;
				else
					this.Previous = this.Reader.NodeType;
			}

			public readonly string GetPath() => ConfigurationPath.Combine(this.Paths.Reverse());
			public readonly string GetPath(string part) => ConfigurationPath.Combine([..this.Paths.Reverse(), part]);

			public readonly void Clear() => this.Paths.Clear();
			public readonly void Indent(string path) => this.Paths.Push(path);
			public readonly string Dedent() => this.Paths.Pop();
		}

		partial struct Context
		{
			public readonly void DoOption()
			{
				//确保配置元素必须位于<option>节点之内
				if(this.Reader.LocalName != XML_OPTION_ELEMENT)
					throw new FormatException(string.Format(Properties.Resources.Error_InvalidOptionConfigurationFileFormat, GetLineInfo(this.Reader)));

				this.Clear();
				var path = this.Reader.GetAttribute(XML_OPTION_PATH_ATTRIBUTE);

				if(!string.IsNullOrWhiteSpace(path))
				{
					path = path.Trim('/', ' ', '\t');

					if(path.Length > 0)
						this.Indent(path.Replace("/", ConfigurationPath.KeyDelimiter));
				}
			}

			public readonly void DoElement()
			{
				var elementName = this.Reader.LocalName;
				this.Indent(elementName);

				for(int i = 0; i < this.Reader.AttributeCount; i++)
				{
					this.Reader.MoveToAttribute(i);

					if(!string.IsNullOrEmpty(this.Reader.NamespaceURI))
						throw new FormatException(string.Format(Properties.Resources.Error_NamespaceIsNotSupported, GetLineInfo(this.Reader)));

					if(i == 0 &&
					   string.Equals(this.Reader.LocalName, elementName + "." + XML_KEY_ATTRIBUTE, StringComparison.OrdinalIgnoreCase) ||
					   string.Equals(this.Reader.LocalName, elementName + "." + XML_NAME_ATTRIBUTE, StringComparison.OrdinalIgnoreCase))
					{
						if(this.Reader.Value.IndexOfAny(ILLEGAL_CHARACTERS) >= 0)
							throw new FormatException(string.Format(Properties.Resources.Error_IllegalConfigurationKeyValue, this.Reader.Value, GetLineInfo(this.Reader)));

						this.Dedent();

						if(string.IsNullOrWhiteSpace(this.Reader.Value))
							throw new ConfigurationException($"Missing name or key for '{this.Paths.Reverse()}' configuration entry. Located: {GetLineInfo(this.Reader)}");

						this.Indent(this.Reader.Value);
						this.Indent(this.Reader.LocalName[(elementName.Length + 1)..]);
					}
					else
					{
						if(this.Reader.LocalName.Contains('.'))
							throw new FormatException(string.Format(Properties.Resources.Error_IllegalConfigurationAttributeName, this.Reader.LocalName, GetLineInfo(this.Reader)));

						this.Indent(this.Reader.LocalName);
					}

					var key = this.GetPath();;
					if(this.Data.ContainsKey(key))
						throw new FormatException(string.Format(Properties.Resources.Error_KeyIsDuplicated, key, GetLineInfo(this.Reader)));

					this.Data[key] = this.Reader.Value;

					this.Dedent();
				}

				this.Reader.MoveToElement();

				if(this.Reader.IsEmptyElement)
				{
					//如果当前元素为纯空元素（即无Attribute，也无TEXT/CDATA节点）
					//因此必须将其作为值为空的单值集合元素处理，参考DoText()方法的实现逻辑
					if(this.Reader.AttributeCount == 0)
					{
						var key = this.GetPath("[]");
						this.Data[key] = string.Empty;
					}

					this.Dedent();
				}
			}

			public readonly void DoElementEnd()
			{
				if(this.Paths.Count > 0)
				{
					//如果上一个节点类型是元素则说明其为纯空元素（即无Attribute也无TEXT/CDATA节点）
					//因此必须将其作为值为空的单值集合元素处理，参考DoText()方法的实现逻辑
					if(this.Previous == XmlNodeType.Element)
					{
						var key = this.GetPath("[]");
						this.Data[key] = string.Empty;
					}

					this.Dedent();
				}
			}

			public readonly void DoText()
			{
				var key = this.GetPath();

				//文本节点所在的元素必须当作单值集合元素处理，即该集合类型为ICollection类型
				if(string.IsNullOrWhiteSpace(this.Reader.Value))
					key = ConfigurationPath.Combine(key, "[]");
				else
					key = ConfigurationPath.Combine(key, $"[{this.Reader.Value.Trim()}]");

				if(this.Data.ContainsKey(key))
					throw new FormatException(string.Format(Properties.Resources.Error_KeyIsDuplicated, key, GetLineInfo(this.Reader)));

				this.Data[key] = this.Reader.Value;
			}
		}
	}
}
