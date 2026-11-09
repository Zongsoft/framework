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
 * The above copyright notice and this permission notice shall be included in all
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Upgrading;

/// <summary>提供宿主应用程序启动功能的接口，即启动器。</summary>
public interface ILauncher
{
	/// <summary>获取启动器名称。</summary>
	string Name { get; }

	/// <summary>启动宿主应用程序。</summary>
	/// <param name="argument">部署器的调用参数。</param>
	/// <param name="cancellation">取消标记。</param>
	/// <returns>启动与日志刷新完成的任务。</returns>
	ValueTask LaunchAsync(Deployer.Argument argument, CancellationToken cancellation = default);
}
