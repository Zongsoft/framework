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

namespace Zongsoft.Diagnostics;

public interface ILogger
{
	string Name { get; }
	ValueTask FlushAsync(CancellationToken cancellation = default);
	ValueTask LogAsync<TLog>(TLog log, CancellationToken cancellation = default) where TLog : ILog;
}

public interface ILogger<TLog> : ILogger where TLog : ILog
{
	Common.IPredication<TLog> Predication { get; set; }
	ValueTask LogAsync(TLog log, CancellationToken cancellation = default);
}

public interface ILogger<TLog, out TModel> : ILogger<TLog> where TLog : ILog
{
	ILogFormatter<TLog, TModel> Formatter { get; }
}
