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
