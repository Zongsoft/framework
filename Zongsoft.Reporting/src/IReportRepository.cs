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

namespace Zongsoft.Reporting;

public interface IReportRepository
{
	ValueTask<bool> DeleteAsync(string key, CancellationToken cancellation = default);

	ValueTask<string> CreateAsync(IReport report, CancellationToken cancellation = default) => this.CreateAsync(report, null, cancellation);
	ValueTask<string> CreateAsync(IReport report, ReportRepositoryOptions options, CancellationToken cancellation = default);
	ValueTask<string> CreateAsync(string name, string type, byte[] data, CancellationToken cancellation = default) => this.CreateAsync(name, type, data, null, cancellation);
	ValueTask<string> CreateAsync(string name, string type, byte[] data, ReportRepositoryOptions options, CancellationToken cancellation = default);
	ValueTask<string> UpdateAsync(string key, IReport report, CancellationToken cancellation = default);
	ValueTask<string> UpdateAsync(string key, string type, byte[] data, CancellationToken cancellation = default);
}
