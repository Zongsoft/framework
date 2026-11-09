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

namespace Zongsoft.IO;

/// <summary>表示关于路径的锚定点。</summary>
public enum PathAnchor
{
	/// <summary>未锚定</summary>
	None,

	/// <summary>基于当前位置</summary>
	Current,

	/// <summary>基于上级节点</summary>
	Parent,

	/// <summary>从根节点开始</summary>
	Root,

	/// <summary>从应用目录开始</summary>
	Application,
}
