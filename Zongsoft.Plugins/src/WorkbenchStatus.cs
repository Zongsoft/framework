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

/// <summary>表示工作台的运行状态。</summary>
public enum WorkbenchStatus
{
	/// <summary>未启动或已关闭。</summary>
	None = 0,

	/// <summary>正在打开中，表示正在启动工作台。</summary>
	Opening = 1,

	/// <summary>正常运行。</summary>
	Running = 2,

	/// <summary>正在关闭中，表示正在执行关闭操作。</summary>
	Closing = 3,
}
