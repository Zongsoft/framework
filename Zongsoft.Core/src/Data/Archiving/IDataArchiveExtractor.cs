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
using System.Collections.Generic;

namespace Zongsoft.Data.Archiving;

/// <summary>提供数据文件提取功能的接口。</summary>
public interface IDataArchiveExtractor
{
	/// <summary>获取提取器名称。</summary>
	string Name { get; }

	/// <summary>获取提取器格式。</summary>
	DataArchiveFormat Format { get; }

	/// <summary>从数据流中提取数据。</summary>
	/// <typeparam name="T">指定要提取的数据模型的类型。</typeparam>
	/// <param name="input">待提取的数据流。</param>
	/// <param name="options">提取操作选项设置。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回提取到的数据集。</returns>
	IAsyncEnumerable<T> ExtractAsync<T>(Stream input, IDataArchiveExtractorOptions options, CancellationToken cancellation = default);
}