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

public class XmlStreamConfigurationProvider(XmlStreamConfigurationSource source) : StreamConfigurationProvider(source)
{
	#region 常量定义
	private const string XML_OPTION_ELEMENT = "option";
	private const string XML_OPTION_PATH_ATTRIBUTE = "path";

	private const string XML_KEY_ATTRIBUTE = "key";
	private const string XML_NAME_ATTRIBUTE = "name";

	private static readonly System.Buffers.SearchValues<char> ILLEGAL_CHARACTERS = System.Buffers.SearchValues.Create([':', ';', '/', '\\', '*', '?', '[', ']', '{', '}']);
	#endregion

	#region 公共属性
	/// <summary>获取当前配置数据对应的处理指令只读快照，按文档顺序保留重复声明。</summary>
	public IReadOnlyList<XmlConfigurationInstruction> Instructions => GetInstructions(this.Data);
	#endregion

	#region 重写方法
	/// <summary>读取 XML 配置和处理指令，成功后替换当前快照。</summary>
	/// <param name="stream">由调用方负责关闭的输入流。</param>
	public override void Load(Stream stream) => this.Data = Read(stream, out _, ((XmlStreamConfigurationSource)this.Source).FilePath);
	#endregion

	#region 公共方法
	/// <summary>读取 XML 配置数据，不关闭输入流。</summary>
	/// <param name="stream">配置输入流。</param>
	/// <returns>使用忽略大小写的键名比较规则的配置字典。</returns>
	public static IDictionary<string, string> Read(Stream stream) => Read(stream, out _);

	/// <summary>读取 XML 配置数据及全部处理指令，不执行指令或关闭输入流。</summary>
	/// <param name="stream">配置输入流。</param>
	/// <param name="instructions">读取成功时按文档顺序排列的指令只读快照；无指令时为空集合。</param>
	/// <param name="filePath">记录在指令中的来源路径；为 <c>null</c> 时从文件流获取路径，其它流采用空字符串。</param>
	/// <returns>使用忽略大小写的键名比较规则的配置字典。</returns>
	public static IDictionary<string, string> Read(Stream stream, out IReadOnlyList<XmlConfigurationInstruction> instructions, string filePath = null)
	{
		ArgumentNullException.ThrowIfNull(stream);

		instructions = [];
		filePath ??= stream is FileStream file ? file.Name : string.Empty;

		using var reader = XmlReader.Create(stream, new XmlReaderSettings()
		{
			CloseInput = false,
			IgnoreComments = true,
			IgnoreWhitespace = true,
			DtdProcessing = DtdProcessing.Prohibit,
			XmlResolver = null,
		});

		var context = new Context(reader);
		var paths = new List<string>();
		List<XmlConfigurationInstruction> declarations = null;

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
						context.DoOption();
					else
						context.DoElement();

					if(!reader.IsEmptyElement)
						paths.Add(reader.Name);

					break;
				case XmlNodeType.EndElement:
					context.DoElementEnd();
					paths.RemoveAt(paths.Count - 1);
					break;
				case XmlNodeType.CDATA:
				case XmlNodeType.Text:
					context.DoText();
					break;
				case XmlNodeType.ProcessingInstruction:
					var position = (IXmlLineInfo)reader;
					(declarations ??= []).Add(new(reader.Name, reader.Value,
						paths.Count == 0 ? string.Empty : "/" + string.Join('/', paths),
						filePath, position.LineNumber, position.LinePosition));
					continue;
				case XmlNodeType.XmlDeclaration:
				case XmlNodeType.Comment:
				case XmlNodeType.Whitespace:
					continue;
				default:
					throw new FormatException(string.Format(Properties.Resources.Error_UnsupportedNodeType_Message, reader.NodeType, GetLineInfo(reader)));
			}

			context.Next();
		}

		instructions = declarations == null ? [] : declarations.AsReadOnly();
		context.Data.Instructions = instructions;

		return context.Data;
	}
	#endregion

	#region 内部方法
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
		public Context(XmlReader reader)
		{
			this.Reader = reader;
			this.Previous = reader.NodeType;
			this.Data = new();
			this.Paths = new();
		}
		#endregion

		#region 公共字段
		public XmlNodeType Previous;
		public readonly XmlReader Reader;
		public readonly Stack<string> Paths;
		public readonly ConfigurationData Data;
		#endregion

		#region 公共属性
		public readonly int Depth => this.Reader.Depth;
		#endregion

		#region 公共方法
		public void Next()
		{
			if(this.Reader.NodeType == XmlNodeType.Element && this.Reader.IsEmptyElement)
				this.Previous = XmlNodeType.EndElement;
			else
				this.Previous = this.Reader.NodeType;
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
				if(this.Data.ContainsKey(key))
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
				throw new FormatException(string.Format(Properties.Resources.Error_KeyIsDuplicated_Message, key, GetLineInfo(this.Reader)));

			this.Data[key] = this.Reader.Value;
		}
		#endregion
	}
}
