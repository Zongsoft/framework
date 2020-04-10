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
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration.Options
{
	public class OptionStreamConfigurationProvider : StreamConfigurationProvider
	{
		#region 常量定义
		private const string XML_OPTION_ELEMENT = "option";
		private const string XML_OPTION_PATH_ATTRIBUTE = "path";

		private const string XML_KEY_ATTRIBUTE = "key";
		private const string XML_NAME_ATTRIBUTE = "name";

		private static readonly char[] ILLEGAL_CHARACTERS = new char[] { ':', '/', '\\', '*', '?' };
		#endregion

		#region 构造函数
		public OptionStreamConfigurationProvider(OptionStreamConfigurationSource source) : base(source)
		{
		}
		#endregion

		#region 重写方法
		public override void Load(Stream stream)
		{
			this.Data = Read(stream);
		}
		#endregion

		#region 公共方法
		public static IDictionary<string, string> Read(Stream stream)
		{
			var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			var settings = new XmlReaderSettings()
			{
				CloseInput = false,
				IgnoreComments = true,
				IgnoreWhitespace = true
			};

			using(var reader = XmlReader.Create(stream, settings))
			{
				var prefixStack = new Stack<string>();

				SkipUntilRootElement(reader);

				//确认根节点的名称是否合法
				if(reader.LocalName != "configuration" && reader.LocalName != "options")
					throw new FormatException(string.Format(Properties.Resources.Error_IllegalRootNodeName, reader.LocalName, GetLineInfo(reader)));

				var preNodeType = reader.NodeType;

				while(reader.Read())
				{
					switch(reader.NodeType)
					{
						case XmlNodeType.Element:
							if(reader.Depth == 1)
							{
								//确保配置元素必须位于<option>节点之内
								if(reader.LocalName != XML_OPTION_ELEMENT)
									throw new FormatException(string.Format(Properties.Resources.Error_InvalidOptionConfigurationFileFormat, GetLineInfo(reader)));

								prefixStack.Clear();

								var path = reader.GetAttribute(XML_OPTION_PATH_ATTRIBUTE);

								if(!string.IsNullOrWhiteSpace(path))
								{
									path = path.Trim('/', ' ', '\t');

									if(path.Length > 0)
										prefixStack.Push(path.Replace("/", ConfigurationPath.KeyDelimiter));
								}
							}
							else
							{
								var elementName = reader.LocalName;
								prefixStack.Push(elementName);

								for(int i = 0; i < reader.AttributeCount; i++)
								{
									reader.MoveToAttribute(i);

									if(!string.IsNullOrEmpty(reader.NamespaceURI))
										throw new FormatException(string.Format(Properties.Resources.Error_NamespaceIsNotSupported, GetLineInfo(reader)));

									if(i == 0 &&
									   string.Equals(reader.LocalName, elementName + "." + XML_KEY_ATTRIBUTE, StringComparison.OrdinalIgnoreCase) ||
									   string.Equals(reader.LocalName, elementName + "." + XML_NAME_ATTRIBUTE, StringComparison.OrdinalIgnoreCase))
									{
										if(reader.Value.IndexOfAny(ILLEGAL_CHARACTERS) >= 0)
											throw new FormatException(string.Format(Properties.Resources.Error_IllegalConfigurationKeyValue, reader.Value, GetLineInfo(reader)));

										var prefix = prefixStack.Pop();
										prefixStack.Push(ConfigurationPath.Combine(prefix, reader.Value));
										prefixStack.Push(reader.LocalName.Substring(elementName.Length + 1));
									}
									else
									{
										if(reader.LocalName.Contains('.'))
											throw new FormatException(string.Format(Properties.Resources.Error_IllegalConfigurationAttributeName, reader.LocalName, GetLineInfo(reader)));

										prefixStack.Push(reader.LocalName);
									}

									var key = ConfigurationPath.Combine(prefixStack.Reverse());
									if(data.ContainsKey(key))
										throw new FormatException(string.Format(Properties.Resources.Error_KeyIsDuplicated, key, GetLineInfo(reader)));

									data[key] = reader.Value;
									prefixStack.Pop();
								}

								reader.MoveToElement();

								if(reader.IsEmptyElement)
									prefixStack.Pop();
							}

							break;
						case XmlNodeType.EndElement:
							if(prefixStack.Count > 0)
							{
								// 如果上一个节点类型是元素则说明当前元素中没有TEXT/CDATA节点
								if(preNodeType == XmlNodeType.Element)
								{
									var key = ConfigurationPath.Combine(prefixStack.Reverse());
									data[key] = string.Empty;
								}

								prefixStack.Pop();
							}

							break;
						case XmlNodeType.CDATA:
						case XmlNodeType.Text:
							{
								var key = ConfigurationPath.Combine(prefixStack.Reverse());

								if(data.ContainsKey(key))
									throw new FormatException(string.Format(Properties.Resources.Error_KeyIsDuplicated, key, GetLineInfo(reader)));

								data[key] = reader.Value;
							}

							break;
						case XmlNodeType.XmlDeclaration:
						case XmlNodeType.ProcessingInstruction:
						case XmlNodeType.Comment:
						case XmlNodeType.Whitespace:
							break;
						default:
							throw new FormatException(string.Format(Properties.Resources.Error_UnSupportedNodeType, reader.NodeType, GetLineInfo(reader)));
					}

					preNodeType = reader.NodeType;

					if(preNodeType == XmlNodeType.Element && reader.IsEmptyElement)
						preNodeType = XmlNodeType.EndElement;
				}
			}

			return data;
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
	}
}
