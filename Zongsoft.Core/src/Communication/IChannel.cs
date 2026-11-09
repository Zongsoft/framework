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

namespace Zongsoft.Communication;

/// <summary>表示通道的接口。</summary>
public interface IChannel : IAsyncDisposable
{
	#region 事件定义
	/// <summary>表示通道关闭完成的事件。</summary>
	event EventHandler Closed;
	#endregion

	#region 属性定义
	/// <summary>获取一个值，指示通道是否已关闭。</summary>
	bool IsClosed { get; }

	/// <summary>获取一个值，指示通道是否已释放。</summary>
	bool IsDisposed { get; }
	#endregion

	#region 方法定义
	/// <summary>关闭通道。</summary>
	/// <param name="cancellation">指定的关闭操作的异步标记。</param>
	/// <returns>返回的关闭任务。</returns>
	ValueTask CloseAsync(CancellationToken cancellation = default);
	#endregion
}
