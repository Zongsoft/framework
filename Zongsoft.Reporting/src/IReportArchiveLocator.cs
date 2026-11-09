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

namespace Zongsoft.Reporting;

public interface IReportArchiveLocator
{
	ValueTask<ReportArchiveResult> OpenAsync(string key, CancellationToken cancellation = default);
	IAsyncEnumerable<ReportArchive> FindAsync(string type, CancellationToken cancellation = default) => this.FindAsync(type, null, cancellation);
	IAsyncEnumerable<ReportArchive> FindAsync(string type, string contentType, CancellationToken cancellation = default);
}
