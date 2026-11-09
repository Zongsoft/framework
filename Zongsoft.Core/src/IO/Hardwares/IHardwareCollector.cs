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

using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.IO.Hardwares;

/// <summary>表示硬件信息采集器。</summary>
public interface IHardwareCollector
{
	/// <summary>异步采集硬件信息。</summary>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回采集到的硬件设备异步序列。</returns>
	IAsyncEnumerable<IHardware> CollectAsync(CancellationToken cancellation = default);
}
