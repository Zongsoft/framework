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

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration.Xml;

/// <summary>表示 XML 配置流来源及其加载选项。</summary>
public class XmlStreamConfigurationSource : StreamConfigurationSource
{
	/// <summary>获取本来源的 XML 加载及指令选项。</summary>
	public XmlConfigurationOptions Options { get; } = new();

	/// <summary>获取或设置指令的来源路径；为 <see langword="null"/> 时从文件流获取路径，其它流采用空字符串。</summary>
	/// <remarks>仅用于来源定位，不打开该路径，也不作为相对导入的路径基准。</remarks>
	public string FilePath { get; set; }

	public override IConfigurationProvider Build(IConfigurationBuilder builder)
	{
		return new XmlStreamConfigurationProvider(this);
	}
}
