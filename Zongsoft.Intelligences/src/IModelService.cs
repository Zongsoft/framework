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
using System.Collections.Generic;

namespace Zongsoft.Intelligences;

public interface IModelService
{
	/// <summary>获取当前模型标识。</summary>
	string Model { get; }
	/// <summary>获取模型服务的设置信息。</summary>
	Configuration.IConnectionSettings Settings { get; }

	ValueTask<bool> ActivateAsync(string identifier, CancellationToken cancellation = default);
	IAsyncEnumerable<IModel> GetModelsAsync(string pattern, CancellationToken cancellation = default);
	ValueTask<IModel> GetModelAsync(string identifier, CancellationToken cancellation = default);
	ValueTask<bool> RunAsync(string identifier, CancellationToken cancellation = default);
	ValueTask<bool> StopAsync(string identifier, CancellationToken cancellation = default);
}
