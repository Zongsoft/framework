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

namespace Zongsoft.Externals.Opc;

/// <summary>表示预制件种类的枚举。</summary>
public enum PrefabKind
{
	/// <summary>未定义</summary>
	None,
	/// <summary>类型</summary>
	Type,
	/// <summary>目录</summary>
	Folder,
	/// <summary>对象</summary>
	Object,
	/// <summary>变量</summary>
	Variable,
}
