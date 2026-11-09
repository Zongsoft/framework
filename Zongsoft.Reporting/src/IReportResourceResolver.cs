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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Reporting;

public interface IReportResourceResolver
{
	string Name { get; }

	ValueTask<IReportResource> ResolveAsync(string name, Stream stream, CancellationToken cancellation = default) => this.ResolveAsync(name, stream, null, null, cancellation);
	ValueTask<IReportResource> ResolveAsync(string name, Stream stream, string title, CancellationToken cancellation = default) => this.ResolveAsync(name, stream, title, null, cancellation);
	ValueTask<IReportResource> ResolveAsync(string name, Stream stream, string title, string description, CancellationToken cancellation = default);
}
