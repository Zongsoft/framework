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
using System.Threading.Tasks;

namespace Zongsoft.Components;

/// <summary>表示命令链执行完成的回调接口。</summary>
public interface ICommandCompletion
{
	/// <summary>等待命令链的清理操作完成。</summary>
	/// <param name="context">命令完成的结果及失败信息。</param>
	/// <returns>清理操作完成的任务。</returns>
	ValueTask OnCompletedAsync(CommandCompletionContext context);
}
