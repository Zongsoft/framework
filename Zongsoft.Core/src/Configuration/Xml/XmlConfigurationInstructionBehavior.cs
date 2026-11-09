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

namespace Zongsoft.Configuration.Xml;

/// <summary>表示 XML 处理指令的处理行为。</summary>
public enum XmlConfigurationInstructionBehavior
{
	/// <summary>未指定具体处理行为，采用该指令的内置默认行为。</summary>
	None = 0,
	/// <summary>严格处理指令，具体校验规则由该指令定义。</summary>
	Strict,
	/// <summary>忽略指令，只收集原始定义，不执行或通知指令回调。</summary>
	Ignore,
	/// <summary>禁止指令，如果存在该指令则抛出异常。</summary>
	Suppress,
}
