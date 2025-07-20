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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Components;

/// <summary>
/// 表示输出样式的枚举。
/// </summary>
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
