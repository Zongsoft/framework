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

namespace Zongsoft.Data.Archiving;

/// <summary>提供数据归档文件生成功能的接口。</summary>
/// <remarks>
/// <para>
///     约定当
///     <see cref="GenerateAsync(Stream, ModelDescriptor, object, CancellationToken)"/> 或
///     <see cref="GenerateAsync(Stream, ModelDescriptor, object, IDataArchiveGeneratorOptions, CancellationToken)"/>
///     的 <c>data</c> 参数为空(<see langword="null"/>)时，表示调用方请求生成供后续数据导入使用的空模板，而不是执行一次普通的零记录导出。
/// </para>
/// <para>实现应在该情况下保留模型字段结构以及导入所需的元数据，并可按导入语义排除不应由用户填写的自动生成字段。空集合仍表示普通的零记录导出，不应被解释为导入模板。</para>
/// </remarks>
public interface IDataArchiveGenerator
{
	/// <summary>获取生成器名称。</summary>
	string Name { get; }

	/// <summary>获取生成器格式。</summary>
	DataArchiveFormat Format { get; }

	/// <summary>将指定的数据写入到输出流中。</summary>
	/// <param name="output">待写入的数据流。</param>
	/// <param name="model">对应的模型描述器。</param>
	/// <param name="data">待写入的数据。若为空(<see langword="null"/>)，则表示生成供后续数据导入使用的空模板；空集合表示普通的零记录导出。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回的生成任务。</returns>
	ValueTask GenerateAsync(Stream output, ModelDescriptor model, object data, CancellationToken cancellation = default);

	/// <summary>将指定的数据写入到输出流中。</summary>
	/// <param name="output">待写入的数据流。</param>
	/// <param name="model">对应的模型描述器。</param>
	/// <param name="data">待写入的数据。若为空(<see langword="null"/>)，则表示生成供后续数据导入使用的空模板；空集合表示普通的零记录导出。</param>
	/// <param name="options">生成操作选项设置。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回的生成任务。</returns>
	ValueTask GenerateAsync(Stream output, ModelDescriptor model, object data, IDataArchiveGeneratorOptions options, CancellationToken cancellation = default);
}
