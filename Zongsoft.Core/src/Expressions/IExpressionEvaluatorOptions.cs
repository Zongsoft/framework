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
using System.IO;

namespace Zongsoft.Expressions;

/// <summary>表示表达式运算器配置选项的接口。</summary>
public interface IExpressionEvaluatorOptions
{
	/// <summary>获取或设置错误输出通道。</summary>
	TextWriter Error { get; set; }

	/// <summary>获取或设置输入通道。</summary>
	TextReader Input { get; set; }

	/// <summary>获取或设置输出通道。</summary>
	TextWriter Output { get; set; }

	/// <summary>获取配置选项的属性集。</summary>
	Collections.Parameters Properties { get; }
}
