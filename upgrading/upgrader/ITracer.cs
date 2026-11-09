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

namespace Zongsoft.Upgrading;

public interface ITracer
{
	ValueTask TraceAsync(string phase, IEnumerable<KeyValuePair<string, string>> parameters, CancellationToken cancellation) => this.TraceAsync(phase, null, parameters, cancellation);
	ValueTask TraceAsync(string phase, string message, IEnumerable<KeyValuePair<string, string>> parameters, CancellationToken cancellation);
}
