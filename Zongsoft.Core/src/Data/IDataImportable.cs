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

namespace Zongsoft.Data;

/// <summary>表示提供数据导入功能的接口。</summary>
public interface IDataImportable
{
	/// <summary>获取一个值，指示是否支持导入操作。</summary>
	bool CanImport { get; }

	ValueTask<int> ImportAsync(Stream input, DataImportOptions options = null, CancellationToken cancellation = default);
	ValueTask<int> ImportAsync(Stream input, string format, DataImportOptions options = null, CancellationToken cancellation = default);
	#region 取消重载
	ValueTask<int> ImportAsync(Stream input, CancellationToken cancellation) => this.ImportAsync(input, (DataImportOptions)null, cancellation);
	ValueTask<int> ImportAsync(Stream input, string format, CancellationToken cancellation) => this.ImportAsync(input, format, null, cancellation);
	#endregion
}