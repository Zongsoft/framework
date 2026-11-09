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

namespace Zongsoft.Components;

/// <summary>表示输出样式的枚举。</summary>
[Flags]
public enum CommandOutletStyles
{
	/// <summary>无样式</summary>
	None = 0,
	/// <summary>普通文本</summary>
	Regular = 0,
	/// <summary>加粗文本</summary>
	Bold = 1,
	/// <summary>倾斜文本</summary>
	Italic = 2,
	/// <summary>下划线</summary>
	Underline = 4,
	/// <summary>删除线</summary>
	Strikeout = 8,
	/// <summary>闪烁</summary>
	Blinking = 16,
	/// <summary>高亮(反色)</summary>
	Highlight = 32,
}
