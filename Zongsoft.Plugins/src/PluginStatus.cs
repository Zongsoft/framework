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

/// <summary>表示插件的状态。</summary>
public enum PluginStatus
{
	/// <summary>尚未加载，表示插件刚创建。</summary>
	None = 0,

	/// <summary>表示插件正在加载。</summary>
	Loading,

	/// <summary>表示插件已经成功加载。</summary>
	Loaded,

	/// <summary>表示插件正在卸载。</summary>
	Unloading,

	/// <summary>表示插件已经被卸载。</summary>
	Unloaded,
}
