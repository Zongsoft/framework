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

namespace Zongsoft.Text.Templating;

/// <summary>提供模板渲染功能的接口。</summary>
public interface ITemplateRenderer
{
	/// <summary>获取渲染器名称。</summary>
	string Name { get; }

	/// <summary>渲染指定模板到输出流中。</summary>
	/// <param name="output">指定的渲染输出的数据流。</param>
	/// <param name="template">指定的模板。</param>
	/// <param name="data">指定的渲染数据。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回的渲染任务。</returns>
	ValueTask RenderAsync(Stream output, ITemplate template, object data, CancellationToken cancellation = default);

	/// <summary>渲染指定模板到输出流中。</summary>
	/// <param name="output">指定的渲染输出的数据流。</param>
	/// <param name="template">指定的模板。</param>
	/// <param name="data">指定的渲染数据。</param>
	/// <param name="format">指定的渲染格式。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回的渲染任务。</returns>
	ValueTask RenderAsync(Stream output, ITemplate template, object data, string format, CancellationToken cancellation = default);

	/// <summary>渲染指定模板到输出流中。</summary>
	/// <param name="output">指定的渲染输出的数据流。</param>
	/// <param name="template">指定的模板。</param>
	/// <param name="data">指定的渲染数据。</param>
	/// <param name="parameters">指定的渲染参数。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回的渲染任务。</returns>
	ValueTask RenderAsync(Stream output, ITemplate template, object data, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default);

	/// <summary>渲染指定模板到输出流中。</summary>
	/// <param name="output">指定的渲染输出的数据流。</param>
	/// <param name="template">指定的模板。</param>
	/// <param name="data">指定的渲染数据。</param>
	/// <param name="parameters">指定的渲染参数。</param>
	/// <param name="format">指定的渲染格式。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回的渲染任务。</returns>
	ValueTask RenderAsync(Stream output, ITemplate template, object data, IEnumerable<KeyValuePair<string, object>> parameters, string format, CancellationToken cancellation = default);
}
