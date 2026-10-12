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
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration.Xml;

/// <summary>从调用方提供的流加载 XML 配置并执行处理指令。</summary>
/// <param name="source">XML 配置流来源及其加载选项。</param>
public class XmlStreamConfigurationProvider(XmlStreamConfigurationSource source) : StreamConfigurationProvider(source)
{
	/// <summary>获取当前配置数据对应的处理指令只读快照，按导入位置展开并保留重复声明。</summary>
	public IReadOnlyList<XmlConfigurationInstruction> Instructions => GetInstructions(this.Data);

	/// <summary>读取配置并执行指令，成功后替换当前快照，不关闭调用方的流。</summary>
	/// <param name="stream">配置输入流，由调用方负责关闭。</param>
	public override void Load(Stream stream)
	{
		var source = (XmlStreamConfigurationSource)this.Source;
		var session = new XmlConfigurationSession(source.Options);
		this.Data = session.Read(stream, XmlConfigurationSession.Source.Create(stream), source.FilePath);
	}

	/// <summary>读取配置数据，不执行指令或关闭调用方的流。</summary>
	/// <param name="stream">配置输入流。</param>
	/// <returns>配置键不区分大小写的配置字典。</returns>
	public static IDictionary<string, string> Read(Stream stream) => Read(stream, out _);

	/// <summary>读取配置及全部原始处理指令，不执行指令或关闭调用方的流。</summary>
	/// <param name="stream">配置输入流。</param>
	/// <param name="instructions">读取成功后按文档顺序排列的指令只读快照。</param>
	/// <param name="filePath">仅用于记录来源；<see langword="null"/> 从 <see cref="FileStream.Name"/> 获取，其它流采用空字符串。</param>
	/// <returns>配置键不区分大小写的配置字典。</returns>
	public static IDictionary<string, string> Read(Stream stream, out IReadOnlyList<XmlConfigurationInstruction> instructions, string filePath = null)
	{
		instructions = [];
		var data = XmlConfigurationReader.Read(stream, filePath);
		instructions = GetInstructions(data);
		return data;
	}

	internal static IReadOnlyList<XmlConfigurationInstruction> GetInstructions(IDictionary<string, string> data) => XmlConfigurationReader.GetInstructions(data);
}
