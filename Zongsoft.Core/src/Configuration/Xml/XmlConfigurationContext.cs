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
using System.Collections.Generic;

namespace Zongsoft.Configuration.Xml;

/// <summary>提供当前 XML 配置文件的加载上下文。</summary>
public class XmlConfigurationContext
{
	internal XmlConfigurationContext(string filePath, int depth, XmlConfigurationInstruction referer, IDictionary<string, string> configuration)
	{
		this.FilePath = filePath;
		this.Depth = depth;
		this.Referer = referer;
		this.Configuration = configuration;
	}

	/// <summary>获取当前来源路径或标识，匿名来源为空字符串。</summary>
	public string FilePath { get; }
	/// <summary>获取加载层数，根文件为一。</summary>
	public int Depth { get; }
	/// <summary>获取触发本次导入的原始指令，根文件为 <see langword="null"/>。</summary>
	public XmlConfigurationInstruction Referer { get; }
	/// <summary>获取当前文件已加载的配置，修改参与后续合并；不包含引用方的配置。</summary>
	public IDictionary<string, string> Configuration { get; }
}
