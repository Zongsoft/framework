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

namespace Zongsoft.Plugins;

/// <summary>表示解析器的接口。</summary>
public interface IParser
{
	/// <summary>获取解析器目标对象的类型。</summary>
	/// <param name="context">解析器上下文对象。</param>
	/// <returns>返回的目标类型。</returns>
	/// <remarks>该方法尽量以不构建目标类型的方式去获取目标类型。</remarks>
	Type GetValueType(Parsers.ParserContext context);

	/// <summary>解析表达式，返回目标对象。</summary>
	/// <param name="context">解析器上下文对象。</param>
	/// <returns>返回解析后的目标对象。</returns>
	object Parse(Parsers.ParserContext context);
}
