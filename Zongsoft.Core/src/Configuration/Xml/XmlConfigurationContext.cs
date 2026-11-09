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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
