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

/// <summary>表示 XML 配置文件来源及其加载选项。</summary>
public class XmlConfigurationSource : FileConfigurationSource
{
	/// <summary>初始化 <see cref="XmlConfigurationSource"/> 类的新实例。</summary>
	public XmlConfigurationSource() { }
	internal XmlConfigurationSource(XmlConfigurationOptions options) => this.Options = options;

	/// <summary>获取本来源的 XML 加载及指令选项。</summary>
	public XmlConfigurationOptions Options { get; } = new();

	public override IConfigurationProvider Build(IConfigurationBuilder builder)
	{
		if(builder == null)
			this.ResolveFileProvider();
		else
			this.EnsureDefaults(builder);

		return new XmlConfigurationProvider(this);
	}
}
