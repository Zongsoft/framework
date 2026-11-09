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

/// <summary>表示尝试器的接口。</summary>
public interface IAttempter
{
	/// <summary>获取或设置尝试器选项。</summary>
	IAttempterOptions Options { get; set; }

	/// <summary>确认指定身份是否可以继续验证。</summary>
	/// <param name="key">指定的尝试标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果确认成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> CheckAsync(string key, CancellationToken cancellation = default);

	/// <summary>尝试成功方法。</summary>
	/// <param name="key">指定的尝试标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果指定的尝试标识存在则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> DoneAsync(string key, CancellationToken cancellation = default);

	/// <summary>尝试失败方法。</summary>
	/// <param name="key">指定的尝试标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回验证失败是否超过阈值，如果返回真(<c>True</c>)则表示失败次数超过阈值。</returns>
	ValueTask<bool> FailAsync(string key, CancellationToken cancellation = default);
}
