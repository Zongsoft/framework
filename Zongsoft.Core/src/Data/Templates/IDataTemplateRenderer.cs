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
using System.Collections.Generic;

namespace Zongsoft.Data.Templates
{
	/// <summary>
	/// 提供数据模板渲染功能的接口。
	/// </summary>
	public interface IDataTemplateRenderer
	{
		/// <summary>获取渲染器名称。</summary>
		string Name { get; }

		/// <summary>渲染指定数据模板到输出流中。</summary>
		/// <param name="output">指定的渲染输出的数据流。</param>
		/// <param name="template">指定的数据模型。</param>
		/// <param name="data">指定的渲染数据。</param>
		/// <param name="cancellation">异步操作的取消标记。</param>
		/// <returns>返回的渲染任务。</returns>
		ValueTask RenderAsync(Stream output, IDataTemplate template, object data, CancellationToken cancellation = default);

		/// <summary>渲染指定数据模板到输出流中。</summary>
		/// <param name="output">指定的渲染输出的数据流。</param>
		/// <param name="template">指定的数据模型。</param>
		/// <param name="data">指定的渲染数据。</param>
		/// <param name="format">指定的渲染格式。</param>
		/// <param name="cancellation">异步操作的取消标记。</param>
		/// <returns>返回的渲染任务。</returns>
		ValueTask RenderAsync(Stream output, IDataTemplate template, object data, string format, CancellationToken cancellation = default);

		/// <summary>渲染指定数据模板到输出流中。</summary>
		/// <param name="output">指定的渲染输出的数据流。</param>
		/// <param name="template">指定的数据模型。</param>
		/// <param name="data">指定的渲染数据。</param>
		/// <param name="parameters">指定的渲染参数。</param>
		/// <param name="cancellation">异步操作的取消标记。</param>
		/// <returns>返回的渲染任务。</returns>
		ValueTask RenderAsync(Stream output, IDataTemplate template, object data, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default);

		/// <summary>渲染指定数据模板到输出流中。</summary>
		/// <param name="output">指定的渲染输出的数据流。</param>
		/// <param name="template">指定的数据模型。</param>
		/// <param name="data">指定的渲染数据。</param>
		/// <param name="parameters">指定的渲染参数。</param>
		/// <param name="format">指定的渲染格式。</param>
		/// <param name="cancellation">异步操作的取消标记。</param>
		/// <returns>返回的渲染任务。</returns>
		ValueTask RenderAsync(Stream output, IDataTemplate template, object data, IEnumerable<KeyValuePair<string, object>> parameters, string format, CancellationToken cancellation = default);
	}
}