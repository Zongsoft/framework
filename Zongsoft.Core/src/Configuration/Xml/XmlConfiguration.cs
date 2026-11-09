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

using Zongsoft.Configuration.Xml.Instructions;

namespace Zongsoft.Configuration.Xml;

/// <summary>提供 XML 配置的指令注册入口。</summary>
public static class XmlConfiguration
{
	/// <summary>获取全局处理器注册表，默认注册导入指令；每次根加载固定一份快照。</summary>
	public static XmlConfigurationInstructionProcessorCollection Instructions { get; } = [ImportInstructionProcessor.Instance];
}
