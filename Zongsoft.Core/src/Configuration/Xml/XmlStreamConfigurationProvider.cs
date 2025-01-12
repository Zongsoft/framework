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

		private static readonly char[] ILLEGAL_CHARACTERS = [':', '/', '\\', '*', '?'];

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
						//if(context.Indexes.Count < context.Depth)
						//	context.Indexes.Push(0);

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
						throw new FormatException(string.Format(Properties.Resources.Error_UnSupportedNodeType, reader.NodeType, GetLineInfo(reader)));
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
				return string.Format(Properties.Resources.Text_LinePositionInfo, info.LineNumber, info.LinePosition);

			return string.Empty;
		}
		#endregion

		#region 静态方法
		private static volatile int _count;
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _counters = new(StringComparer.OrdinalIgnoreCase);
		private static int Increase(string path) => _counters.AddOrUpdate(path ?? string.Empty, 1, (key, value) => value + 1);
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
					var index = 0;
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

						if(string.IsNullOrWhiteSpace(this.Reader.Value) || this.Reader.Value == "#")
							this.Indent($"#{index = Interlocked.Increment(ref _count)}");
						else
							this.Indent(this.Reader.Value);

						this.Indent(this.Reader.LocalName.Substring(elementName.Length + 1));
					}
					else
					{
						if(this.Reader.LocalName.Contains('.'))
							throw new FormatException(string.Format(Properties.Resources.Error_IllegalConfigurationAttributeName, this.Reader.LocalName, GetLineInfo(this.Reader)));

						this.Indent(this.Reader.LocalName);
					}

					var key = ConfigurationPath.Combine(this.Paths.Reverse());
					if(this.Data.ContainsKey(key))
						throw new FormatException(string.Format(Properties.Resources.Error_KeyIsDuplicated, key, GetLineInfo(this.Reader)));

					if(index > 0)
						this.Data[key] = index.ToString();
					else
						this.Data[key] = this.Reader.Value;

					this.Dedent();
				}

				this.Reader.MoveToElement();

				if(this.Reader.IsEmptyElement)
					this.Dedent();
			}

			public readonly void DoElementEnd()
			{
				if(this.Paths.Count > 0)
				{
					// 如果上一个节点类型是元素则说明当前元素中没有TEXT/CDATA节点
					if(this.Previous == XmlNodeType.Element)
					{
						var key = ConfigurationPath.Combine(this.Paths.Reverse());
						this.Data[key] = string.Empty;
					}

					this.Dedent();
				}

				//if(this.Indexes.Count > 0)
				//	this.Indexes.Pop();
			}

			public readonly void DoText()
			{
				var key = ConfigurationPath.Combine(this.Paths.Reverse());

				if(!string.IsNullOrWhiteSpace(this.Reader.Value))
					key = ConfigurationPath.Combine(key, this.Reader.Value.Trim());

				if(this.Data.ContainsKey(key))
					throw new FormatException(string.Format(Properties.Resources.Error_KeyIsDuplicated, key, GetLineInfo(this.Reader)));

				this.Data[key] = this.Reader.Value;
			}
		}
	}
}
