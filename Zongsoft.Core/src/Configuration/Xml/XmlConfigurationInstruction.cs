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
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zongsoft.Configuration.Xml;

/// <summary>表示从 XML 配置读取的一条处理指令及其来源位置。</summary>
/// <remarks>实例不可变，只描述声明，不执行指令。属性式参数由调用方按需解析，成功结果可供多个线程共享。</remarks>
public sealed class XmlConfigurationInstruction
{
	#region 成员字段
	private IReadOnlyDictionary<string, string> _attributes;
	#endregion

	#region 构造函数
	internal XmlConfigurationInstruction(string name, string data, string path, string filePath, int lineNumber, int linePosition)
	{
		this.Name = name;
		this.Data = data;
		this.Path = path;
		this.FilePath = filePath;
		this.LineNumber = lineNumber;
		this.LinePosition = linePosition;
	}
	#endregion

	#region 公共属性
	/// <summary>获取指令名称，保留原始大小写，不包含 <c>&lt;?</c> 和 <c>?&gt;</c> 标记。</summary>
	public string Name { get; }
	/// <summary>获取 <see cref="XmlReader.Value"/> 提供的指令数据，不额外裁剪空白或解码实体。</summary>
	/// <remarks>XML 读取器已处理名称与数据之间的分隔空白及 XML 换行规范化；此属性不表示原文件的字节切片。</remarks>
	public string Data { get; }
	/// <summary>获取包含指令的 XML 父元素路径；元素名称以斜杠分隔并以前导斜杠开始，根元素之外为空字符串。</summary>
	/// <remarks>保留元素限定名称与大小写，不包含索引或谓词，不是映射后的配置键路径。</remarks>
	public string Path { get; }
	/// <summary>获取来源文件路径或调用方提供的来源标识；匿名来源为空字符串。</summary>
	public string FilePath { get; }
	/// <summary>获取指令名称起始位置的行号，从 <c>1</c> 开始。</summary>
	public int LineNumber { get; }
	/// <summary>获取指令名称起始位置的列号，从 <c>1</c> 开始。</summary>
	public int LinePosition { get; }
	/// <summary>获取指令数据是否为空。</summary>
	public bool IsEmpty => string.IsNullOrEmpty(this.Data);
	#endregion

	#region 公共方法
	/// <summary>将指令数据完整解析为只读的属性式参数集合。</summary>
	/// <returns>名称忽略大小写的字符串键值集合；空数据或仅包含 XML 空白时返回空集合。</returns>
	/// <exception cref="FormatException">数据不符合 XML 属性语法，或存在忽略大小写后重名的属性。</exception>
	/// <remarks>遵循 XML 属性的引号、空白规范化和实体解码规则，不进行类型转换，也不修改 <see cref="Data"/>。成功结果按实例缓存。</remarks>
	public IReadOnlyDictionary<string, string> GetAttributes()
	{
		var attributes = Volatile.Read(ref _attributes);
		if(attributes != null)
			return attributes;

		attributes = this.ParseAttributes();
		return Interlocked.CompareExchange(ref _attributes, attributes, null) ?? attributes;
	}

	/// <summary>尝试将指令数据完整解析为属性式参数集合。</summary>
	/// <param name="attributes">解析成功时为只读参数集合，失败时为 <c>null</c>，不返回部分结果。</param>
	/// <returns>解析成功则返回 <c>true</c>，属性语法或重名校验失败则返回 <c>false</c>。</returns>
	public bool TryGetAttributes(out IReadOnlyDictionary<string, string> attributes)
	{
		try
		{
			attributes = this.GetAttributes();
			return true;
		}
		catch(FormatException)
		{
			attributes = null;
			return false;
		}
	}
	#endregion

	#region 重写方法
	public override string ToString() => string.IsNullOrEmpty(this.Data) ? $"<?{this.Name}?>" : $"<?{this.Name} {this.Data} ?>";
	#endregion

	#region 私有方法
	private IReadOnlyDictionary<string, string> ParseAttributes()
	{
		try
		{
			using var input = new StringReader($"<instruction {this.Data} />");
			using var reader = XmlReader.Create(input, new XmlReaderSettings()
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null,
			});

			//只接受完整的属性列表，不能借闭合标记注入元素、注释或尾部内容
			if(!reader.Read() || reader.NodeType != XmlNodeType.Element || !reader.IsEmptyElement)
				throw this.CreateAttributesException();

			var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			while(reader.MoveToNextAttribute())
			{
				if(!attributes.TryAdd(reader.Name, reader.Value))
					throw this.CreateAttributesException();
			}

			reader.MoveToElement();
			if(reader.Read())
				throw this.CreateAttributesException();

			return new ReadOnlyDictionary<string, string>(attributes);
		}
		catch(XmlException exception)
		{
			throw this.CreateAttributesException(exception);
		}
	}

	private FormatException CreateAttributesException(Exception exception = null) =>
		new(string.Format(Properties.Resources.Configuration_Xml_InstructionAttributesInvalid_Message, this.Name, this.FilePath, this.LineNumber, this.LinePosition), exception);
	#endregion
}
