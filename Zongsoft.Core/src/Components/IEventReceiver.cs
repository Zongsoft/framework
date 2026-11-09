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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components;

/// <summary>接收传输通道还原的事件，不再次向远端交换。</summary>
public interface IEventReceiver
{
	/// <summary>将接收到的事件交给本地事件注册表处理。</summary>
	/// <param name="name">事件的完整限定名称。</param>
	/// <param name="argumentType">发送方选择的已声明参数类型。</param>
	/// <param name="argument">还原后的事件实参。</param>
	/// <param name="parameters">事件参数集合。</param>
	/// <param name="cancellation">取消事件处理的令牌。</param>
	/// <returns>本地事件处理任务。</returns>
	ValueTask ReceiveAsync(string name, Type argumentType, object argument, Collections.Parameters parameters, CancellationToken cancellation = default);
}
