/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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

namespace Zongsoft.Data.Templates
{
	/// <summary>
	/// 提供数据文件生成功能的接口。
	/// </summary>
	public interface IDataArchiveGenerator
	{
		/// <summary>获取生成器名称。</summary>
		string Name { get; }

		/// <summary>获取生成器格式。</summary>
		DataArchiveFormat Format { get; }

		/// <summary>将指定的数据写入到输出流中。</summary>
		/// <param name="output">待写入的数据流。</param>
		/// <param name="model">对应的模型描述器。</param>
		/// <param name="data">待写入的数据。</param>
		/// <param name="cancellation">异步操作的取消标记。</param>
		/// <returns>返回的生成任务。</returns>
		ValueTask GenerateAsync(Stream output, ModelDescriptor model, object data, CancellationToken cancellation = default);

		/// <summary>将指定的数据写入到输出流中。</summary>
		/// <param name="output">待写入的数据流。</param>
		/// <param name="model">对应的模型描述器。</param>
		/// <param name="data">待写入的数据。</param>
		/// <param name="options">生成操作选项设置。</param>
		/// <param name="cancellation">异步操作的取消标记。</param>
		/// <returns>返回的生成任务。</returns>
		ValueTask GenerateAsync(Stream output, ModelDescriptor model, object data, IDataArchiveGeneratorOptions options, CancellationToken cancellation = default);
	}
}