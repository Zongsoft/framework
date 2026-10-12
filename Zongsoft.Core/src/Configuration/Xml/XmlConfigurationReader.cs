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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Configuration.Xml;

internal static class XmlConfigurationReader
{
	#region 常量定义
	private const string XML_OPTION_ELEMENT = "option";
	private const string XML_OPTION_PATH_ATTRIBUTE = "path";

	private const string XML_KEY_ATTRIBUTE = "key";
	private const string XML_NAME_ATTRIBUTE = "name";

	private static readonly System.Buffers.SearchValues<char> ILLEGAL_CHARACTERS = System.Buffers.SearchValues.Create([':', ';', '/', '\\', '*', '?', '[', ']', '{', '}']);
	#endregion

	#region 读取方法
	internal static IDictionary<string, string> Read(Stream stream, string filePath, XmlConfigurationSession session = null, XmlConfigurationSession.File file = null)
	{
		ArgumentNullException.ThrowIfNull(stream);

		filePath ??= stream is FileStream input ? input.Name : string.Empty;

		using var reader = XmlReader.Create(stream, new XmlReaderSettings()
		{
			CloseInput = false,
			IgnoreComments = true,
			IgnoreWhitespace = true,
			DtdProcessing = DtdProcessing.Prohibit,
			XmlResolver = null,
		});

		var context = new Context(reader, file == null ? new ConfigurationData() : (ConfigurationData)file.Context.Configuration);
		var elementNames = new List<string>();
		var instructions = file?.Instructions ?? new List<XmlConfigurationInstruction>();
		string optionPath = null;
		var included = true;

		while(reader.Read())
		{
			switch(reader.NodeType)
			{
				case XmlNodeType.Element:
					if(reader.Depth == 0)
					{
						if(reader.LocalName != "configuration" && reader.LocalName != "options")
							throw new FormatException(string.Format(Properties.Resources.Error_IllegalRootNodeName_Message, reader.LocalName, GetLineInfo(reader)));
					}
					else if(reader.Depth == 1)
					{
						context.DoOption();
						optionPath = XmlConfigurationUtility.NormalizeOptionPath(reader.GetAttribute(XML_OPTION_PATH_ATTRIBUTE));
						included = file?.SelectedOptionPath == null || string.Equals(optionPath, file.SelectedOptionPath, StringComparison.OrdinalIgnoreCase);
					}
					else if(included)
						context.DoElement();

					if(!reader.IsEmptyElement)
						elementNames.Add(reader.Name);

					break;
				case XmlNodeType.EndElement:
					if(included)
						context.DoElementEnd();
					elementNames.RemoveAt(elementNames.Count - 1);
					break;
				case XmlNodeType.CDATA:
				case XmlNodeType.Text:
					if(included)
						context.DoText();
					break;
				case XmlNodeType.ProcessingInstruction:
					if(included)
					{
						var position = (IXmlLineInfo)reader;
						var instruction = new XmlConfigurationInstruction(reader.Name, reader.Value,
							elementNames.Count == 0 ? string.Empty : "/" + string.Join('/', elementNames),
							filePath, position.LineNumber, position.LinePosition);
						instructions.Add(instruction);

						if(session != null)
						{
							var count = context.Data.Count;
							var version = file.ContentVersion;
							session.Process(file, instruction, optionPath, reader.Depth);

							//有效导入在 option 内提供了内容，不再把该 option 当作空集合项。
							if(reader.Depth == 2 && (count != context.Data.Count || version != file.ContentVersion))
								context.PreviousNodeType = XmlNodeType.EndElement;
						}
					}

					continue;
				case XmlNodeType.XmlDeclaration:
				case XmlNodeType.Comment:
				case XmlNodeType.Whitespace:
					continue;
				default:
					throw new FormatException(string.Format(Properties.Resources.Error_UnsupportedNodeType_Message, reader.NodeType, GetLineInfo(reader)));
			}

			context.Next();
			if(reader.Depth == 1 && (reader.NodeType == XmlNodeType.EndElement || reader.NodeType == XmlNodeType.Element && reader.IsEmptyElement))
			{
				optionPath = null;
				included = true;
			}
		}

		context.Data.Instructions = instructions.AsReadOnly();

		return context.Data;
	}
	#endregion

	#region 内部方法
	internal static IDictionary<string, string> CreateData() => new ConfigurationData();

	internal static IReadOnlyList<XmlConfigurationInstruction> GetInstructions(IDictionary<string, string> data) => data is ConfigurationData configuration ? configuration.Instructions : [];
	#endregion

	#region 私有方法
	private static string GetLineInfo(XmlReader reader)
	{
		if(reader is IXmlLineInfo info)
			return string.Format(Properties.Resources.LinePositionInfo, info.LineNumber, info.LinePosition);

		return string.Empty;
	}
	#endregion

