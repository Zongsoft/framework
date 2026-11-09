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
using System.ComponentModel;

namespace Zongsoft.Plugins;

/// <summary>表示固定元件的类型。</summary>
public enum FixedElementType
{
	/// <summary>构建器，指实现了 <seealso cref="Zongsoft.Plugins.IBuilder"/> 接口的类。</summary>
	Builder = 0,

	/// <summary>解析器，指实现了 <seealso cref="Zongsoft.Plugins.IParser"/> 接口的类。</summary>
	Parser = 1,
}
