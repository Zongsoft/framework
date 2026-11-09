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

/// <summary>关于<seealso cref="IWorker"/>的状态信息。</summary>
public enum WorkerState
{
	/// <summary>未运行/已停止。</summary>
	Stopped = 0,
	/// <summary>运行中。</summary>
	Running = 1,

	/// <summary>正在启动中。</summary>
	Starting,
	/// <summary>正在停止中。</summary>
	Stopping,

	/// <summary>正在暂停中。</summary>
	Pausing,
	/// <summary>已经暂停。</summary>
	Paused,
	/// <summary>正在恢复中。</summary>
	Resuming,
}