	#region 配置数据
	//将指令与同次读取的数据一起发布；文件基类清空数据时也自然清空指令视图。
	private sealed class ConfigurationData() : Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		public IReadOnlyList<XmlConfigurationInstruction> Instructions { get; set; } = [];
	}
	#endregion

	private partial struct Context
	{
		#region 构造函数
		public Context(XmlReader reader, ConfigurationData data)
		{
			this.Reader = reader;
			this.PreviousNodeType = reader.NodeType;
			this.Data = data;
			this.Paths = new();
			this.DeclaredKeys = new(StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共字段
		public XmlNodeType PreviousNodeType;
		public readonly XmlReader Reader;
		public readonly Stack<string> Paths;
		public readonly ConfigurationData Data;
		public readonly HashSet<string> DeclaredKeys;
		#endregion

		#region 公共属性
		public readonly int Depth => this.Reader.Depth;
		#endregion

		#region 公共方法
		public void Next()
		{
			if(this.Reader.NodeType == XmlNodeType.Element && this.Reader.IsEmptyElement)
				this.PreviousNodeType = XmlNodeType.EndElement;
			else
				this.PreviousNodeType = this.Reader.NodeType;
		}

		public readonly string GetPath() => ConfigurationPath.Combine(this.Paths.Reverse());
		public readonly string GetPath(string part) => ConfigurationPath.Combine([.. this.Paths.Reverse(), part]);

		public readonly void Clear() => this.Paths.Clear();
		public readonly void Indent(string path) => this.Paths.Push(path);
		public readonly string Dedent() => this.Paths.Pop();
		#endregion
	}

	partial struct Context
	{
		#region 公共方法
		public readonly void DoOption()
		{
			//确保配置元素必须位于<option>节点之内
			if(this.Reader.LocalName != XML_OPTION_ELEMENT)
				throw new FormatException(string.Format(Properties.Resources.Error_InvalidOptionConfigurationFileFormat_Message, GetLineInfo(this.Reader)));

			this.Clear();
			var path = XmlConfigurationUtility.NormalizeOptionPath(this.Reader.GetAttribute(XML_OPTION_PATH_ATTRIBUTE));

			if(path.Length > 0)
				this.Indent(path);
		}

		public readonly void DoElement()
		{
			var elementName = this.Reader.LocalName;
			this.Indent(elementName);

			for(int i = 0; i < this.Reader.AttributeCount; i++)
			{
				this.Reader.MoveToAttribute(i);

				if(!string.IsNullOrEmpty(this.Reader.NamespaceURI))
					throw new FormatException(string.Format(Properties.Resources.Error_NamespaceIsNotSupported_Message, GetLineInfo(this.Reader)));

				if(i == 0 &&
				   string.Equals(this.Reader.LocalName, elementName + "." + XML_KEY_ATTRIBUTE, StringComparison.OrdinalIgnoreCase) ||
				   string.Equals(this.Reader.LocalName, elementName + "." + XML_NAME_ATTRIBUTE, StringComparison.OrdinalIgnoreCase))
				{
					if(this.Reader.Value.IndexOfAny(ILLEGAL_CHARACTERS) >= 0)
						throw new FormatException(string.Format(Properties.Resources.Error_IllegalConfigurationKeyValue_Message, this.Reader.Value, GetLineInfo(this.Reader)));

					this.Dedent();

					if(string.IsNullOrWhiteSpace(this.Reader.Value))
						throw new ConfigurationException(string.Format(Properties.Resources.Configuration_EntryNameRequired_Message, this.Paths.Reverse(), GetLineInfo(this.Reader)));

					this.Indent(this.Reader.Value);
					this.Indent(this.Reader.LocalName[(elementName.Length + 1)..]);
				}
				else
				{
					if(this.Reader.LocalName.Contains('.'))
						throw new FormatException(string.Format(Properties.Resources.Error_IllegalConfigurationAttributeName_Message, this.Reader.LocalName, GetLineInfo(this.Reader)));

					this.Indent(this.Reader.LocalName);
				}

				var key = this.GetPath();
				if(!this.DeclaredKeys.Add(key))
					throw new FormatException(string.Format(Properties.Resources.Error_KeyIsDuplicated_Message, key, GetLineInfo(this.Reader)));

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
				if(this.PreviousNodeType == XmlNodeType.Element)
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

			if(!this.DeclaredKeys.Add(key))
				throw new FormatException(string.Format(Properties.Resources.Error_KeyIsDuplicated_Message, key, GetLineInfo(this.Reader)));

			this.Data[key] = this.Reader.Value;
		}
		#endregion
	}
}
