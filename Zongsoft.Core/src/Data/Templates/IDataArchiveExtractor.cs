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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Data.Templates
{
	/// <summary>
	/// 提供数据文件提取功能的接口。
	/// </summary>
	public interface IDataArchiveExtractor
	{
		/// <summary>获取提取器名称。</summary>
		string Name { get; }

		/// <summary>获取提取器格式。</summary>
		DataArchiveFormat Format { get; }

		/// <summary>从数据流中提取数据。</summary>
		/// <typeparam name="T">指定要提取的数据模型的类型。</typeparam>
		/// <param name="input">待提取的数据流。</param>
		/// <param name="model">对应的模型描述器。</param>
		/// <param name="cancellation">异步操作的取消标记。</param>
		/// <returns>返回提取到的数据集。</returns>
		IAsyncEnumerable<T> ExtractAsync<T>(Stream input, ModelDescriptor model, CancellationToken cancellation = default);

		/// <summary>从数据流中提取数据。</summary>
		/// <typeparam name="T">指定要提取的数据模型的类型。</typeparam>
		/// <param name="input">待提取的数据流。</param>
		/// <param name="model">对应的模型描述器。</param>
		/// <param name="options">提取操作选项设置。</param>
		/// <param name="cancellation">异步操作的取消标记。</param>
		/// <returns>返回提取到的数据集。</returns>
		IAsyncEnumerable<T> ExtractAsync<T>(Stream input, ModelDescriptor model, IDataArchiveExtractorOptions options, CancellationToken cancellation = default);
	}
}